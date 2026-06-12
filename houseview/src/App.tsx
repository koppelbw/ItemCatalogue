import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { loadCatalogue } from './api';
import { CAR_POSITION, FLOOR_ORDER, LEVEL_HEIGHT, levelY, type FloorLevel } from './layout';
import { buildSceneModel, roomCenter, siteKeyForLocation, type PlacedRoom, type Site } from './model';
import { OVERVIEW_FOCUS, Scene, type Focus } from './scene/Scene';
import { AboutPage } from './ui/AboutPage';
import { DetailPanel } from './ui/DetailPanel';
import { Hud } from './ui/Hud';
import { IndexPage } from './ui/IndexPage';
import { Splash } from './ui/Splash';
import type { CatalogueData, Selection } from './types';

type View = 'house' | 'index' | 'about';

const viewFromHash = (): View =>
  window.location.hash === '#/index' ? 'index' : window.location.hash === '#/about' ? 'about' : 'house';

export default function App() {
  const [data, setData] = useState<CatalogueData | null>(null);
  const [floor, setFloor] = useState<FloorLevel>(0);
  const [selection, setSelection] = useState<Selection>(null);
  const [focus, setFocus] = useState<Focus>({ ...OVERVIEW_FOCUS, seq: 0 });
  const [view, setView] = useState<View>(viewFromHash);
  const [activeSite, setActiveSite] = useState('house');
  const seqRef = useRef(0);

  // hash routing: #/index <-> the 3D house, so refresh and deep links work
  useEffect(() => {
    const onHash = () => setView(viewFromHash());
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const navigate = useCallback((next: View) => {
    window.location.hash = next === 'index' ? '#/index' : next === 'about' ? '#/about' : '#/';
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

  // Fly to a site and make it the active one.
  const goToSite = useCallback(
    (site: Site, zoomOverride?: number) => {
      setActiveSite(site.key);
      flyTo(site.def.focus, zoomOverride ?? site.def.zoom);
    },
    [flyTo],
  );

  const selectSite = (key: string) => {
    if (!model) return;
    const site = model.sites.find((s) => s.key === key);
    if (!site) return;
    setSelection(site.location ? { kind: 'location', id: site.location.id } : null);
    goToSite(site);
  };

  // Keyboard: up/down steps floors (house only); left/right hops between locations.
  useEffect(() => {
    if (view !== 'house') return;
    const onKey = (e: KeyboardEvent) => {
      if ((e.key === 'ArrowUp' || e.key === 'ArrowDown') && activeSite === 'house') {
        const idx = FLOOR_ORDER.indexOf(floor); // FLOOR_ORDER is top-down
        const next = e.key === 'ArrowUp' ? FLOOR_ORDER[idx - 1] : FLOOR_ORDER[idx + 1];
        if (next === undefined) return;
        e.preventDefault();
        changeFloor(next);
        return;
      }
      if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && model) {
        e.preventDefault();
        const keys = model.sites.map((s) => s.key);
        const idx = Math.max(0, keys.indexOf(activeSite));
        const next = e.key === 'ArrowRight' ? (idx + 1) % keys.length : (idx - 1 + keys.length) % keys.length;
        const site = model.sites[next];
        setSelection(site.location ? { kind: 'location', id: site.location.id } : null);
        goToSite(site);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [floor, changeFloor, view, activeSite, model, goToSite]);

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
      setActiveSite('house');
      flyToPlaced(placed, 2.2);
      return;
    }
    if (model.carRooms.some((c) => c.room.id === roomId)) {
      setActiveSite('car');
      flyTo([CAR_POSITION[0], CAR_POSITION[1] + 1, CAR_POSITION[2]], 2.2);
    }
  };

  // An item lives at its location's site: house items fly to their room,
  // everything else flies to the site building.
  const selectItem = (itemId: number) => {
    if (!model) return;
    setSelection({ kind: 'item', id: itemId });
    const resolved = model.itemsById.get(itemId);
    if (!resolved?.location) return;
    const siteKey = siteKeyForLocation(resolved.location);
    const site = model.sites.find((s) => s.key === siteKey);
    if (!site) return;
    if (siteKey === 'house') {
      setActiveSite('house');
      const placed = resolved.room ? model.placedRooms.find((p) => p.room.id === resolved.room!.id) : undefined;
      if (placed) flyToPlaced(placed, 2.6);
      else flyTo(site.def.focus, 2.0);
      return;
    }
    goToSite(site, site.def.zoom + 0.4);
  };

  const resetView = () => {
    setSelection(null);
    setActiveSite('house');
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
            activeSite={activeSite}
            onSelectItem={selectItem}
            onSelectRoom={selectRoom}
            onSelectSite={selectSite}
            onClear={clearSelection}
          />
          {view === 'house' && (
            <>
              <Hud
                model={model}
                live={data.live}
                floor={floor}
                activeSite={activeSite}
                onFloor={changeFloor}
                onFlyToRoom={selectRoom}
                onSite={selectSite}
                onResetView={resetView}
                onBrowse={() => navigate('index')}
                onAbout={() => navigate('about')}
              />
              <DetailPanel model={model} selection={selection} onSelectItem={selectItem} onClose={clearSelection} />
            </>
          )}
          {view === 'index' && (
            <IndexPage
              model={model}
              live={data.live}
              onBack={() => navigate('house')}
              onAbout={() => navigate('about')}
              onViewItem={viewItemInHouse}
            />
          )}
          {view === 'about' && (
            <AboutPage model={model} live={data.live} onBack={() => navigate('house')} onIndex={() => navigate('index')} />
          )}
        </>
      )}
      <Splash ready={model !== null} />
    </div>
  );
}
