import type { ReactNode } from 'react';

// Compact primitive helpers - the whole house is assembled from these.

interface BoxProps {
  p: [number, number, number];
  s: [number, number, number];
  c: string;
  r?: number; // roughness
  rotY?: number;
  emissive?: string;
  emissiveIntensity?: number;
  opacity?: number;
}

/** Shadow-casting box: position `p`, size `s`, color `c`. */
export function B({ p, s, c, r = 0.85, rotY = 0, emissive, emissiveIntensity, opacity }: BoxProps) {
  return (
    <mesh position={p} rotation={[0, rotY, 0]} castShadow receiveShadow>
      <boxGeometry args={s} />
      <meshStandardMaterial
        color={c}
        roughness={r}
        emissive={emissive ?? '#000000'}
        emissiveIntensity={emissiveIntensity ?? 0}
        transparent={opacity !== undefined}
        opacity={opacity ?? 1}
      />
    </mesh>
  );
}

interface CylProps {
  p: [number, number, number];
  rTop: number;
  rBottom: number;
  h: number;
  c: string;
  seg?: number;
  rough?: number;
  rotZ?: number;
  rotX?: number;
  opacity?: number;
}

/** Shadow-casting cylinder. */
export function Cyl({ p, rTop, rBottom, h, c, seg = 20, rough = 0.8, rotZ = 0, rotX = 0, opacity }: CylProps) {
  return (
    <mesh position={p} rotation={[rotX, 0, rotZ]} castShadow receiveShadow>
      <cylinderGeometry args={[rTop, rBottom, h, seg]} />
      <meshStandardMaterial color={c} roughness={rough} transparent={opacity !== undefined} opacity={opacity ?? 1} />
    </mesh>
  );
}

/** Sphere blob, used for foliage and soft shapes. */
export function Blob({ p, r, c, scale }: { p: [number, number, number]; r: number; c: string; scale?: [number, number, number] }) {
  return (
    <mesh position={p} scale={scale ?? [1, 1, 1]} castShadow>
      <sphereGeometry args={[r, 20, 16]} />
      <meshStandardMaterial color={c} roughness={0.9} />
    </mesh>
  );
}

export function Group({ p, rotY = 0, children }: { p: [number, number, number]; rotY?: number; children: ReactNode }) {
  return (
    <group position={p} rotation={[0, rotY, 0]}>
      {children}
    </group>
  );
}

/** Shifts a "#RRGGBB" hex colour's channels by `by` (positive lightens, negative darkens). */
export function shift(hex: string, by: number): string {
  const n = parseInt(hex.slice(1, 7), 16);
  if (Number.isNaN(n)) return hex;
  const clamp = (v: number) => Math.min(255, Math.max(0, v));
  const r = clamp(((n >> 16) & 0xff) + by);
  const g = clamp(((n >> 8) & 0xff) + by);
  const b = clamp((n & 0xff) + by);
  return `rgb(${r},${g},${b})`;
}

export function lighten(hex: string, by = 24): string {
  return shift(hex, by);
}

export function darken(hex: string, by = 28): string {
  return shift(hex, -by);
}
