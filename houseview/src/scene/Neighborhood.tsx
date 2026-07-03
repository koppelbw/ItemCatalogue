import { useFrame } from '@react-three/fiber';
import { useRef, type ReactNode } from 'react';
import type { Group as ThreeGroup } from 'three';
import { B, Blob, Cyl, Group } from './primitives';

// ---------------------------------------------------------------------------
// Ambient life dressing the lawn: a duck pond, a bike path with passing
// riders, dog walkers, a strolling tuxedo cat, birds hopping between the
// trees, a utility crew fixing the phone pole from a boom lift, and an
// ice-cream cart. Everything is deterministic and clock-driven, and every
// prop lives in the gaps between the stage, the satellite ring and the edge
// of the lawn so nothing collides with the buildings.
// ---------------------------------------------------------------------------

/** Tree spots shared with the Lawn so the birds know where the canopies are. */
export const TREE_SPOTS: { p: [number, number, number]; h: number; r: number }[] = [
  { p: [-6.5, 0, -4.5], h: 1.6, r: 1.1 },
  { p: [19.6, 0, 2.6], h: 1.3, r: 0.85 },
  { p: [24.9, 0, 16.9], h: 1.7, r: 1.05 },
  { p: [-7.5, 0, 14.5], h: 1.2, r: 0.8 },
  { p: [-9.2, 0, 2.5], h: 1.4, r: 0.95 },
  { p: [27, 0, 6], h: 1.5, r: 1.0 },
];

/** center of the grass disc (the bike path rings it) */
const LAWN_CENTER: [number, number] = [5.5, 5];
/** duck pond, on the northern lawn just left of the utility crew */
const POND_XZ: [number, number] = [-5.0, -9.0];

/** Walks a group around a circle, facing along its direction of travel (+x forward). */
function CircleWalker({
  center,
  radius,
  speed,
  phase,
  y = 0,
  bob = 0,
  children,
}: {
  center: [number, number];
  radius: number;
  /** angular speed in rad/s; negative walks the other way round */
  speed: number;
  phase: number;
  y?: number;
  /** hop height while moving, for walking gaits */
  bob?: number;
  children: ReactNode;
}) {
  const ref = useRef<ThreeGroup>(null);
  useFrame(({ clock }) => {
    const g = ref.current;
    if (!g) return;
    const t = clock.elapsedTime;
    const a = phase + t * speed;
    g.position.set(
      center[0] + Math.cos(a) * radius,
      y + Math.abs(Math.sin(t * 6)) * bob,
      center[1] + Math.sin(a) * radius,
    );
    const vx = -Math.sin(a) * speed;
    const vz = Math.cos(a) * speed;
    g.rotation.y = Math.atan2(-vz, vx);
  });
  return <group ref={ref}>{children}</group>;
}

/** Tiny lawn person, standing on the origin and facing +x. */
function Person({ shirt, pants = '#4a5560', hat }: { shirt: string; pants?: string; hat?: string }) {
  return (
    <>
      <B p={[0, 0.16, 0.07]} s={[0.1, 0.32, 0.1]} c={pants} />
      <B p={[0, 0.16, -0.07]} s={[0.1, 0.32, 0.1]} c={pants} />
      <B p={[0, 0.54, 0]} s={[0.2, 0.44, 0.32]} c={shirt} r={0.7} />
      <Blob p={[0, 0.9, 0]} r={0.13} c="#e8c39e" />
      {hat && <Cyl p={[0, 1.02, 0]} rTop={0.12} rBottom={0.14} h={0.09} c={hat} />}
    </>
  );
}

