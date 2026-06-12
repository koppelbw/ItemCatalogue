import type { FurnitureKind, Rect } from '../layout';
import { B, Blob, Cyl, Group } from './primitives';

// Sims-style furniture vignettes built from primitives. Each set receives the
// room footprint and lays out against the north (z=0) and west (x=0) walls,
// which are the walls the cutaway keeps.

const WOOD = '#9a7b54';
const WOOD_DARK = '#7a5f40';
const WHITE = '#f7f4ee';
const STEEL = '#aeb6bd';
const FABRIC = '#e2a18f';

function Living({ w, d }: Rect) {
  return (
    <>
      {/* rug */}
      <mesh position={[w / 2, 0.012, d / 2 + 0.3]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <circleGeometry args={[1.5, 32]} />
        <meshStandardMaterial color="#e8cdb1" roughness={1} />
      </mesh>
      {/* sofa against west wall, facing east */}
      <Group p={[0.65, 0, d / 2 + 0.2]}>
        <B p={[0, 0.28, 0]} s={[0.85, 0.4, 2.2]} c={FABRIC} />
        <B p={[-0.3, 0.66, 0]} s={[0.28, 0.5, 2.2]} c={FABRIC} />
        <B p={[0, 0.52, -1.05]} s={[0.85, 0.34, 0.24]} c={FABRIC} />
        <B p={[0, 0.52, 1.05]} s={[0.85, 0.34, 0.24]} c={FABRIC} />
        <B p={[0.05, 0.55, -0.5]} s={[0.6, 0.16, 0.85]} c="#f0bfae" />
        <B p={[0.05, 0.55, 0.5]} s={[0.6, 0.16, 0.85]} c="#f0bfae" />
      </Group>
      {/* coffee table */}
      <Group p={[w / 2, 0, d / 2 + 0.3]}>
        <B p={[0, 0.36, 0]} s={[1.1, 0.07, 0.6]} c={WOOD} />
        <B p={[-0.46, 0.18, -0.22]} s={[0.07, 0.36, 0.07]} c={WOOD_DARK} />
        <B p={[0.46, 0.18, -0.22]} s={[0.07, 0.36, 0.07]} c={WOOD_DARK} />
        <B p={[-0.46, 0.18, 0.22]} s={[0.07, 0.36, 0.07]} c={WOOD_DARK} />
        <B p={[0.46, 0.18, 0.22]} s={[0.07, 0.36, 0.07]} c={WOOD_DARK} />
      </Group>
      {/* TV against north wall */}
      <Group p={[w / 2, 0, 0.42]}>
        <B p={[0, 0.25, 0]} s={[1.8, 0.5, 0.42]} c={WOOD_DARK} />
        <B p={[0, 1.05, 0.02]} s={[1.6, 0.92, 0.07]} c="#23262b" emissive="#3b4f66" emissiveIntensity={0.7} />
      </Group>
      {/* plant in the corner */}
      <Group p={[w - 0.6, 0, 0.6]}>
        <Cyl p={[0, 0.22, 0]} rTop={0.2} rBottom={0.15} h={0.44} c="#c96f4a" />
        <Blob p={[0, 0.78, 0]} r={0.34} c="#5e9b62" scale={[1, 1.25, 1]} />
      </Group>
    </>
  );
}

function Kitchen({ w, d }: Rect) {
  const counterUnits = Math.floor((w - 1.4) / 0.7);
  return (
    <>
      {/* fridge in the west corner */}
      <Group p={[0.55, 0, 0.55]}>
        <B p={[0, 0.95, 0]} s={[0.75, 1.9, 0.72]} c={STEEL} r={0.45} />
        <B p={[0.31, 1.25, 0.3]} s={[0.06, 0.5, 0.05]} c="#7c848b" />
      </Group>
      {/* counter run along the north wall */}
      {Array.from({ length: counterUnits }, (_, i) => (
        <Group key={i} p={[1.45 + i * 0.7, 0, 0.45]}>
          <B p={[0, 0.45, 0]} s={[0.68, 0.9, 0.62]} c={WHITE} />
          <B p={[0, 0.93, 0]} s={[0.7, 0.06, 0.66]} c="#cdd4cf" r={0.4} />
        </Group>
      ))}
      {/* stove slot with burners */}
      <Group p={[1.45 + counterUnits * 0.7, 0, 0.45]}>
        <B p={[0, 0.45, 0]} s={[0.68, 0.9, 0.62]} c="#dadfe2" r={0.5} />
        <B p={[0, 0.93, 0]} s={[0.7, 0.06, 0.66]} c="#2e3338" r={0.4} />
        <Cyl p={[-0.15, 0.97, -0.12]} rTop={0.09} rBottom={0.09} h={0.02} c="#15181b" />
        <Cyl p={[0.15, 0.97, -0.12]} rTop={0.09} rBottom={0.09} h={0.02} c="#15181b" />
        <Cyl p={[-0.15, 0.97, 0.16]} rTop={0.09} rBottom={0.09} h={0.02} c="#15181b" />
        <Cyl p={[0.15, 0.97, 0.16]} rTop={0.09} rBottom={0.09} h={0.02} c="#15181b" />
      </Group>
      {/* island */}
      <Group p={[w / 2, 0, d / 2 + 0.5]}>
        <B p={[0, 0.45, 0]} s={[1.3, 0.9, 0.7]} c="#cfa97e" />
        <B p={[0, 0.93, 0]} s={[1.4, 0.06, 0.8]} c={WHITE} r={0.4} />
      </Group>
    </>
  );
}

function Dining({ w, d }: Rect) {
  const cx = w / 2;
  const cz = d / 2;
  const chair = (x: number, z: number, rotY: number) => (
    <Group key={`${x}-${z}`} p={[x, 0, z]} rotY={rotY}>
      <B p={[0, 0.24, 0]} s={[0.4, 0.07, 0.4]} c={WOOD} />
      <B p={[0, 0.5, -0.18]} s={[0.4, 0.55, 0.06]} c={WOOD} />
      <B p={[-0.16, 0.1, -0.16]} s={[0.05, 0.22, 0.05]} c={WOOD_DARK} />
      <B p={[0.16, 0.1, -0.16]} s={[0.05, 0.22, 0.05]} c={WOOD_DARK} />
      <B p={[-0.16, 0.1, 0.16]} s={[0.05, 0.22, 0.05]} c={WOOD_DARK} />
      <B p={[0.16, 0.1, 0.16]} s={[0.05, 0.22, 0.05]} c={WOOD_DARK} />
    </Group>
  );
  return (
    <>
      <Group p={[cx, 0, cz]}>
        <B p={[0, 0.72, 0]} s={[1.9, 0.08, 1.05]} c={WOOD} />
        <B p={[-0.85, 0.36, -0.42]} s={[0.08, 0.72, 0.08]} c={WOOD_DARK} />
        <B p={[0.85, 0.36, -0.42]} s={[0.08, 0.72, 0.08]} c={WOOD_DARK} />
        <B p={[-0.85, 0.36, 0.42]} s={[0.08, 0.72, 0.08]} c={WOOD_DARK} />
        <B p={[0.85, 0.36, 0.42]} s={[0.08, 0.72, 0.08]} c={WOOD_DARK} />
        {/* fruit bowl */}
        <Cyl p={[0, 0.82, 0]} rTop={0.18} rBottom={0.1} h={0.1} c="#e3eef2" />
        <Blob p={[0, 0.9, 0]} r={0.09} c="#e8893c" />
      </Group>
      {chair(cx - 0.55, cz - 0.95, 0)}
      {chair(cx + 0.55, cz - 0.95, 0)}
      {chair(cx - 0.55, cz + 0.95, Math.PI)}
      {chair(cx + 0.55, cz + 0.95, Math.PI)}
      {/* sideboard along west wall */}
      <Group p={[0.4, 0, d / 2]}>
        <B p={[0, 0.4, 0]} s={[0.5, 0.8, 1.6]} c={WOOD} />
        <Cyl p={[0, 0.95, 0]} rTop={0.12} rBottom={0.16} h={0.3} c="#7fa8c9" />
      </Group>
    </>
  );
}

function Bathroom({ w, d }: Rect) {
  return (
    <>
      {/* tub along north wall */}
      <Group p={[w / 2 + 0.3, 0, 0.6]}>
        <B p={[0, 0.3, 0]} s={[1.7, 0.6, 0.85]} c={WHITE} r={0.35} />
        <B p={[0, 0.52, 0]} s={[1.45, 0.2, 0.6]} c="#bfdfe8" r={0.2} />
        <Cyl p={[-0.7, 0.75, -0.25]} rTop={0.03} rBottom={0.03} h={0.3} c={STEEL} />
      </Group>
      {/* pedestal sink against west wall */}
      <Group p={[0.45, 0, d - 0.8]}>
        <Cyl p={[0, 0.4, 0]} rTop={0.1} rBottom={0.14} h={0.8} c={WHITE} />
        <Cyl p={[0, 0.84, 0]} rTop={0.26} rBottom={0.18} h={0.12} c={WHITE} />
      </Group>
      {/* toilet */}
      <Group p={[0.5, 0, d / 2 - 0.4]}>
        <B p={[0, 0.22, 0]} s={[0.42, 0.44, 0.5]} c={WHITE} r={0.35} />
        <B p={[-0.16, 0.55, 0]} s={[0.12, 0.5, 0.46]} c={WHITE} r={0.35} />
      </Group>
      {/* bath mat */}
      <mesh position={[w / 2 + 0.3, 0.012, 1.6]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <planeGeometry args={[1.2, 0.6]} />
        <meshStandardMaterial color="#9ec8d8" roughness={1} />
      </mesh>
    </>
  );
}

function Bedroom({ w, d }: Rect) {
  return (
    <>
      {/* bed against the north wall */}
      <Group p={[w / 2 - 0.6, 0, 1.35]}>
        <B p={[0, 0.22, 0]} s={[1.75, 0.3, 2.25]} c={WOOD} />
        <B p={[0, 0.46, 0]} s={[1.65, 0.22, 2.15]} c={WHITE} r={0.6} />
        <B p={[0, 0.5, 0.35]} s={[1.65, 0.18, 1.45]} c="#a78bdb" r={0.7} />
        <B p={[-0.42, 0.62, -0.85]} s={[0.62, 0.14, 0.4]} c="#f3eee6" r={0.6} />
        <B p={[0.42, 0.62, -0.85]} s={[0.62, 0.14, 0.4]} c="#f3eee6" r={0.6} />
        <B p={[0, 0.62, -1.16]} s={[1.75, 0.85, 0.1]} c={WOOD_DARK} />
      </Group>
      {/* nightstands */}
      <Group p={[w / 2 - 1.85, 0, 0.55]}>
        <B p={[0, 0.27, 0]} s={[0.45, 0.54, 0.45]} c={WOOD} />
        <Cyl p={[0, 0.66, 0]} rTop={0.1} rBottom={0.13} h={0.22} c="#f2c14e" />
      </Group>
      <Group p={[w / 2 + 0.65, 0, 0.55]}>
        <B p={[0, 0.27, 0]} s={[0.45, 0.54, 0.45]} c={WOOD} />
      </Group>
      {/* wardrobe along the west wall */}
      <Group p={[0.45, 0, d - 1.4]}>
        <B p={[0, 1.0, 0]} s={[0.6, 2.0, 1.5]} c={WOOD} />
        <B p={[0.26, 1.0, -0.3]} s={[0.05, 0.4, 0.06]} c={WOOD_DARK} />
        <B p={[0.26, 1.0, 0.3]} s={[0.05, 0.4, 0.06]} c={WOOD_DARK} />
      </Group>
      {/* rug */}
      <mesh position={[w / 2, 0.012, d - 1.3]} rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <circleGeometry args={[1.1, 32]} />
        <meshStandardMaterial color="#d9c8ea" roughness={1} />
      </mesh>
    </>
  );
}

function Office({ w, d }: Rect) {
  const books = ['#ef6f6c', '#5b8def', '#f2a93b', '#3ec6b8', '#a78bdb', '#e8893c'];
  return (
    <>
      {/* desk along north wall */}
      <Group p={[w / 2 + 0.4, 0, 0.6]}>
        <B p={[0, 0.74, 0]} s={[1.8, 0.06, 0.75]} c={WOOD} />
        <B p={[-0.82, 0.37, 0]} s={[0.08, 0.74, 0.65]} c={WOOD_DARK} />
        <B p={[0.82, 0.37, 0]} s={[0.08, 0.74, 0.65]} c={WOOD_DARK} />
        {/* monitor */}
        <B p={[0, 1.12, -0.12]} s={[0.85, 0.5, 0.05]} c="#23262b" emissive="#4a6f8f" emissiveIntensity={0.8} />
        <Cyl p={[0, 0.82, -0.1]} rTop={0.05} rBottom={0.09} h={0.14} c="#3a3f44" />
        {/* keyboard */}
        <B p={[0, 0.79, 0.18]} s={[0.55, 0.03, 0.18]} c="#dadfe2" />
      </Group>
      {/* chair */}
      <Group p={[w / 2 + 0.4, 0, 1.55]}>
        <Cyl p={[0, 0.12, 0]} rTop={0.04} rBottom={0.3} h={0.1} c="#3a3f44" />
        <Cyl p={[0, 0.3, 0]} rTop={0.04} rBottom={0.04} h={0.3} c="#3a3f44" />
        <B p={[0, 0.5, 0]} s={[0.48, 0.08, 0.48]} c="#4b5560" />
        <B p={[0, 0.85, 0.2]} s={[0.46, 0.62, 0.08]} c="#4b5560" />
      </Group>
      {/* bookshelf along west wall */}
      <Group p={[0.4, 0, d / 2 + 0.4]}>
        <B p={[0, 0.95, 0]} s={[0.42, 1.9, 1.5]} c={WOOD} />
        {books.map((c, i) => (
          <B
            key={i}
            p={[0.05, 0.55 + Math.floor(i / 3) * 0.62, -0.5 + (i % 3) * 0.42]}
            s={[0.3, 0.42, 0.3]}
            c={c}
          />
        ))}
      </Group>
    </>
  );
}

function Storage({ w, d }: Rect) {
  const crate = (x: number, y: number, z: number, s: number, rotY = 0) => (
    <B key={`${x}${y}${z}`} p={[x, y, z]} s={[s, s * 0.78, s]} c="#c89a6b" rotY={rotY} />
  );
  return (
    <>
      {/* shelving rack along north wall */}
      <Group p={[w / 2, 0, 0.5]}>
        <B p={[0, 0.05, 0]} s={[2.4, 0.06, 0.7]} c={STEEL} />
        <B p={[0, 0.75, 0]} s={[2.4, 0.06, 0.7]} c={STEEL} />
        <B p={[0, 1.45, 0]} s={[2.4, 0.06, 0.7]} c={STEEL} />
        <B p={[-1.17, 0.75, -0.32]} s={[0.06, 1.5, 0.06]} c="#7c848b" />
        <B p={[1.17, 0.75, -0.32]} s={[0.06, 1.5, 0.06]} c="#7c848b" />
        <B p={[-1.17, 0.75, 0.32]} s={[0.06, 1.5, 0.06]} c="#7c848b" />
        <B p={[1.17, 0.75, 0.32]} s={[0.06, 1.5, 0.06]} c="#7c848b" />
        {crate(-0.7, 0.32, 0, 0.5)}
        {crate(0.1, 0.32, 0, 0.55, 0.3)}
        {crate(0.8, 1.0, 0, 0.45)}
        {crate(-0.4, 1.0, 0, 0.5, -0.2)}
      </Group>
      {/* loose stack on the floor */}
      {crate(0.8, 0.27, d - 1.0, 0.68)}
      {crate(0.85, 0.78, d - 1.05, 0.55, 0.4)}
    </>
  );
}

function Garage({ w, d }: Rect) {
  return (
    <>
      {/* workbench along north wall */}
      <Group p={[w / 2 + 0.5, 0, 0.55]}>
        <B p={[0, 0.45, 0]} s={[2.0, 0.9, 0.7]} c={WOOD_DARK} />
        <B p={[0, 0.93, 0]} s={[2.1, 0.06, 0.78]} c="#b08a5a" />
        <B p={[-0.5, 1.0, 0]} s={[0.3, 0.08, 0.2]} c="#d04f3a" />
      </Group>
      {/* pegboard */}
      <B p={[w / 2 + 0.5, 1.6, 0.16]} s={[1.8, 0.8, 0.06]} c="#cfa97e" />
      {/* tire stack */}
      <Group p={[0.8, 0, d - 1.2]}>
        <Cyl p={[0, 0.16, 0]} rTop={0.36} rBottom={0.36} h={0.3} c="#2e3338" />
        <Cyl p={[0, 0.48, 0]} rTop={0.36} rBottom={0.36} h={0.3} c="#2e3338" />
        <Cyl p={[0, 0.8, 0]} rTop={0.36} rBottom={0.36} h={0.3} c="#2e3338" />
      </Group>
      {/* tool cabinet */}
      <Group p={[0.55, 0, 1.0]}>
        <B p={[0, 0.55, 0]} s={[0.7, 1.1, 0.55]} c="#d04f3a" r={0.5} />
        <B p={[0.3, 0.4, 0]} s={[0.05, 0.05, 0.4]} c={STEEL} />
        <B p={[0.3, 0.7, 0]} s={[0.05, 0.05, 0.4]} c={STEEL} />
      </Group>
    </>
  );
}

function Basement({ w, d }: Rect) {
  return (
    <>
      {/* water heater */}
      <Group p={[0.9, 0, 0.9]}>
        <Cyl p={[0, 0.85, 0]} rTop={0.42} rBottom={0.42} h={1.7} c="#dadfe2" />
        <Cyl p={[0, 1.78, 0]} rTop={0.1} rBottom={0.1} h={0.5} c="#9aa1a8" />
      </Group>
      {/* washer + dryer */}
      <Group p={[2.6, 0, 0.55]}>
        <B p={[0, 0.45, 0]} s={[0.8, 0.9, 0.7]} c={WHITE} r={0.4} />
        <Cyl p={[0, 0.5, 0.36]} rTop={0.22} rBottom={0.22} h={0.04} c="#7fa8c9" rotX={Math.PI / 2} />
      </Group>
      <Group p={[3.5, 0, 0.55]}>
        <B p={[0, 0.45, 0]} s={[0.8, 0.9, 0.7]} c={WHITE} r={0.4} />
        <Cyl p={[0, 0.5, 0.36]} rTop={0.22} rBottom={0.22} h={0.04} c="#9aa1a8" rotX={Math.PI / 2} />
      </Group>
      {/* support columns */}
      <Cyl p={[w / 2, 1.1, d / 2]} rTop={0.12} rBottom={0.12} h={2.2} c="#b6bac0" />
      {/* box pile */}
      <Group p={[w - 1.6, 0, d - 1.6]}>
        <B p={[0, 0.3, 0]} s={[0.8, 0.6, 0.8]} c="#c89a6b" />
        <B p={[0.1, 0.85, 0.05]} s={[0.6, 0.5, 0.6]} c="#b8854f" rotY={0.35} />
        <B p={[-0.7, 0.25, 0.2]} s={[0.55, 0.5, 0.55]} c="#d3a979" rotY={-0.2} />
      </Group>
      {/* old sofa with a dust sheet */}
      <Group p={[w - 2.2, 0, 1.1]}>
        <B p={[0, 0.4, 0]} s={[1.8, 0.8, 0.9]} c="#e7e2d8" r={0.95} />
        <B p={[0, 0.85, -0.25]} s={[1.85, 0.25, 0.5]} c="#ddd6c9" r={0.95} />
      </Group>
    </>
  );
}

function Attic({ w, d }: Rect) {
  return (
    <>
      {/* memory chest */}
      <Group p={[w / 2 - 1.5, 0, d / 2]}>
        <B p={[0, 0.3, 0]} s={[1.1, 0.6, 0.65]} c="#8a5f3a" />
        <B p={[0, 0.64, 0]} s={[1.14, 0.12, 0.69]} c="#6e4a2c" />
        <B p={[0, 0.42, 0.34]} s={[0.12, 0.18, 0.04]} c="#d8b25c" />
      </Group>
      {/* dusty boxes near the gable */}
      <Group p={[1.4, 0, 1.6]}>
        <B p={[0, 0.28, 0]} s={[0.7, 0.56, 0.7]} c="#c89a6b" rotY={0.2} />
        <B p={[0.5, 0.22, 0.5]} s={[0.5, 0.44, 0.5]} c="#d3a979" rotY={-0.3} />
      </Group>
      {/* standing lamp */}
      <Group p={[w - 2.0, 0, d / 2 - 0.6]}>
        <Cyl p={[0, 0.7, 0]} rTop={0.03} rBottom={0.16} h={1.4} c="#7a5f40" />
        <Cyl p={[0, 1.5, 0]} rTop={0.16} rBottom={0.3} h={0.3} c="#f2c14e" />
      </Group>
      {/* leaning mirror */}
      <Group p={[w - 1.2, 0, 1.2]} rotY={0.4}>
        <B p={[0, 0.8, 0]} s={[0.7, 1.6, 0.08]} c={WOOD_DARK} />
        <B p={[0, 0.8, 0.045]} s={[0.55, 1.42, 0.02]} c="#bcd6e2" r={0.1} />
      </Group>
    </>
  );
}

function Generic({ w, d }: Rect) {
  return (
    <>
      <Group p={[w / 2, 0, d / 2]}>
        <Cyl p={[0, 0.25, 0]} rTop={0.5} rBottom={0.6} h={0.5} c="#cdc3b2" />
      </Group>
      <Group p={[0.6, 0, 0.6]}>
        <B p={[0, 0.3, 0]} s={[0.6, 0.6, 0.6]} c="#c89a6b" rotY={0.3} />
      </Group>
    </>
  );
}

const SETS: Record<FurnitureKind, (rect: Rect) => JSX.Element> = {
  living: Living,
  kitchen: Kitchen,
  dining: Dining,
  bathroom: Bathroom,
  bedroom: Bedroom,
  office: Office,
  storage: Storage,
  garage: Garage,
  basement: Basement,
  attic: Attic,
  generic: Generic,
};

export function Furniture({ kind, rect }: { kind: FurnitureKind; rect: Rect }) {
  const Set = SETS[kind];
  return <Set {...rect} />;
}
