import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useCatalogue } from './api';
import { FLOOR_ORDER, LEVEL_HEIGHT, levelY, type FloorLevel } from './layout';
import { buildSceneModel, placeRooms, roomCenter, type PlacedRoom, type Site } from './model';
import { OVERVIEW_FOCUS, Scene, type Focus } from './scene/Scene';
import { AboutPage } from './ui/AboutPage';
import { DetailPanel } from './ui/DetailPanel';
import { DeleteItemDialog, ItemForm, type RefData } from './ui/forms/EntityForms';
import { Hud } from './ui/Hud';
import { IndexPage } from './ui/IndexPage';
import { ManagePage } from './ui/ManagePage';
import { Splash } from './ui/Splash';
import type { ItemResponse, Selection } from './types';

type View = 'house' | 'index' | 'about' | 'manage';

const viewFromHash = (): View =>
  window.location.hash === '#/index'
    ? 'index'
    : window.location.hash === '#/about'
      ? 'about'
      : window.location.hash === '#/manage'
        ? 'manage'
        : 'house';

export default function App() {
  const { data } = useCatalogue();
  const model = useMemo(() => (data ? buildSceneModel(data) : null), [data]);

  const [floor, setFloor] = useState<FloorLevel>(0);
  const [selection, setSelection] = useState<Selection>(null);
  const [focus, setFocus] = useState<Focus>({ ...OVERVIEW_FOCUS, seq: 0 });
  const [view, setView] = useState<View>(viewFromHash);
  const [activeSite, setActiveSite] = useState<string>('');
  // 3D edit affordances host their modals here so the house view can open them
  const [itemForm, setItemForm] = useState<
    { mode: 'edit'; item: ItemResponse } | { mode: 'add'; roomId?: number; containerId?: number } | null
  >(null);
  const [deleteTarget, setDeleteTarget] = useState<ItemResponse | null>(null);
  const seqRef = useRef(0);

  // The Location whose rooms fill the central dollhouse. Falls back to the first
  // location until one is explicitly chosen, so the stage is never empty.
  const activeKey = model ? (model.sitesByKey.has(activeSite) ? activeSite : (model.sites[0]?.key ?? '')) : '';
  const activeSiteObj = model?.sitesByKey.get(activeKey) ?? null;
  const placedRooms = useMemo(
    () => (model && activeSiteObj ? placeRooms(activeSiteObj.rooms, model.itemsByRoom) : []),
    [model, activeSiteObj],
  );

  // reference lists for the item form's room/container/owner pickers
  const lookups = useMemo<RefData>(
    () => ({
      locations: data?.locations ?? [],
      rooms: data?.rooms ?? [],
      containers: data?.containers ?? [],
      persons: data?.persons ?? [],
    }),
    [data],
  );

  // hash routing: #/index and #/about <-> the 3D house, so refresh and deep links work
  useEffect(() => {
    const onHash = () => setView(viewFromHash());
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const navigate = useCallback((next: View) => {
    window.location.hash =
      next === 'index' ? '#/index' : next === 'about' ? '#/about' : next === 'manage' ? '#/manage' : '#/';
    setView(next);
  }, []);

  const flyTo = useCallback((target: [number, number, number], zoomFactor: number) => {
    seqRef.current += 1;
    setFocus({ target, zoomFactor, seq: seqRef.current });
  }, []);

  // Switching floors glides the camera up/down to the storey being shown.
  const changeFloor = useCallback(
    (level: FloorLevel) => {
      setFloor(level);
      const [x, , z] = OVERVIEW_FOCUS.target;
      const targetY = level === 2 ? 4.2 : level <= 0 ? 1.2 : levelY(level) + 1.2;
      flyTo([x, targetY, z], level === 2 ? 0.92 : 1);
    },
    [flyTo],
  );

  // Fly to a Location: make it active (its rooms fill the dollhouse) and frame the stage.
  const goToSite = useCallback(
    (site: Site) => {
      setActiveSite(site.key);
      flyTo(OVERVIEW_FOCUS.target, 1.05);
    },
    [flyTo],
  );

  const selectSite = (key: string) => {
    if (!model) return;
    const site = model.sitesByKey.get(key);
    if (!site) return;
    setSelection({ kind: 'location', id: site.location.id });
    goToSite(site);
  };

  // Keyboard: up/down steps floors of the active dollhouse; left/right hops Locations.
  useEffect(() => {
    if (view !== 'house') return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
        const idx = FLOOR_ORDER.indexOf(floor); // FLOOR_ORDER is top-down
        const next = e.key === 'ArrowUp' ? FLOOR_ORDER[idx - 1] : FLOOR_ORDER[idx + 1];
        if (next === undefined) return;
        e.preventDefault();
        changeFloor(next);
        return;
      }
      if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && model && model.sites.length > 0) {
        e.preventDefault();
        const keys = model.sites.map((s) => s.key);
        const idx = Math.max(0, keys.indexOf(activeKey));
        const nextIdx = e.key === 'ArrowRight' ? (idx + 1) % keys.length : (idx - 1 + keys.length) % keys.length;
        const site = model.sites[nextIdx];
        setSelection({ kind: 'location', id: site.location.id });
        goToSite(site);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [floor, changeFloor, view, activeKey, model, goToSite]);

  // Selecting a room/item never changes the active floor (locked to the switcher).
  // Rooms on buried floors can't be seen, so only the panel opens.
  const flyToPlaced = (placed: PlacedRoom, zoom: number) => {
    if (placed.def.level === -1 && floor !== -1) return;
    const [x, y, z] = roomCenter(placed);
    const lift = floor === -1 && placed.def.level >= 0 ? LEVEL_HEIGHT : 0;
    flyTo([x, y + lift, z], zoom);
  };

  // Bring the Location that owns `roomId` onto the stage, then fly to the room.
  const flyToRoomInLocation = (roomId: number, zoom: number) => {
    if (!model) return;
    const room = model.roomsById.get(roomId);
    if (!room) return;
    const site = model.sites.find((s) => s.location.id === room.locationId);
    if (!site) return;
    setActiveSite(site.key);
    const placed = placeRooms(site.rooms, model.itemsByRoom).find((p) => p.room.id === roomId);
    if (placed) flyToPlaced(placed, zoom);
    else flyTo(OVERVIEW_FOCUS.target, 1.05);
  };

  const selectRoom = (roomId: number) => {
    if (!model) return;
    setSelection({ kind: 'room', roomId });
    flyToRoomInLocation(roomId, 2.2);
  };

  // A container ultimately sits in a room; resolve that and fly to it.
  const selectContainer = (id: number) => {
    if (!model) return;
    setSelection({ kind: 'container', id });
    let c = model.containersById.get(id) ?? null;
    const seen = new Set<number>();
    while (c && !seen.has(c.id)) {
      seen.add(c.id);
      if (c.roomId != null) {
        flyToRoomInLocation(c.roomId, 2.4);
        return;
      }
      c = c.parentContainerId != null ? (model.containersById.get(c.parentContainerId) ?? null) : null;
    }
  };

  const selectItem = (itemId: number) => {
    if (!model) return;
    setSelection({ kind: 'item', id: itemId });
    const resolved = model.itemsById.get(itemId);
    if (!resolved?.room) return;
    flyToRoomInLocation(resolved.room.id, 2.6);
  };

  const resetView = () => {
    setSelection(null);
    flyTo(OVERVIEW_FOCUS.target, 0.7); // pull back to take in the whole neighbourhood
  };

  const clearSelection = () => setSelection(null);

  // 3D edit affordances (only shown when live)
  const onEditItem = (item: ItemResponse) => setItemForm({ mode: 'edit', item });
  const onDeleteItem = (item: ItemResponse) => setDeleteTarget(item);
  const onAddToRoom = (roomId: number) => setItemForm({ mode: 'add', roomId });
  const onAddToContainer = (containerId: number) => setItemForm({ mode: 'add', containerId });

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
            placedRooms={placedRooms}
            floor={floor}
            selection={selection}
            focus={focus}
            activeSite={activeKey}
            onSelectItem={selectItem}
            onSelectRoom={selectRoom}
            onSelectSite={selectSite}
            onClear={clearSelection}
          />
          {view === 'house' && (
            <>
              <Hud
                model={model}
                placedRooms={placedRooms}
                live={data.live}
                floor={floor}
                activeSite={activeKey}
                onFloor={changeFloor}
                onFlyToRoom={selectRoom}
                onSite={selectSite}
                onResetView={resetView}
                onBrowse={() => navigate('index')}
                onAbout={() => navigate('about')}
                onManage={() => navigate('manage')}
              />
              <DetailPanel
                model={model}
                selection={selection}
                live={data.live}
                onSelectItem={selectItem}
                onEditItem={onEditItem}
                onDeleteItem={onDeleteItem}
                onAddToRoom={onAddToRoom}
                onAddToContainer={onAddToContainer}
                onClose={clearSelection}
              />
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
          {view === 'manage' && <ManagePage onBack={() => navigate('house')} />}
          {itemForm && (
            <ItemForm
              initial={itemForm.mode === 'edit' ? itemForm.item : undefined}
              lookups={lookups}
              presetRoomId={itemForm.mode === 'add' ? (itemForm.roomId ?? null) : null}
              presetContainerId={itemForm.mode === 'add' ? (itemForm.containerId ?? null) : null}
              onClose={() => setItemForm(null)}
            />
          )}
          {deleteTarget && <DeleteItemDialog item={deleteTarget} onClose={() => setDeleteTarget(null)} />}
        </>
      )}
      <Splash ready={model !== null} />
    </div>
  );
}