/** Little dog trotting on the origin, facing +x. */
function Dog({ coat }: { coat: string }) {
  return (
    <>
      <B p={[0, 0.22, 0]} s={[0.42, 0.17, 0.16]} c={coat} r={0.8} />
      <Blob p={[0.27, 0.33, 0]} r={0.1} c={coat} />
      <B p={[0.36, 0.3, 0]} s={[0.09, 0.05, 0.06]} c={coat} />
      <B p={[0.24, 0.42, 0.05]} s={[0.04, 0.09, 0.03]} c={coat} />
      <B p={[0.24, 0.42, -0.05]} s={[0.04, 0.09, 0.03]} c={coat} />
      <B p={[-0.24, 0.34, 0]} s={[0.05, 0.2, 0.04]} c={coat} />
      {[0.14, -0.14].map((x) =>
        [0.06, -0.06].map((z) => <B key={`${x}${z}`} p={[x, 0.07, z]} s={[0.05, 0.14, 0.05]} c={coat} />),
      )}
    </>
  );
}

/** Tuxedo cat: black coat, white chest, muzzle and paws. Faces +x. */
function TuxedoCat() {
  const black = '#26262c';
  const white = '#f7f4ee';
  return (
    <>
      <B p={[0, 0.13, 0]} s={[0.32, 0.12, 0.11]} c={black} r={0.8} />
      <B p={[0.1, 0.1, 0]} s={[0.1, 0.08, 0.115]} c={white} />
      <Blob p={[0.19, 0.22, 0]} r={0.075} c={black} />
      <Blob p={[0.24, 0.19, 0]} r={0.035} c={white} />
      <B p={[0.17, 0.29, 0.04]} s={[0.03, 0.05, 0.025]} c={black} />
      <B p={[0.17, 0.29, -0.04]} s={[0.03, 0.05, 0.025]} c={black} />
      <B p={[-0.17, 0.22, 0]} s={[0.035, 0.22, 0.03]} c={black} />
      {[0.1, -0.1].map((x) =>
        [0.04, -0.04].map((z) => <B key={`${x}${z}`} p={[x, 0.035, z]} s={[0.035, 0.07, 0.035]} c={white} />),
      )}
    </>
  );
}

/** Mallard-ish duck floating on the origin, facing +x. */
function Duck({ body, head }: { body: string; head: string }) {
  return (
    <>
      <Blob p={[0, 0.05, 0]} r={0.14} c={body} scale={[1.35, 0.85, 1]} />
      <Blob p={[0.15, 0.18, 0]} r={0.075} c={head} />
      <B p={[0.24, 0.17, 0]} s={[0.09, 0.035, 0.05]} c="#e9b44c" />
    </>
  );
}

/** Cyclist rolling on the origin, facing +x. */
function Cyclist({ frame, shirt }: { frame: string; shirt: string }) {
  const dark = '#2e3338';
  return (
    <>
      <Cyl p={[0.38, 0.26, 0]} rTop={0.26} rBottom={0.26} h={0.07} c={dark} rotX={Math.PI / 2} />
      <Cyl p={[-0.38, 0.26, 0]} rTop={0.26} rBottom={0.26} h={0.07} c={dark} rotX={Math.PI / 2} />
      <B p={[0, 0.5, 0]} s={[0.78, 0.07, 0.06]} c={frame} />
      <B p={[-0.18, 0.64, 0]} s={[0.06, 0.26, 0.06]} c={frame} />
      <B p={[0.32, 0.68, 0]} s={[0.06, 0.34, 0.06]} c={frame} />
      <B p={[0.32, 0.86, 0]} s={[0.05, 0.05, 0.34]} c={dark} />
      <B p={[-0.1, 0.98, 0]} s={[0.2, 0.42, 0.28]} c={shirt} r={0.7} />
      <Blob p={[0.0, 1.3, 0]} r={0.12} c="#e8c39e" />
      <Blob p={[0.02, 1.38, 0]} r={0.1} c={frame} scale={[1.1, 0.55, 1.1]} />
    </>
  );
}

// canopy top = trunk height + blob center offset (0.6r) + stretched blob radius (1.15r)
const BIRD_PERCHES = TREE_SPOTS.map(({ p, h, r }) => [p[0], h + r * 1.75 + 0.05, p[2]] as [number, number, number]);

