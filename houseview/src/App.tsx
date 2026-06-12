import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { loadCatalogue } from './api';
import { CAR_POSITION, FLOOR_ORDER, LEVEL_HEIGHT, levelY, type FloorLevel } from './layout';
import { buildSceneModel, roomCenter, type PlacedRoom } from './model';
import { OVERVIEW_FOCUS, Scene, type Focus } from './scene/Scene';
import { DetailPanel } from './ui/DetailPanel';
import { Hud } from './ui/Hud';
import { IndexPage } from './ui/IndexPage';
import { Splash } from './ui/Splash';
import type { CatalogueData, Selection } from './types';

type View = 'house' | 'index';

const viewFromHash = (): View => (window.location.hash === '#/index' ? 'index' : 'house');

export default function App() {
  const [data, setData] = useState<CatalogueData | null>(null);
  const [floor, setFloor] = useState<FloorLevel>(0);
  const [selection, setSelection] = useState<Selection>(null);
  const [focus, setFocus] = useState<Focus>({ ...OVERVIEW_FOCUS, seq: 0 });
  const [view, setView] = useState<View>(viewFromHash);
  const seqRef = useRef(0);

  // hash routing: #/index <-> the 3D house, so refresh and deep links work
  useEffect(() => {
    const onHash = () => setView(viewFromHash());
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const navigate = useCallback((next: View) => {
    window.location.hash = next === 'index' ? '#/index' : '#/';
    setView(next);
  }, []);

  useEffect(() => {
    let cancelled = false;
    loadCatalogue().then((result) => {
      if (!cancelled) setData(result);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const model = useMemo(() => (data ? buildSceneModel(data) : null), [data]);

  const flyTo = useCallback((target: [number, number, number], zoomFactor: number) => {
    seqRef.current += 1;
    setFocus({ target, zoomFactor, seq: seqRef.current });
  }, []);

  // Switching floors glides the camera up/down to the storey being shown,
  // so the slice you asked for is also the one you are looking at.
  const changeFloor = useCallback(
    (level: FloorLevel) => {
      setFloor(level);
      const [x, , z] = OVERVIEW_FOCUS.target;
      // attic shows the whole house, so frame its middle and pull back a touch
      const targetY = level === 2 ? 4.2 : level <= 0 ? 1.2 : levelY(level) + 1.2;
      flyTo([x, targetY, z], level === 2 ? 0.92 : 1);
    },
    [flyTo],
  );

  // Arrow keys step between floors (house view only).
  useEffect(() => {
    if (view !== 'house') return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
      const idx = FLOOR_ORDER.indexOf(floor); // FLOOR_ORDER is top-down
      const next = e.key === 'ArrowUp' ? FLOOR_ORDER[idx - 1] : FLOOR_ORDER[idx + 1];
      if (next === undefined) return;
      e.preventDefault();
      changeFloor(next);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [floor, changeFloor, view]);

  // Selecting a room or item never changes the active floor - the floor is
  // locked to the switcher / arrow keys. Rooms on other floors are visible as
  // ghosts, so the camera still flies there; rooms in the buried basement
  // can't be seen, so only the panel opens.
  const flyToPlaced = (placed: PlacedRoom, zoom: number) => {
    if (placed.def.level === -1 && floor !== -1) return;
    const [x, y, z] = roomCenter(placed);
    // when the basement is out, the rest of the house is ghosted one storey up
    const lift = floor === -1 && placed.def.level >= 0 ? LEVEL_HEIGHT : 0;
    flyTo([x, y + lift, z], zoom);
  };

  const selectRoom = (roomId: number) => {
    if (!model) return;
    setSelection({ kind: 'room', roomId });
    const placed = model.placedRooms.find((p) => p.room.id === roomId);
    if (placed) {
      flyToPlaced(placed, 2.2);
      return;
    }
    const car = model.carRooms.find((c) => c.room.id === roomId);
    if (car) {
      flyTo([CAR_POSITION[0], CAR_POSITION[1] + 1, CAR_POSITION[2]], 2.2);
    }
  };

  const selectItem = (itemId: number) => {
    if (!model) return;
    setSelection({ kind: 'item', id: itemId });
    const resolved = model.itemsById.get(itemId);
    if (!resolved?.room) return;
    const placed = model.placedRooms.find((p) => p.room.id === resolved.room!.id);
    if (placed) {
      flyToPlaced(placed, 2.6);
    } else if (model.carRooms.some((c) => c.room.id === resolved.room!.id)) {
      flyTo([CAR_POSITION[0], CAR_POSITION[1] + 1, CAR_POSITION[2]], 2.6);
    }
  };

  const resetView = () => {
    setSelection(null);
    flyTo(OVERVIEW_FOCUS.target, OVERVIEW_FOCUS.zoomFactor);
  };

  const clearSelection = () => {
    setSelection(null);
  };

  // From the index: jump into the house with the camera already on its way.
  const viewItemInHouse = (itemId: number) => {
    navigate('house');
    selectItem(itemId);
  };

  return (
    <div className="app">
      {model && data && (
        <>
          <Scene
            model={model}
            floor={floor}
            selection={selection}
            focus={focus}
            onSelectItem={selectItem}
            onSelectRoom={selectRoom}
            onClear={clearSelection}
          />
          {view === 'house' && (
            <>
              <Hud
                model={model}
                live={data.live}
                floor={floor}
                onFloor={changeFloor}
                onFlyToRoom={selectRoom}
                onResetView={resetView}
                onBrowse={() => navigate('index')}
              />
              <DetailPanel model={model} selection={selection} onSelectItem={selectItem} onClose={clearSelection} />
            </>
          )}
          {view === 'index' && (
            <IndexPage model={model} live={data.live} onBack={() => navigate('house')} onViewItem={viewItemInHouse} />
          )}
        </>
      )}
      <Splash ready={model !== null} />
    </div>
  );
}
