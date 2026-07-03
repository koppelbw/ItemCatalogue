import { OrbitControls, OrthographicCamera, ContactShadows } from '@react-three/drei';
import { Canvas, useThree } from '@react-three/fiber';
import gsap from 'gsap';
import { useEffect, useMemo, useRef } from 'react';
import {
  Vector3,
  type Group as ThreeGroup,
  type Material,
  type Mesh,
  type OrthographicCamera as ThreeOrthographicCamera,
} from 'three';
import type { OrbitControls as OrbitControlsImpl } from 'three-stdlib';
import { HEADLESS } from '../headless';
import { HOUSE_BASE, LEVEL_HEIGHT, levelY, itemSpots, type Rect } from '../layout';
import { placedBounds, type PlacedRoom, type SceneModel } from '../model';
import { ItemMarker } from './ItemMarker';
import { NeighborhoodLife, TREE_SPOTS } from './Neighborhood';
import { RoomBox } from './RoomBox';
import { SiteBuilding } from './Sites';
import { B, Blob, Cyl, Group } from './primitives';
import type { Selection } from '../types';

export interface Focus {
  target: [number, number, number];
  zoomFactor: number;
  seq: number;
}

export const OVERVIEW_FOCUS: Omit<Focus, 'seq'> = { target: [6.5, 1.2, 4.5], zoomFactor: 1 };

interface SceneProps {
  model: SceneModel;
  /** rooms of the active Location, laid onto the central dollhouse stage */
  placedRooms: PlacedRoom[];
  /** levelIndex of the storey in focus */
  floor: number;
  selection: Selection;
  focus: Focus;
  activeSite: string;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  onSelectContainer: (id: number) => void;
  onSelectSite: (key: string) => void;
  onClear: () => void;
}