/**
 * A bird that perches on a tree top, then flies an arc to the next tree on
 * its route. Wings fold while perched and flap in flight.
 */
function Bird({ route, color, phase }: { route: number[]; color: string; phase: number }) {
  const ref = useRef<ThreeGroup>(null);
  const wingL = useRef<ThreeGroup>(null);
  const wingR = useRef<ThreeGroup>(null);
  useFrame(({ clock }) => {
    const g = ref.current;
    if (!g) return;
    const T = 7; // seconds per stop (perch + flight)
    const k = ((clock.elapsedTime + phase) / T) % route.length;
    const i = Math.floor(k);
    const u = k - i;
    const from = BIRD_PERCHES[route[i]];
    const to = BIRD_PERCHES[route[(i + 1) % route.length]];
    const f = u < 0.65 ? 0 : (u - 0.65) / 0.35; // last 35% of each stop is flight
    const s = f * f * (3 - 2 * f);
    // long hops arc high enough to clear the central dollhouse
    const hop = Math.hypot(to[0] - from[0], to[2] - from[2]);
    g.position.set(
      from[0] + (to[0] - from[0]) * s,
      from[1] + (to[1] - from[1]) * s + Math.sin(Math.PI * s) * (1.5 + hop * 0.28),
      from[2] + (to[2] - from[2]) * s,
    );
    const flying = f > 0 && f < 1;
    if (flying) g.rotation.y = Math.atan2(-(to[2] - from[2]), to[0] - from[0]);
    const flap = flying ? Math.sin(clock.elapsedTime * 22) * 0.75 : 0.95;
    if (wingL.current) wingL.current.rotation.x = flap;
    if (wingR.current) wingR.current.rotation.x = -flap;
  });
  return (
    <group ref={ref}>
      <Blob p={[0, 0, 0]} r={0.1} c={color} scale={[1.4, 1, 1]} />
      <Blob p={[0.12, 0.06, 0]} r={0.065} c={color} />
      <B p={[0.2, 0.05, 0]} s={[0.07, 0.03, 0.03]} c="#e9b44c" />
      <group ref={wingL} position={[0, 0.04, 0.04]}>
        <B p={[0, 0, 0.09]} s={[0.15, 0.02, 0.17]} c={color} />
      </group>
      <group ref={wingR} position={[0, 0.04, -0.04]}>
        <B p={[0, 0, -0.09]} s={[0.15, 0.02, 0.17]} c={color} />
      </group>
    </group>
  );
}

/** Sandy-edged pond with reeds and lily pads; the ducks paddle circles on it. */
function Pond() {
  return (
    <group position={[POND_XZ[0], 0, POND_XZ[1]]}>
      <mesh position={[0, -0.03, 0]} receiveShadow>
        <cylinderGeometry args={[3.4, 3.5, 0.1, 40]} />
        <meshStandardMaterial color="#d8cba8" roughness={1} />
      </mesh>
      <mesh position={[0, 0.005, 0]} receiveShadow>
        <cylinderGeometry args={[3.0, 3.0, 0.08, 40]} />
        <meshStandardMaterial color="#7fb5d8" roughness={0.35} />
      </mesh>
      {[
        [2.6, 0.9],
        [2.9, 1.7],
        [-2.5, -1.2],
        [-2.8, -0.3],
      ].map(([x, z], i) => (
        <Group key={i} p={[x, 0, z]}>
          <Cyl p={[0, 0.3, 0]} rTop={0.02} rBottom={0.03} h={0.6} c="#549058" />
          <B p={[0, 0.58, 0]} s={[0.06, 0.16, 0.06]} c="#8a5f3a" />
        </Group>
      ))}
      <Cyl p={[-1.2, 0.05, 1.6]} rTop={0.22} rBottom={0.22} h={0.02} c="#6fae72" />
      <Cyl p={[0.8, 0.05, -1.9]} rTop={0.17} rBottom={0.17} h={0.02} c="#549058" />
    </group>
  );
}

