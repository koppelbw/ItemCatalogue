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
import { CAR_POSITION, FLOOR_ORDER, HOUSE_BASE, LEVEL_HEIGHT, levelY, itemSpots, type FloorLevel } from '../layout';
import type { SceneModel } from '../model';
import { Car } from './Car';
import { ItemMarker } from './ItemMarker';
import { RoomBox } from './RoomBox';
import { B, Blob, Cyl, Group } from './primitives';
import type { Selection } from '../types';

export interface Focus {
  target: [number, number, number];
  zoomFactor: number;
  seq: number;
}

export const OVERVIEW_FOCUS: Omit<Focus, 'seq'> = { target: [6.5, 1.2, 5.5], zoomFactor: 1 };

interface SceneProps {
  model: SceneModel;
  floor: FloorLevel;
  selection: Selection;
  focus: Focus;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
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

function Lawn() {
  return (
    <group>
      {/* grass disc */}
      <mesh position={[5.5, -0.16, 5]} receiveShadow>
        <cylinderGeometry args={[26, 27, 0.32, 64]} />
        <meshStandardMaterial color="#9cc480" roughness={1} />
      </mesh>
      {/* concrete foundation plinth the house sits on; its top stays below the
          floor slab tops so no two surfaces ever share a plane */}
      <mesh position={[5, HOUSE_BASE / 2 - 0.06, 4]} castShadow receiveShadow>
        <boxGeometry args={[10.5, HOUSE_BASE, 8.5]} />
        <meshStandardMaterial color="#b3aa99" roughness={1} />
      </mesh>
      <mesh position={[12.3, HOUSE_BASE / 2 - 0.06, 2.6]} castShadow receiveShadow>
        <boxGeometry args={[4.9, HOUSE_BASE, 5.7]} />
        <meshStandardMaterial color="#b3aa99" roughness={1} />
      </mesh>
      {/* driveway in front of the garage */}
      <B p={[12.4, -0.045, 9.2]} s={[4.4, 0.24, 8.0]} c="#c2bcb0" r={1} />
      {/* path to the porch */}
      <B p={[4.5, -0.045, 10]} s={[1.3, 0.24, 4.2]} c="#d4cabb" r={1} />
      {/* porch slab at the south entry gap */}
      <B p={[4.5, -0.02, 8.6]} s={[2.6, 0.28, 1.4]} c="#cfc5b6" r={1} />
      {/* mailbox at the end of the path */}
      <Group p={[3.6, 0, 12.0]}>
        <Cyl p={[0, 0.5, 0]} rTop={0.05} rBottom={0.05} h={1.0} c="#7a5f40" />
        <B p={[0, 1.08, 0]} s={[0.3, 0.26, 0.5]} c="#d04f3a" r={0.5} />
      </Group>
      <Tree p={[-3.5, 0, -2.5]} h={1.6} r={1.1} />
      <Tree p={[18.5, 0, 1.5]} h={1.3} r={0.85} />
      <Tree p={[19, 0, 12.5]} h={1.7} r={1.05} />
      <Tree p={[-6.5, 0, 13.5]} h={1.2} r={0.8} />
      <Blob p={[1.2, 0.25, 9.6]} r={0.45} c="#6fae72" scale={[1.3, 0.7, 1]} />
      <Blob p={[7.6, 0.25, 9.4]} r={0.4} c="#549058" scale={[1.2, 0.65, 1]} />
      <Blob p={[-2.2, 0.3, 3.0]} r={0.5} c="#6fae72" scale={[1.25, 0.7, 1.1]} />
    </group>
  );
}

/** Gabled roof that crowns the attic level. The south-facing panel is glassy so attic items stay visible. */
function Roof() {
  const halfDepth = 4.0; // attic footprint is 10 x 8, ridge runs along x at z = 4
  const rise = 2.0;
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.5;
  const angle = Math.atan2(rise, halfDepth);
  const wallTop = 1.45; // attic uses short knee walls
  return (
    <group position={[5, wallTop, 4]}>
      {/* north panel (solid) */}
      <mesh position={[0, rise / 2, -halfDepth / 2]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[11, 0.14, panelLen]} />
        <meshStandardMaterial color="#b86b4b" roughness={0.85} />
      </mesh>
      {/* south panel (glassy, see-through) */}
      <mesh position={[0, rise / 2, halfDepth / 2]} rotation={[angle, 0, 0]}>
        <boxGeometry args={[11, 0.1, panelLen]} />
        <meshStandardMaterial color="#e8f4fa" roughness={0.2} transparent opacity={0.22} depthWrite={false} />
      </mesh>
      {/* ridge beam */}
      <B p={[0, rise + 0.05, 0]} s={[11.2, 0.18, 0.3]} c="#9c5639" />
      {/* chimney */}
      <Group p={[3.2, rise - 0.4, -1.6]}>
        <B p={[0, 0.6, 0]} s={[0.7, 1.6, 0.7]} c="#b08a8a" />
        <B p={[0, 1.45, 0]} s={[0.85, 0.14, 0.85]} c="#8d6d6d" />
      </Group>
    </group>
  );
}

interface HouseLevelsProps extends Omit<SceneProps, 'focus' | 'onClear'> {}

/** How far a floor rises off the house while fading away. */
const FLOOR_LIFT = 2.4;

interface FadeEntry {
  mat: Material;
  baseOpacity: number;
  baseTransparent: boolean;
  baseDepthWrite: boolean;
}

/** Opacity of inactive floors - present but see-through. */
const GHOST_OPACITY = 0.16;

function HouseLevels({ model, floor, selection, onSelectItem, onSelectRoom }: HouseLevelsProps) {
  const groupRefs = useRef(new Map<FloorLevel, ThreeGroup>());
  const fadeMats = useRef(new Map<FloorLevel, FadeEntry[]>());
  const fadeProxies = useRef(new Map<FloorLevel, { f: number }>());
  const firstRun = useRef(true);

  const byLevel = useMemo(() => {
    const map = new Map<FloorLevel, typeof model.placedRooms>();
    for (const placed of model.placedRooms) {
      const list = map.get(placed.def.level) ?? [];
      list.push(placed);
      map.set(placed.def.level, list);
    }
    return map;
  }, [model]);

  // Floor focus, dollhouse style: the active floor (and the ones below it) are
  // solid; floors above stay in place as translucent ghosts so the whole house
  // silhouette is always readable. The basement rises out of the ground when
  // active, and the rest of the house lifts a storey (as ghosts) to make room.
  useEffect(() => {
    const first = firstRun.current;
    firstRun.current = false;

    const materialsFor = (level: FloorLevel, g: ThreeGroup): FadeEntry[] => {
      const cached = fadeMats.current.get(level);
      if (cached) return cached;
      const entries: FadeEntry[] = [];
      g.traverse((obj) => {
        const mesh = obj as Mesh;
        if (!mesh.isMesh) return;
        const mats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
        for (const mat of mats) {
          entries.push({ mat, baseOpacity: mat.opacity, baseTransparent: mat.transparent, baseDepthWrite: mat.depthWrite });
        }
      });
      fadeMats.current.set(level, entries);
      return entries;
    };

    const applyFade = (entries: FadeEntry[], f: number) => {
      for (const e of entries) e.mat.opacity = e.baseOpacity * f;
    };

    FLOOR_ORDER.forEach((level, i) => {
      const g = groupRefs.current.get(level);
      if (!g) return;
      const entries = materialsFor(level, g);
      const proxy = fadeProxies.current.get(level) ?? { f: 1 };
      fadeProxies.current.set(level, proxy);

      const targetF =
        floor === -1
          ? level === -1
            ? 1
            : GHOST_OPACITY
          : level === -1
            ? 0
            : level <= floor
              ? 1
              : GHOST_OPACITY;
      const targetY =
        level === -1
          ? floor === -1
            ? HOUSE_BASE
            : levelY(-1)
          : floor === -1
            ? levelY(level) + LEVEL_HEIGHT
            : levelY(level);

      gsap.killTweensOf(proxy);
      gsap.killTweensOf(g.position);

      if (first) {
        // intro: every floor drops in from above while fading up
        g.position.y = level === -1 ? levelY(-1) : levelY(level) + FLOOR_LIFT;
        proxy.f = 0;
        applyFade(entries, 0);
      }
      if (targetF > 0) g.visible = true;
      for (const e of entries) {
        e.mat.transparent = true;
        e.mat.depthWrite = targetF === 1 ? e.baseDepthWrite : false;
      }

      const delay = first ? 0.35 + (FLOOR_ORDER.length - 1 - i) * 0.09 : 0; // bottom-up on intro
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
  }, [floor]);

  return (
    <group>
      {FLOOR_ORDER.map((level) => (
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
              showTag={placed.def.level === floor}
              interactive={floor === -1 ? placed.def.level === -1 : placed.def.level <= floor}
            />
          ))}
          {level === 2 && <Roof />}
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
    <group position={[-4.5, 0, 9.5]}>
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

function CarRig(props: Pick<SceneProps, 'model' | 'selection' | 'onSelectItem' | 'onSelectRoom'>) {
  const ref = useRef<ThreeGroup>(null);
  useEffect(() => {
    const g = ref.current;
    if (!g) return;
    // the car arrives home during the intro
    gsap.fromTo(
      g.position,
      { z: CAR_POSITION[2] - 16 },
      { z: CAR_POSITION[2], duration: 1.6, delay: 0.7, ease: 'power3.out' },
    );
  }, []);
  if (props.model.carRooms.length === 0) return null;
  return (
    <group ref={ref} position={CAR_POSITION}>
      <Car
        carRooms={props.model.carRooms}
        selection={props.selection}
        onSelectItem={props.onSelectItem}
        onSelectRoom={props.onSelectRoom}
      />
    </group>
  );
}

export function Scene({ model, floor, selection, focus, onSelectItem, onSelectRoom, onClear }: SceneProps) {
  return (
    <Canvas
      shadows
      dpr={[1, 2]}
      gl={{ antialias: true, alpha: true }}
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
        shadow-camera-left={-26}
        shadow-camera-right={26}
        shadow-camera-top={26}
        shadow-camera-bottom={-26}
        shadow-camera-near={1}
        shadow-camera-far={90}
        shadow-bias={-0.0004}
      />
      <Lawn />
      <HouseLevels model={model} floor={floor} selection={selection} onSelectItem={onSelectItem} onSelectRoom={onSelectRoom} />
      <CarRig model={model} selection={selection} onSelectItem={onSelectItem} onSelectRoom={onSelectRoom} />
      <UnassignedPallet model={model} selection={selection} onSelectItem={onSelectItem} />
      <ContactShadows position={[5.5, 0.01, 5]} opacity={0.3} scale={45} blur={2.2} far={6} resolution={512} frames={1} />
    </Canvas>
  );
}
