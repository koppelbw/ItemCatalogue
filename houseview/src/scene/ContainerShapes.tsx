import type { ContainerShapeKind } from '../layout';
import { B, Cyl, shift } from './primitives';

// Distinct primitive silhouettes for containers that are really furniture
// (beds, sofas, tables, dressers, wardrobes) rather than generic storage.
// Every shape is built purely from the container's catalogued dimensions
// (w × h × d) and colour — nothing hardcoded per instance — so what renders
// stays true to the database row, just more legible than a single box.
//
// Colour handling: parts derive from the RAW "#RRGGBB" colour with a single
// channel shift, and hover/selection brightening folds into that same shift
// (shift() cannot re-parse its own "rgb()" output, so never shift twice).

interface ContainerShapeProps {
  kind: ContainerShapeKind;
  w: number;
  h: number;
  d: number;
  /** raw stored colour, "#RRGGBB" */
  color: string;
  /** hovered or selected — brightens every part */
  active: boolean;
}

interface ShapeProps {
  w: number;
  h: number;
  d: number;
  /** part tone: channel shift relative to the stored colour, hover-aware */
  tone: (by: number) => string;
}

export function ContainerShape({ kind, w, h, d, color, active }: ContainerShapeProps) {
  const tone = (by: number) => shift(color, by + (active ? 26 : 0));
  const props: ShapeProps = { w, h, d, tone };
  switch (kind) {
    case 'bed':
      return <BedShape {...props} />;
    case 'seating':
      return <SeatingShape {...props} />;
    case 'table':
      return <TableShape {...props} />;
    case 'dresser':
      return <DresserShape {...props} />;
    case 'wardrobe':
      return <WardrobeShape {...props} />;
    case 'piano':
      return <GrandPianoShape {...props} />;
    default:
      return <BoxShape {...props} />;
  }
}

function BoxShape({ w, h, d, tone }: ShapeProps) {
  return (
    <>
      <B p={[w / 2, h / 2, d / 2]} s={[w, h, d]} c={tone(0)} r={0.75} />
      {/* lid line so it reads as storage rather than furniture; straddles the top
          edge so its faces never sit coplanar with the box's (z-fighting) */}
      <B p={[w / 2, h + 0.005, d / 2]} s={[w + 0.03, 0.04, d + 0.03]} c={tone(-28)} r={0.6} />
    </>
  );
}

/** Frame, mattress, two pillows and a headboard along the container's north (z = 0) edge. */
function BedShape({ w, h, d, tone }: ShapeProps) {
  const frameH = h * 0.35;
  const mattressH = h * 0.45;
  return (
    <>
      <B p={[w / 2, frameH / 2, d / 2]} s={[w * 0.96, frameH, d * 0.96]} c={tone(-22)} />
      <B p={[w / 2, frameH + mattressH / 2, d / 2]} s={[w * 0.9, mattressH, d * 0.92]} c={tone(0)} r={0.9} />
      {/* blanket over the foot two-thirds */}
      <B p={[w / 2, frameH + mattressH + 0.02, d * 0.63]} s={[w * 0.9, 0.05, d * 0.62]} c={tone(-12)} r={0.95} />
      <B p={[w * 0.28, frameH + mattressH + 0.045, d * 0.14]} s={[w * 0.36, 0.09, d * 0.18]} c={tone(42)} r={0.95} />
      <B p={[w * 0.72, frameH + mattressH + 0.045, d * 0.14]} s={[w * 0.36, 0.09, d * 0.18]} c={tone(42)} r={0.95} />
      {/* headboard rises past the mattress so the silhouette reads as a bed */}
      <B p={[w / 2, h * 0.75, d * 0.03]} s={[w * 0.96, h * 1.5, d * 0.06]} c={tone(-34)} />
    </>
  );
}

/** Seat cushion with a backrest along the north edge and armrests at the sides. */
function SeatingShape({ w, h, d, tone }: ShapeProps) {
  const seatH = h * 0.45;
  return (
    <>
      <B p={[w / 2, seatH / 2, d * 0.55]} s={[w * 0.88, seatH, d * 0.8]} c={tone(0)} r={0.9} />
      <B p={[w / 2, h * 0.5, d * 0.11]} s={[w * 0.9, h, d * 0.22]} c={tone(-18)} r={0.9} />
      <B p={[w * 0.06, h * 0.38, d * 0.5]} s={[w * 0.12, h * 0.76, d * 0.9]} c={tone(-18)} r={0.9} />
      <B p={[w * 0.94, h * 0.38, d * 0.5]} s={[w * 0.12, h * 0.76, d * 0.9]} c={tone(-18)} r={0.9} />
    </>
  );
}

/** Thin top on four corner legs — also used for desks. */
function TableShape({ w, h, d, tone }: ShapeProps) {
  const topH = Math.max(0.05, h * 0.1);
  const legR = Math.max(0.025, Math.min(w, d) * 0.045);
  const legH = h - topH;
  const inset = Math.min(w, d) * 0.14;
  const legs: [number, number][] = [
    [inset, inset],
    [w - inset, inset],
    [inset, d - inset],
    [w - inset, d - inset],
  ];
  return (
    <>
      <B p={[w / 2, h - topH / 2, d / 2]} s={[w, topH, d]} c={tone(0)} r={0.7} />
      {legs.map(([x, z], i) => (
        <Cyl key={i} p={[x, legH / 2, z]} rTop={legR} rBottom={legR} h={legH} c={tone(-24)} />
      ))}
    </>
  );
}