/** Utility crew: phone pole, boom lift with a technician up top, spotter and cones. */
function UtilityCrew() {
  const lift = '#e9b44c';
  const hiVis = '#e8893c';
  return (
    <group position={[1.3, 0, -11.4]}>
      {/* phone pole with crossarm and insulators */}
      <Cyl p={[0, 2.5, 0]} rTop={0.08} rBottom={0.11} h={5} c="#7a5f40" />
      <B p={[0, 4.55, 0]} s={[0.09, 0.09, 1.5]} c="#7a5f40" />
      <Blob p={[0, 4.66, 0.6]} r={0.05} c="#c9ced3" />
      <Blob p={[0, 4.66, -0.6]} r={0.05} c="#c9ced3" />
      {/* boom lift reaching in from beside the pole */}
      <group position={[3.4, 0, 0]} rotation={[0, Math.PI, 0]}>
        <B p={[0, 0.42, 0]} s={[1.6, 0.4, 0.95]} c={lift} />
        {[0.55, -0.55].map((x) =>
          [0.5, -0.5].map((z) => (
            <Cyl key={`${x}${z}`} p={[x, 0.2, z]} rTop={0.2} rBottom={0.2} h={0.14} c="#2e3338" rotX={Math.PI / 2} />
          )),
        )}
        <B p={[-0.3, 0.72, 0]} s={[0.55, 0.24, 0.55]} c={lift} />
        <mesh position={[1.05, 2.18, 0]} rotation={[0, 0, 0.81]} castShadow>
          <boxGeometry args={[3.95, 0.16, 0.16]} />
          <meshStandardMaterial color={lift} roughness={0.8} />
        </mesh>
        <B p={[2.4, 3.7, 0]} s={[0.58, 0.42, 0.52]} c={lift} />
        {/* technician in the bucket */}
        <B p={[2.4, 4.05, 0]} s={[0.2, 0.38, 0.26]} c={hiVis} r={0.7} />
        <Blob p={[2.4, 4.36, 0]} r={0.12} c="#e8c39e" />
        <Cyl p={[2.4, 4.47, 0]} rTop={0.11} rBottom={0.13} h={0.08} c="#f2c14e" />
      </group>
      {/* spotter watching from the ground + safety cones */}
      <Group p={[1.5, 0, 1.3]} rotY={2.4}>
        <Person shirt={hiVis} hat="#f2c14e" />
      </Group>
      {[
        [0.9, 1.9],
        [4.6, 1.5],
        [5.2, -1.3],
      ].map(([x, z], i) => (
        <Group key={i} p={[x, 0, z]}>
          <B p={[0, 0.02, 0]} s={[0.22, 0.04, 0.22]} c={hiVis} />
          <Cyl p={[0, 0.17, 0]} rTop={0.03} rBottom={0.12} h={0.3} c={hiVis} />
        </Group>
      ))}
    </group>
  );
}

