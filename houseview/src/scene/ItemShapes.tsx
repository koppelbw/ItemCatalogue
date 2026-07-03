import { B, Cyl, shift } from './primitives';

// Full-size assets for Furniture-tagged room items that represent real
// appliances: a washer reads as a washer, not as a holographic glyph. Items
// carry no dimensions in the database, so each shape is built at typical
// real-world size using the shared scales (plan 24 in/unit, vertical 40
// in/unit). Shapes stand on the floor centered on the item's display spot and
// face south (+z), matching the container-shape convention. Anything without
// a custom shape here keeps the default pedestal marker.

export type ItemShapeKind = 'washer' | 'dryer' | 'waterHeater';

/** name-match table; the LAST keyword in the name wins (head noun decides) */
const NAME_ITEM_SHAPES: [string, ItemShapeKind][] = [
  ['washer', 'washer'],
  ['washing machine', 'washer'],
  ['dryer', 'dryer'],
  ['water heater', 'waterHeater'],
];

export function itemShapeFor(name: string): ItemShapeKind | null {
  const key = name.trim().toLowerCase();
  let best: ItemShapeKind | null = null;
  let bestAt = -1;
  for (const [match, kind] of NAME_ITEM_SHAPES) {
    const at = key.lastIndexOf(match);
    if (at > bestAt) {
      bestAt = at;
      best = kind;
    }
  }
  return best;
}

/** overall asset height in scene units, for hover-tip placement */
export const ITEM_SHAPE_HEIGHTS: Record<ItemShapeKind, number> = {
  washer: 0.95,
  dryer: 0.95,
  waterHeater: 1.85,
};

interface ItemShapeProps {
  kind: ItemShapeKind;
  /** hovered or selected — brightens every part */
  active: boolean;
}

export function ItemShape({ kind, active }: ItemShapeProps) {
  switch (kind) {
    case 'washer':
      return <WasherShape active={active} />;
    case 'dryer':
      return <DryerShape active={active} />;
    case 'waterHeater':
      return <WaterHeaterShape active={active} />;
  }
}

/** part tone helper: channel shift from a base color, hover-aware */
const toner = (base: string, active: boolean) => (by: number) => shift(base, by + (active ? 22 : 0));

/** shared laundry-machine cabinet: 27w × 28d × 38h inches */
const MACHINE = { w: 27 / 24, d: 28 / 24, h: 38 / 40 };

function MachineBody({ tone }: { tone: (by: number) => string }) {
  const { w, d, h } = MACHINE;
  return (
    <>
      <B p={[0, h / 2, 0]} s={[w, h, d]} c={tone(0)} r={0.35} />
      {/* top work surface + toe kick */}
      <B p={[0, h - 0.025, 0]} s={[w + 0.02, 0.05, d + 0.02]} c={tone(-14)} r={0.3} />
      <B p={[0, 0.035, d / 2 - 0.02]} s={[w * 0.94, 0.07, 0.05]} c={tone(-60)} />
      {/* control panel across the top front */}
      <B p={[0, h - 0.115, d / 2 + 0.006]} s={[w * 0.92, 0.11, 0.025]} c={tone(-24)} r={0.4} />
    </>
  );
}

/** Front-load washer: porthole with blue glass, detergent drawer, control knob. */
function WasherShape({ active }: { active: boolean }) {
  const tone = toner('#e9eaec', active);
  const { w, d, h } = MACHINE;
  const doorZ = d / 2;
  return (
    <group>
      <MachineBody tone={tone} />
      {/* detergent drawer + knob on the panel */}
      <B p={[-w * 0.3, h - 0.115, doorZ + 0.02]} s={[w * 0.24, 0.07, 0.02]} c={tone(-6)} />
      <Cyl p={[w * 0.28, h - 0.115, doorZ + 0.025]} rTop={0.05} rBottom={0.05} h={0.035} c={tone(-40)} rotX={Math.PI / 2} />
      {/* porthole: dark drum, chrome ring, glass bubble */}
      <Cyl p={[0, h * 0.44, doorZ + 0.005]} rTop={0.42} rBottom={0.42} h={0.03} c={tone(-46)} rotX={Math.PI / 2} rough={0.35} />
      <Cyl p={[0, h * 0.44, doorZ + 0.02]} rTop={0.3} rBottom={0.3} h={0.035} c="#33414e" rotX={Math.PI / 2} rough={0.3} />
      <Cyl p={[0, h * 0.44, doorZ + 0.035]} rTop={0.24} rBottom={0.24} h={0.03} c="#a9c8de" rotX={Math.PI / 2} rough={0.15} />
    </group>
  );
}

/** Dryer: matching cabinet with a solid door, handle and program dial. */
function DryerShape({ active }: { active: boolean }) {
  const tone = toner('#edebe6', active);
  const { w, d, h } = MACHINE;
  const doorZ = d / 2;
  return (
    <group>
      <MachineBody tone={tone} />
      {/* program dial + start button */}
      <Cyl p={[w * 0.28, h - 0.115, doorZ + 0.03]} rTop={0.07} rBottom={0.07} h={0.045} c={tone(-44)} rotX={Math.PI / 2} />
      <Cyl p={[-w * 0.3, h - 0.115, doorZ + 0.02]} rTop={0.035} rBottom={0.035} h={0.03} c={tone(-30)} rotX={Math.PI / 2} />
      {/* solid door with recessed face and a handle notch */}
      <Cyl p={[0, h * 0.44, doorZ + 0.005]} rTop={0.42} rBottom={0.42} h={0.03} c={tone(-20)} rotX={Math.PI / 2} rough={0.4} />
      <Cyl p={[0, h * 0.44, doorZ + 0.02]} rTop={0.34} rBottom={0.34} h={0.025} c={tone(-8)} rotX={Math.PI / 2} rough={0.5} />
      <B p={[0, h * 0.44 + 0.3, doorZ + 0.035]} s={[0.22, 0.035, 0.025]} c={tone(-48)} />
    </group>
  );
}

/** 40-gallon tank water heater: 24"-diameter cylinder with pipes and a panel. */
function WaterHeaterShape({ active }: { active: boolean }) {
  const tone = toner('#e2e3e6', active);
  const r = 0.5; // 24" diameter
  const tankH = 60 / 40;
  return (
    <group>
      {/* drain pan, tank, domed cap */}
      <Cyl p={[0, 0.025, 0]} rTop={r + 0.06} rBottom={r + 0.08} h={0.05} c={tone(-52)} rough={0.6} />
      <Cyl p={[0, 0.05 + tankH / 2, 0]} rTop={r} rBottom={r} h={tankH} c={tone(0)} seg={28} rough={0.45} />
      <Cyl p={[0, 0.05 + tankH + 0.05, 0]} rTop={r * 0.55} rBottom={r} h={0.12} c={tone(-10)} seg={28} rough={0.45} />
      {/* cold/hot lines up top: one copper, one steel */}
      <Cyl p={[0.18, 0.05 + tankH + 0.22, 0]} rTop={0.04} rBottom={0.04} h={0.28} c="#b4714a" rough={0.4} />
      <Cyl p={[-0.18, 0.05 + tankH + 0.22, 0]} rTop={0.04} rBottom={0.04} h={0.28} c={tone(-36)} rough={0.4} />
      {/* burner access panel + rating label on the south face */}
      <B p={[0, 0.32, r - 0.04]} s={[0.3, 0.24, 0.1]} c={tone(-22)} r={0.5} />
      <B p={[0, 0.95, r - 0.02]} s={[0.26, 0.34, 0.06]} c={tone(28)} r={0.7} />
    </group>
  );
}