function CameraRig({ focus }: { focus: Focus }) {
  const camRef = useRef<ThreeOrthographicCamera>(null);
  const controlsRef = useRef<OrbitControlsImpl>(null);
  const size = useThree((s) => s.size);
  const baseZoom = Math.max(18, Math.min(60, Math.min(size.width, size.height) / 19));
  const baseZoomRef = useRef(baseZoom);
  baseZoomRef.current = baseZoom;
  const tweenRef = useRef<gsap.core.Tween | null>(null);

  useEffect(() => {
    const cam = camRef.current;
    const controls = controlsRef.current;
    if (!cam || !controls) return;
    tweenRef.current?.kill();

    const startPos = cam.position.clone();
    const startTarget = controls.target.clone();
    const delta = new Vector3(...focus.target).sub(startTarget);
    const startZoom = cam.zoom;
    const endZoom = baseZoomRef.current * focus.zoomFactor;
    const proxy = { t: 0 };
    tweenRef.current = gsap.to(proxy, {
      t: 1,
      duration: 1.25,
      ease: 'power3.inOut',
      onUpdate: () => {
        const step = delta.clone().multiplyScalar(proxy.t);
        controls.target.copy(startTarget).add(step);
        cam.position.copy(startPos).add(step);
        cam.zoom = startZoom + (endZoom - startZoom) * proxy.t;
        cam.updateProjectionMatrix();
        controls.update();
      },
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [focus.seq]);

  return (
    <>
      <OrthographicCamera ref={camRef} makeDefault position={[30, 26, 30]} zoom={baseZoom} near={-150} far={400} />
      <OrbitControls
        ref={controlsRef}
        makeDefault
        target={OVERVIEW_FOCUS.target}
        enableDamping
        dampingFactor={0.08}
        minZoom={10}
        maxZoom={180}
        maxPolarAngle={1.15}
        minPolarAngle={0.3}
      />
    </>
  );
}

function Tree({ p, h = 1.4, r = 0.9 }: { p: [number, number, number]; h?: number; r?: number }) {
  return (
    <Group p={p}>
      <Cyl p={[0, h / 2, 0]} rTop={0.13} rBottom={0.2} h={h} c="#8a5f3a" />
      <Blob p={[0, h + r * 0.6, 0]} r={r} c="#5e9b62" scale={[1, 1.15, 1]} />
      <Blob p={[r * 0.5, h + r * 0.25, r * 0.3]} r={r * 0.6} c="#6fae72" />
      <Blob p={[-r * 0.5, h + r * 0.35, -r * 0.25]} r={r * 0.55} c="#549058" />
    </Group>
  );
}

/** Foundation lip around each room footprint, in scene units. Overlaps across
    the ~0.25u gaps between adjacent rooms so the plinths read as one slab. */
const PLINTH_MARGIN = 0.28;

/** Grass, a foundation plinth that follows the building footprint, and lawn dressing. */
function Lawn({ bounds, plinths, house }: { bounds: Rect | null; plinths: Rect[]; house: boolean }) {
  const b = bounds ?? { x: 1.5, z: 0.5, w: 10, d: 8 };
  const cx = b.x + b.w / 2;
  // front (south) edge of the building along its centre line, so the walk-up
  // meets the actual front rooms instead of the far corner of the bounding box
  const spanning = plinths.filter((r) => r.x - PLINTH_MARGIN <= cx && r.x + r.w + PLINTH_MARGIN >= cx);
  const frontZ = spanning.length ? Math.max(...spanning.map((r) => r.z + r.d + PLINTH_MARGIN)) : b.z + b.d;
  return (
    <group>
      {/* grass disc - big enough for the whole neighbourhood */}
      <mesh position={[5.5, -0.16, 5]} receiveShadow>
        <cylinderGeometry args={[31, 32, 0.32, 64]} />
        <meshStandardMaterial color="#9cc480" roughness={1} />
      </mesh>
      {/* concrete foundation: one plinth per ground-floor room, sized to the room
          plus a lip. The lips overlap on shared walls so the slabs merge, while
          concave notches in the plan stay lawn instead of paved dead space. Tops
          are coplanar and one flat colour, so overlaps never read as z-fighting. */}
      {plinths.map((r, i) => (
        <mesh key={i} position={[r.x + r.w / 2, HOUSE_BASE / 2 - 0.06, r.z + r.d / 2]} castShadow receiveShadow>
          <boxGeometry args={[r.w + PLINTH_MARGIN * 2, HOUSE_BASE, r.d + PLINTH_MARGIN * 2]} />
          <meshStandardMaterial color="#b3aa99" roughness={1} />
        </mesh>
      ))}
      {/* walk-up path and mailbox, only when the active building is a home */}
      {house && (
        <>
          <B p={[cx - 0.6, -0.045, frontZ + 1.05]} s={[1.3, 0.24, 2.1]} c="#d4cabb" r={1} />
          <Group p={[cx - 1.7, 0, frontZ + 1.9]}>
            <Cyl p={[0, 0.5, 0]} rTop={0.05} rBottom={0.05} h={1.0} c="#7a5f40" />
            <B p={[0, 1.08, 0]} s={[0.3, 0.26, 0.5]} c="#d04f3a" r={0.5} />
          </Group>
        </>
      )}
      {TREE_SPOTS.map((t, i) => (
        <Tree key={i} p={t.p} h={t.h} r={t.r} />
      ))}
      <Blob p={[cx - 3.4, 0.25, frontZ + 0.7]} r={0.45} c="#6fae72" scale={[1.3, 0.7, 1]} />
      <Blob p={[cx + 2.6, 0.25, frontZ + 0.5]} r={0.4} c="#549058" scale={[1.2, 0.65, 1]} />
    </group>
  );
}

/** Gabled roof crowning the top storey. The south-facing panel is glassy so top-floor items stay visible. */
function Roof({ bounds, wallTop }: { bounds: Rect; wallTop: number }) {
  const halfDepth = bounds.d / 2;
  const rise = Math.min(2.2, halfDepth * 0.55);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.5;
  const angle = Math.atan2(rise, halfDepth);
  return (
    <group position={[bounds.x + bounds.w / 2, wallTop, bounds.z + halfDepth]}>
      {/* north panel (solid) */}
      <mesh position={[0, rise / 2, -halfDepth / 2]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[bounds.w + 1, 0.14, panelLen]} />
        <meshStandardMaterial color="#b86b4b" roughness={0.85} />
      </mesh>
      {/* south panel (glassy, see-through) */}
      <mesh position={[0, rise / 2, halfDepth / 2]} rotation={[angle, 0, 0]}>
        <boxGeometry args={[bounds.w + 1, 0.1, panelLen]} />
        <meshStandardMaterial color="#e8f4fa" roughness={0.2} transparent opacity={0.22} depthWrite={false} />
      </mesh>
      {/* ridge beam */}
      <B p={[0, rise + 0.05, 0]} s={[bounds.w + 1.2, 0.18, 0.3]} c="#9c5639" />
      {/* chimney */}
      <Group p={[bounds.w * 0.28, rise - 0.4, -halfDepth * 0.4]}>
        <B p={[0, 0.6, 0]} s={[0.7, 1.6, 0.7]} c="#b08a8a" />
        <B p={[0, 1.45, 0]} s={[0.85, 0.14, 0.85]} c="#8d6d6d" />
      </Group>
    </group>
  );
}

interface HouseLevelsProps {
  placedRooms: PlacedRoom[];
  floor: number;
  selection: Selection;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  onSelectContainer: (id: number) => void;
}

interface FadeEntry {
  mat: Material & { userData: Record<string, unknown> };
  baseOpacity: number;
  baseTransparent: boolean;
  baseDepthWrite: boolean;
}

/** Opacity of inactive floors - present but see-through. */
const GHOST_OPACITY = 0.16;

function HouseLevels({ placedRooms, floor, selection, onSelectItem, onSelectRoom, onSelectContainer }: HouseLevelsProps) {
  const groupRefs = useRef(new Map<number, ThreeGroup>());
  // one stable fade proxy per storey so a floor change can kill the previous
  // opacity tween; a throwaway proxy would leave the old tween running, and
  // its onComplete would snap a freshly ghosted floor back to full opacity
  const fadeProxies = useRef(new Map<number, { f: number }>());
  const firstRun = useRef(true);

  // storeys present in the active location, top-down
  const byLevel = useMemo(() => {
    const map = new Map<number, PlacedRoom[]>();
    for (const placed of placedRooms) {
      const list = map.get(placed.level) ?? [];
      list.push(placed);
      map.set(placed.level, list);
    }
    return map;
  }, [placedRooms]);
  const levels = useMemo(() => [...byLevel.keys()].sort((a, b) => b - a), [byLevel]);
  const topLevel = levels[0] ?? 0;
  const topBounds = useMemo(() => placedBounds(byLevel.get(topLevel) ?? []), [byLevel, topLevel]);
  const topWallTop = useMemo(
    () => (byLevel.get(topLevel) ?? []).reduce((max, p) => Math.max(max, p.wallHeight), 0),
    [byLevel, topLevel],
  );

  // Floor focus, dollhouse style: the storey in focus (and the ones below it)
  // are solid; storeys above stay in place as translucent ghosts so the whole
  // silhouette is always readable. A basement rises out of the ground when
  // active, and the rest of the house lifts a storey (as ghosts) to make room.
  useEffect(() => {
    const first = firstRun.current;
    firstRun.current = false;

    // materials are recreated whenever the active location's rooms change, so
    // walk the group fresh each time and stash each material's true base
    // opacity in userData the first time we meet it
    const materialsFor = (g: ThreeGroup): FadeEntry[] => {
      const entries: FadeEntry[] = [];
      g.traverse((obj) => {
        const mesh = obj as Mesh;
        if (!mesh.isMesh) return;
        const mats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
        for (const raw of mats) {
          const mat = raw as FadeEntry['mat'];
          if (mat.userData.baseOpacity === undefined) {
            mat.userData.baseOpacity = mat.opacity;
            mat.userData.baseTransparent = mat.transparent;
            mat.userData.baseDepthWrite = mat.depthWrite;
          }
          entries.push({
            mat,
            baseOpacity: mat.userData.baseOpacity as number,
            baseTransparent: mat.userData.baseTransparent as boolean,
            baseDepthWrite: mat.userData.baseDepthWrite as boolean,
          });
        }
      });
      return entries;
    };

    const applyFade = (entries: FadeEntry[], f: number) => {
      for (const e of entries) e.mat.opacity = e.baseOpacity * f;
    };

    const basementView = floor < 0;
    levels.forEach((level, i) => {
      const g = groupRefs.current.get(level);
      if (!g) return;
      const entries = materialsFor(g);
      let proxy = fadeProxies.current.get(level);
      if (!proxy) {
        proxy = { f: 1 };
        fadeProxies.current.set(level, proxy);
      }
      // stop any in-flight fade before reading state: a stale tween would keep
      // fighting this one per-frame and fire its onComplete restore later
      gsap.killTweensOf(proxy);
      // re-sync from the actual material state — materials are recreated (at
      // full base opacity) whenever the active location's room tree changes
      proxy.f = entries[0] ? entries[0].mat.opacity / (entries[0].baseOpacity || 1) : 1;

      let targetF: number;
      let targetY: number;
      if (level < 0) {
        // buried storeys are invisible until focused, then rise to the surface
        targetF = level === floor ? 1 : 0;
        targetY = level === floor ? HOUSE_BASE : levelY(level);
      } else {
        targetF = basementView ? GHOST_OPACITY : level <= floor ? 1 : GHOST_OPACITY;
        targetY = basementView ? levelY(level) + LEVEL_HEIGHT : levelY(level);
      }

      gsap.killTweensOf(g.position);

      if (first) {
        // intro: every floor drops in from above while fading up
        g.position.y = level < 0 ? levelY(level) : levelY(level) + 2.4;
        proxy.f = 0;
        applyFade(entries, 0);
      }
      if (targetF > 0) g.visible = true;
      for (const e of entries) {
        e.mat.transparent = true;
        e.mat.depthWrite = targetF === 1 ? e.baseDepthWrite : false;
      }

      const delay = first ? 0.35 + (levels.length - 1 - i) * 0.09 : 0; // bottom-up on intro
      const duration = first ? 0.85 : 0.6;
      gsap.to(g.position, { y: targetY, duration, delay, ease: 'power3.out' });
      gsap.to(proxy, {
        f: targetF,
        duration: duration * 0.9,
        delay,
        ease: 'power2.out',
        onUpdate: () => applyFade(entries, proxy.f),
        onComplete: () => {
          if (targetF === 0) g.visible = false;
          if (targetF === 1) {
            // restore original material flags so render sorting stays clean
            for (const e of entries) {
              e.mat.transparent = e.baseTransparent;
              e.mat.opacity = e.baseOpacity;
              e.mat.depthWrite = e.baseDepthWrite;
            }
          }
        },
      });
    });
  }, [floor, levels, byLevel]);

  return (
    <group>
      {levels.map((level) => (
        <group
          key={level}
          position={[0, levelY(level), 0]}
          ref={(g) => {
            if (g) groupRefs.current.set(level, g);
          }}
        >
          {(byLevel.get(level) ?? []).map((placed) => (
            <RoomBox
              key={placed.room.id}
              placed={placed}
              selection={selection}
              onSelectItem={onSelectItem}
              onSelectRoom={onSelectRoom}
              onSelectContainer={onSelectContainer}
              showTag={placed.level === floor}
              interactive={floor < 0 ? placed.level === floor : placed.level >= 0 && placed.level <= floor}
            />
          ))}
          {level === topLevel && topLevel >= 1 && topBounds && <Roof bounds={topBounds} wallTop={topWallTop} />}
        </group>
      ))}
    </group>
  );
}

/** Items with no location / unmatched room shown on a pallet by the curb. */
function UnassignedPallet({ model, selection, onSelectItem }: Pick<SceneProps, 'model' | 'selection' | 'onSelectItem'>) {
  if (model.unassigned.length === 0) return null;
  const spots = itemSpots({ x: 0, z: 0, w: 3, d: 2 }, model.unassigned.length);
  return (
    <group position={[-8, 0, 15]}>
      <B p={[1.5, 0.08, 1]} s={[3.4, 0.16, 2.4]} c="#c0a884" r={1} />
      {model.unassigned.map((resolved, i) => (
        <ItemMarker
          key={resolved.item.id}
          resolved={resolved}
          position={[spots[i][0], 0.16, spots[i][1]]}
          selected={selection?.kind === 'item' && selection.id === resolved.item.id}
          onSelect={onSelectItem}
        />
      ))}
    </group>
  );
}

export function Scene({
  model,
  placedRooms,
  floor,
  selection,
  focus,
  activeSite,
  onSelectItem,
  onSelectRoom,
  onSelectContainer,
  onSelectSite,
  onClear,
}: SceneProps) {
  // every Location except the active one is a satellite building; the active one
  // is drawn as the central cutaway dollhouse from `placedRooms`.
  const satellites = model.sites.filter((s) => s.key !== activeSite);
  // The foundation follows only the lowest storey that sits on the ground; upper
  // floors (a floating attic here) would otherwise pave notches with no room below.
  const nonNeg = placedRooms.filter((p) => p.level >= 0);
  const groundLevel = nonNeg.length ? Math.min(...nonNeg.map((p) => p.level)) : 0;
  const groundPlaced = nonNeg.filter((p) => p.level === groundLevel);
  const groundBounds = placedBounds(groundPlaced);
  const plinths = groundPlaced.map((p) => p.rect);
  // homes get the walk-up path + mailbox; apartments, storage units and cars don't
  const activeKind = model.sitesByKey.get(activeSite)?.def.kind;
  const activeIsHouse = activeKind === 'cabin' || activeKind === 'cottage' || activeKind === 'townhouse';
  return (
    <Canvas
      shadows
      dpr={[1, 2]}
      gl={{ antialias: true, alpha: true, preserveDrawingBuffer: HEADLESS }}
      onCreated={(state) => {
        if (HEADLESS) (window as unknown as { __r3f?: unknown }).__r3f = state;
      }}
      onPointerMissed={onClear}
      style={{ position: 'absolute', inset: 0 }}
    >
      <CameraRig focus={focus} />
      <ambientLight intensity={0.6} color="#fff4e2" />
      <hemisphereLight args={['#cfe5ff', '#d8c9a8', 0.5]} />
      <directionalLight
        position={[22, 30, 14]}
        intensity={1.5}
        color="#ffe9c2"
        castShadow
        shadow-mapSize-width={2048}
        shadow-mapSize-height={2048}
        shadow-camera-left={-34}
        shadow-camera-right={34}
        shadow-camera-top={34}
        shadow-camera-bottom={-34}
        shadow-camera-near={1}
        shadow-camera-far={90}
        shadow-bias={-0.0004}
      />
      <Lawn bounds={groundBounds} plinths={plinths} house={activeIsHouse} />
      <NeighborhoodLife />
      <HouseLevels
        placedRooms={placedRooms}
        floor={floor}
        selection={selection}
        onSelectItem={onSelectItem}
        onSelectRoom={onSelectRoom}
        onSelectContainer={onSelectContainer}
      />
      {satellites.map((site, i) => (
        <SiteBuilding
          key={site.key}
          site={site}
          index={i}
          active={selection?.kind === 'location' && selection.id === site.location.id}
          onSelectSite={onSelectSite}
        />
      ))}
      <UnassignedPallet model={model} selection={selection} onSelectItem={onSelectItem} />
      <ContactShadows position={[5.5, 0.01, 5]} opacity={0.3} scale={56} blur={2.2} far={6} resolution={512} frames={1} />
    </Canvas>
  );
}