/** Ice-cream cart with a pink umbrella and its vendor, parked just behind the pond. */
function IceCreamCart() {
  const pink = '#ef6f6c';
  const white = '#f7f4ee';
  return (
    <group position={[-8.1, 0, -12.4]} rotation={[0, 0.6, 0]}>
      <B p={[0, 0.58, 0]} s={[1.1, 0.62, 0.7]} c={white} r={0.6} />
      <B p={[0, 0.93, 0]} s={[1.12, 0.1, 0.72]} c={pink} />
      <Cyl p={[0.4, 0.24, 0.36]} rTop={0.2} rBottom={0.2} h={0.07} c="#2e3338" rotX={Math.PI / 2} />
      <Cyl p={[-0.4, 0.24, 0.36]} rTop={0.2} rBottom={0.2} h={0.07} c="#2e3338" rotX={Math.PI / 2} />
      <B p={[-0.62, 0.72, 0]} s={[0.06, 0.06, 0.5]} c="#9aa1a8" />
      {/* giant display cone on the counter */}
      <Cyl p={[0.3, 1.1, 0]} rTop={0.09} rBottom={0.03} h={0.22} c="#d9b98c" />
      <Blob p={[0.3, 1.26, 0]} r={0.09} c={pink} />
      {/* umbrella */}
      <Cyl p={[-0.25, 1.6, -0.1]} rTop={0.035} rBottom={0.035} h={1.5} c="#9aa1a8" />
      <Cyl p={[-0.25, 2.4, -0.1]} rTop={0.06} rBottom={1.05} h={0.42} c={pink} />
      <Blob p={[-0.25, 2.64, -0.1]} r={0.06} c={white} />
      {/* vendor */}
      <Group p={[1.05, 0, 0]} rotY={Math.PI}>
        <Person shirt={white} hat={pink} />
      </Group>
    </group>
  );
}

/** The paved loop the cyclists ride, between the satellite ring and the lawn edge. */
function BikePath() {
  return (
    <mesh position={[LAWN_CENTER[0], 0.015, LAWN_CENTER[1]]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
      <ringGeometry args={[27.0, 28.2, 96]} />
      <meshStandardMaterial color="#d8cfbc" roughness={1} />
    </mesh>
  );
}

export function NeighborhoodLife() {
  return (
    <group>
      <Pond />
      <BikePath />
      <UtilityCrew />
      <IceCreamCart />
      {/* ducks paddling the pond */}
      <CircleWalker center={POND_XZ} radius={1.0} speed={0.3} phase={0.5} y={0.06}>
        <Duck body="#cfc9b8" head="#2e7d4f" />
      </CircleWalker>
      <CircleWalker center={POND_XZ} radius={1.6} speed={-0.22} phase={2.6} y={0.06}>
        <Duck body="#c3b9a4" head="#2e7d4f" />
      </CircleWalker>
      <CircleWalker center={POND_XZ} radius={2.1} speed={0.26} phase={4.4} y={0.06}>
        <Duck body="#f2efe4" head="#f2efe4" />
      </CircleWalker>
      {/* cyclists lapping the bike path in both directions */}
      <CircleWalker center={LAWN_CENTER} radius={27.6} speed={0.16} phase={1.2}>
        <Cyclist frame="#d04f3a" shirt="#5b8def" />
      </CircleWalker>
      <CircleWalker center={LAWN_CENTER} radius={27.6} speed={-0.12} phase={4.2}>
        <Cyclist frame="#3a6ea5" shirt="#e9b44c" />
      </CircleWalker>
      {/* dog walkers strolling the bike path (the dollhouse footprint owns the inner lawn) */}
      <CircleWalker center={LAWN_CENTER} radius={27.35} speed={0.035} phase={0.8} bob={0.03}>
        <Person shirt="#5b8def" />
        <Group p={[0.55, 0, 0.4]}>
          <Dog coat="#8a5f3a" />
        </Group>
      </CircleWalker>
      <CircleWalker center={LAWN_CENTER} radius={27.85} speed={-0.03} phase={3.6} bob={0.03}>
        <Person shirt="#ef6f6c" pants="#6b5a4a" />
        <Group p={[0.55, 0, -0.4]}>
          <Dog coat="#d9c2a0" />
        </Group>
      </CircleWalker>
      {/* the tuxedo cat patrolling the pond's sandy rim */}
      <CircleWalker center={POND_XZ} radius={3.25} speed={0.14} phase={1.9} y={0.03} bob={0.015}>
        <TuxedoCat />
      </CircleWalker>
      {/* birds hopping from tree to tree */}
      <Bird route={[0, 4, 3, 2, 5, 1]} color="#d04f3a" phase={0} />
      <Bird route={[5, 1, 0, 3, 4, 2]} color="#5b8def" phase={9.5} />
    </group>
  );
}