/** Storage box with drawer fronts and knobs on the south face (toward the camera). */
function DresserShape({ w, h, d, tone }: ShapeProps) {
  const rows = Math.max(2, Math.min(4, Math.round(h / 0.3)));
  const rowH = h / rows;
  return (
    <>
      <B p={[w / 2, h / 2, d / 2]} s={[w, h, d]} c={tone(0)} r={0.75} />
      {Array.from({ length: rows }, (_, i) => {
        const cy = (i + 0.5) * rowH;
        return (
          <group key={i}>
            <B p={[w / 2, cy, d + 0.005]} s={[w * 0.86, rowH * 0.6, 0.02]} c={tone(-20)} r={0.7} />
            <B p={[w / 2, cy, d + 0.02]} s={[Math.min(0.12, w * 0.2), 0.035, 0.02]} c={tone(-44)} />
          </group>
        );
      })}
    </>
  );
}

/**
 * Grand piano: body slab with a rounded tail at the back, keyboard along the
 * south face, lid propped open toward the east, three round legs and a bench.
 */
function GrandPianoShape({ w, h, d, tone }: ShapeProps) {
  const legH = h * 0.5;
  const bodyH = h - legH;
  const legR = Math.max(0.03, Math.min(w, d) * 0.05);
  const tailR = Math.min(w / 2, d * 0.4);
  return (
    <>
      {/* straight front half of the case + rounded tail */}
      <B p={[w / 2, legH + bodyH / 2, d * 0.7]} s={[w, bodyH, d * 0.6]} c={tone(0)} r={0.4} />
      <Cyl p={[w / 2, legH + bodyH / 2, d * 0.4]} rTop={tailR} rBottom={tailR} h={bodyH} c={tone(0)} rough={0.4} seg={28} />
      {/* lid, hinged on the west rim and propped open */}
      <group position={[w * 0.08, h + 0.015, d * 0.45]} rotation={[0, 0, 0.4]}>
        <B p={[w * 0.42, 0, 0]} s={[w * 0.84, 0.035, d * 0.62]} c={tone(-14)} r={0.3} />
      </group>
      {/* keyboard shelf: white keys under a dark fascia, cheek blocks at the ends */}
      <B p={[w / 2, legH + 0.05, d + 0.07]} s={[w * 0.8, 0.09, 0.16]} c={tone(200)} r={0.5} />
      <B p={[w / 2, legH + 0.13, d - 0.015]} s={[w * 0.86, 0.07, 0.05]} c={tone(-18)} r={0.5} />
      <B p={[w * 0.08, legH + 0.06, d + 0.06]} s={[w * 0.06, 0.12, 0.18]} c={tone(-10)} r={0.5} />
      <B p={[w * 0.92, legH + 0.06, d + 0.06]} s={[w * 0.06, 0.12, 0.18]} c={tone(-10)} r={0.5} />
      {/* legs (two front, one under the tail) and the pedal lyre */}
      <Cyl p={[w * 0.12, legH / 2, d * 0.9]} rTop={legR} rBottom={legR} h={legH} c={tone(-12)} />
      <Cyl p={[w * 0.88, legH / 2, d * 0.9]} rTop={legR} rBottom={legR} h={legH} c={tone(-12)} />
      <Cyl p={[w / 2, legH / 2, d * 0.14]} rTop={legR} rBottom={legR} h={legH} c={tone(-12)} />
      <B p={[w / 2, legH * 0.45, d * 0.86]} s={[0.06, legH * 0.9, 0.05]} c={tone(-12)} />
      <B p={[w / 2, legH * 0.08, d * 0.88]} s={[0.22, 0.04, 0.1]} c={tone(24)} />
      {/* bench at the keyboard */}
      <B p={[w / 2, legH * 0.85, d + 0.42]} s={[w * 0.42, 0.06, 0.34]} c={tone(-8)} r={0.5} />
      <B p={[w / 2 - w * 0.17, legH * 0.41, d + 0.42]} s={[0.05, legH * 0.82, 0.26]} c={tone(-20)} />
      <B p={[w / 2 + w * 0.17, legH * 0.41, d + 0.42]} s={[0.05, legH * 0.82, 0.26]} c={tone(-20)} />
    </>
  );
}

/** Tall cabinet with a centre door seam and handles on the south face. */
function WardrobeShape({ w, h, d, tone }: ShapeProps) {
  return (
    <>
      <B p={[w / 2, h / 2, d / 2]} s={[w, h, d]} c={tone(0)} r={0.75} />
      {/* slightly proud top cornice; straddles the top edge so its faces never
          sit coplanar with the cabinet's (z-fighting) */}
      <B p={[w / 2, h + 0.005, d / 2]} s={[w + 0.04, 0.04, d + 0.04]} c={tone(-24)} r={0.6} />
      {/* door seam + handles */}
      <B p={[w / 2, h * 0.48, d + 0.005]} s={[0.015, h * 0.9, 0.02]} c={tone(-34)} />
      <B p={[w / 2 - 0.045, h * 0.5, d + 0.015]} s={[0.03, Math.min(0.28, h * 0.2), 0.025]} c={tone(-44)} />
      <B p={[w / 2 + 0.045, h * 0.5, d + 0.015]} s={[0.03, Math.min(0.28, h * 0.2), 0.025]} c={tone(-44)} />
    </>
  );
}
