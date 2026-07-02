import { Html } from '@react-three/drei';
import gsap from 'gsap';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Shape, type Group as ThreeGroup } from 'three';
import { SITE_INTERIOR } from '../layout';
import type { Site } from '../model';
import { B, Blob, Cyl, Group } from './primitives';

// Every database Location is its own little diorama in the neighbourhood.
// Non-active locations stand as fully closed buildings - four walls, a roof,
// a front door and lit windows - so only the central dollhouse ever shows its
// interior. Each building shares the same bones (pad, body, nameplate) and
// gets a shell that gives it a silhouette of its own.

export interface SiteBuildingProps {
  site: Site;
  index: number;
  active: boolean;
  onSelectSite: (key: string) => void;
}

interface ShellPalette {
  wall: string;
  floor: string;
  accent: string;
}

const PALETTES: Record<string, ShellPalette> = {
  apartment: { wall: '#dfe7ee', floor: '#d9c2a0', accent: '#3ec6b8' },
  cottage: { wall: '#f3e6cf', floor: '#d9c2a0', accent: '#ef6f6c' },
  storage: { wall: '#c9cdd2', floor: '#b6bac0', accent: '#e8893c' },
  cabin: { wall: '#e7dfd0', floor: '#cdc3b2', accent: '#8c97a5' },
  car: { wall: '#7fa8c9', floor: '#9a9a9e', accent: '#5b8def' },
};

/** Triangular gable filling the space between the wall top and a pitched roof. */
function Gable({ p, width, rise, color }: { p: [number, number, number]; width: number; rise: number; color: string }) {
  const shape = useMemo(() => {
    const s = new Shape();
    s.moveTo(-width / 2, 0);
    s.lineTo(width / 2, 0);
    s.lineTo(0, rise);
    s.closePath();
    return s;
  }, [width, rise]);
  return (
    <mesh position={p} rotation={[0, Math.PI / 2, 0]} castShadow receiveShadow>
      <extrudeGeometry args={[shape, { depth: 0.14, bevelEnabled: false }]} />
      <meshStandardMaterial color={color} roughness={0.95} />
    </mesh>
  );
}

/** Framed, warmly lit window flush against a wall face. Thin axis picks the face. */
function WindowPane({ p, facing, w = 0.84, h = 0.66 }: { p: [number, number, number]; facing: 'south' | 'east'; w?: number; h?: number }) {
  const frame: [number, number, number] = facing === 'south' ? [w + 0.16, h + 0.16, 0.06] : [0.06, h + 0.16, w + 0.16];
  const glass: [number, number, number] = facing === 'south' ? [w, h, 0.06] : [0.06, h, w];
  const off: [number, number, number] = facing === 'south' ? [p[0], p[1], p[2] + 0.03] : [p[0] + 0.03, p[1], p[2]];
  return (
    <>
      <B p={p} s={frame} c="#f7f4ee" />
      <B p={off} s={glass} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.5} />
    </>
  );
}

interface SiteBaseProps extends SiteBuildingProps {
  palette: ShellPalette;
  wallH: number;
  /** custom silhouette replacing the default closed four-wall box */
  body?: (hovered: boolean) => ReactNode;
  /** decorative shell rendered around/above the closed body */
  children?: ReactNode;
}

function SiteBase({ site, index, active, onSelectSite, palette, wallH, body, children }: SiteBaseProps) {
  const { w, d } = SITE_INTERIOR;
  const groupRef = useRef<ThreeGroup>(null);
  const [hovered, setHovered] = useState(false);

  // pop in after the main house intro
  useEffect(() => {
    const g = groupRef.current;
    if (!g) return;
    gsap.fromTo(
      g.scale,
      { x: 0.001, y: 0.001, z: 0.001 },
      { x: 1, y: 1, z: 1, duration: 0.7, delay: 1.0 + index * 0.14, ease: 'back.out(1.5)' },
    );
  }, [index]);

  const wallTint = hovered || active ? '#ffffff' : palette.wall;

  return (
    <group
      ref={groupRef}
      position={[site.def.origin[0], 0, site.def.origin[1]]}
      onClick={(e) => {
        e.stopPropagation();
        onSelectSite(site.key);
      }}
      onPointerOver={(e) => {
        e.stopPropagation();
        setHovered(true);
        document.body.style.cursor = 'pointer';
      }}
      onPointerOut={() => {
        setHovered(false);
        document.body.style.cursor = 'auto';
      }}
    >
      {/* foundation pad */}
      <B p={[w / 2, 0.07, d / 2]} s={[w + 0.9, 0.18, d + 0.9]} c="#b3aa99" r={1} />

      {/* active halo on the lawn */}
      {active && (
        <mesh position={[w / 2, 0.02, d / 2]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[Math.max(w, d) * 0.82, Math.max(w, d) * 0.9, 48]} />
          <meshBasicMaterial color={palette.accent} transparent opacity={0.45} depthWrite={false} />
        </mesh>
      )}

      {/* closed building body + decorative shell */}
      <group position={[0, 0.3, 0]}>
        {body ? (
          body(hovered || active)
        ) : (
          <mesh position={[w / 2, wallH / 2, d / 2]} castShadow receiveShadow>
            <boxGeometry args={[w, wallH, d]} />
            <meshStandardMaterial color={wallTint} roughness={0.95} />
          </mesh>
        )}
        {children}
      </group>

      {/* nameplate */}
      <Html position={[0.3, wallH + 1.0, 0.3]} zIndexRange={[12, 0]} style={{ pointerEvents: 'none' }}>
        <div className={`room-tag site-tag${hovered || active ? ' room-tag-active' : ''}`}>
          <span>{site.label}</span>
          {site.items.length > 0 && <em>{site.items.length}</em>}
        </div>
      </Html>
    </group>
  );
}

function ApartmentShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  return (
    <>
      {/* flat roof slab over the main body */}
      <B p={[w / 2, wallH + 0.04, d / 2]} s={[w + 0.3, 0.12, d + 0.3]} c="#aebccb" />
      {/* set-back upper storey along the north edge */}
      <B p={[w / 2, wallH + 1.15, 0.95]} s={[w + 0.24, 2.1, 1.9]} c="#cdd9e4" r={0.8} />
      {/* glowing window band on the upper storey */}
      <B p={[w / 2, wallH + 1.15, 1.92]} s={[w - 0.6, 0.7, 0.06]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.55} />
      {/* roof lip + rooftop AC unit */}
      <B p={[w / 2, wallH + 2.26, 0.95]} s={[w + 0.44, 0.12, 2.1]} c="#aebccb" />
      <B p={[w / 2 + 1.2, wallH + 2.55, 0.8]} s={[0.6, 0.45, 0.6]} c="#9aa1a8" r={0.5} />
      {/* front entrance with awning */}
      <B p={[w / 2, 0.62, d + 0.05]} s={[0.85, 1.24, 0.1]} c="#37474f" r={0.6} />
      <B p={[w / 2, 1.5, d + 0.3]} s={[1.15, 0.07, 0.75]} c="#3ec6b8" />
      {/* lit windows flanking the door + on the east face */}
      <WindowPane p={[0.95, 1.5, d]} facing="south" />
      <WindowPane p={[w - 0.95, 1.5, d]} facing="south" />
      <WindowPane p={[w, 1.5, 1.2]} facing="east" />
      <WindowPane p={[w, 1.5, 2.9]} facing="east" />
    </>
  );
}

function CottageShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const halfDepth = d / 2;
  const rise = 1.15;
  const angle = Math.atan2(rise, halfDepth);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.45;
  return (
    <>
      {/* full gabled roof */}
      <mesh position={[w / 2, wallH + rise / 2, d / 4]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.7, 0.12, panelLen]} />
        <meshStandardMaterial color="#b86b4b" roughness={0.85} />
      </mesh>
      <mesh position={[w / 2, wallH + rise / 2, (3 * d) / 4]} rotation={[angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.7, 0.12, panelLen]} />
        <meshStandardMaterial color="#b86b4b" roughness={0.85} />
      </mesh>
      <B p={[w / 2, wallH + rise + 0.04, d / 2]} s={[w + 0.9, 0.16, 0.26]} c="#9c5639" />
      <Gable p={[w - 0.07, wallH, d / 2]} width={d} rise={rise} color="#efe0c6" />
      <Gable p={[-0.07, wallH, d / 2]} width={d} rise={rise} color="#efe0c6" />
      {/* chimney */}
      <Group p={[w - 1.0, wallH + 0.55, 0.9]}>
        <B p={[0, 0.4, 0]} s={[0.5, 1.1, 0.5]} c="#b08a8a" />
        <B p={[0, 1.0, 0]} s={[0.62, 0.12, 0.62]} c="#8d6d6d" />
      </Group>
      {/* front door + lit windows */}
      <B p={[1.35, 0.62, d + 0.05]} s={[0.78, 1.24, 0.1]} c="#8a5f3a" r={0.6} />
      <B p={[1.62, 0.58, d + 0.11]} s={[0.07, 0.07, 0.05]} c="#f2c14e" />
      <WindowPane p={[3.15, 1.3, d]} facing="south" />
      <WindowPane p={[w, 1.3, 1.5]} facing="east" />
      {/* flower bushes by the entrance */}
      <Blob p={[-0.6, 0.25, d - 0.5]} r={0.35} c="#6fae72" scale={[1.2, 0.7, 1]} />
      <Blob p={[-0.55, 0.45, d - 0.55]} r={0.12} c="#ef6f6c" />
      <Blob p={[w + 0.55, 0.22, d - 1.4]} r={0.3} c="#549058" scale={[1.1, 0.7, 1]} />
    </>
  );
}

function StorageShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const ribs = Math.floor(w / 0.62);
  const doorH = wallH - 0.55;
  return (
    <>
      {/* corrugated ribs on the north and south walls */}
      {Array.from({ length: ribs }, (_, i) => (
        <B key={`n${i}`} p={[0.5 + i * 0.62, wallH / 2, -0.05]} s={[0.12, wallH, 0.08]} c="#aeb4ba" />
      ))}
      {Array.from({ length: ribs }, (_, i) => (
        <B key={`s${i}`} p={[0.5 + i * 0.62, wallH / 2, d + 0.05]} s={[0.12, wallH, 0.08]} c="#aeb4ba" />
      ))}
      {/* flat metal roof with an accent fascia band under it */}
      <B p={[w / 2, wallH + 0.07, d / 2]} s={[w + 0.5, 0.12, d + 0.5]} c="#8e959c" />
      <B p={[w / 2, wallH - 0.06, d / 2]} s={[w + 0.2, 0.16, d + 0.2]} c="#e8893c" />
      {/* closed roll-up door on the east face */}
      <B p={[w + 0.05, doorH / 2, d / 2]} s={[0.1, doorH, 2.8]} c="#dde1e5" />
      {Array.from({ length: 4 }, (_, j) => (
        <B key={`slat${j}`} p={[w + 0.11, 0.45 + j * 0.42, d / 2]} s={[0.02, 0.05, 2.8]} c="#b7bcc2" />
      ))}
      <B p={[w + 0.12, 0.26, d / 2]} s={[0.04, 0.08, 0.6]} c="#7d838a" />
    </>
  );
}

function CabinShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const halfDepth = d / 2;
  const rise = 0.9;
  const angle = Math.atan2(rise, halfDepth);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.4;
  return (
    <>
      {/* full gabled roof */}
      <mesh position={[w / 2, wallH + rise / 2, d / 4]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.5, 0.12, panelLen]} />
        <meshStandardMaterial color="#8a7355" roughness={0.9} />
      </mesh>
      <mesh position={[w / 2, wallH + rise / 2, (3 * d) / 4]} rotation={[angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.5, 0.12, panelLen]} />
        <meshStandardMaterial color="#8a7355" roughness={0.9} />
      </mesh>
      <B p={[w / 2, wallH + rise + 0.03, d / 2]} s={[w + 0.7, 0.14, 0.24]} c="#6f5a41" />
      <Gable p={[w - 0.07, wallH, d / 2]} width={d} rise={rise} color="#ded2bd" />
      <Gable p={[-0.07, wallH, d / 2]} width={d} rise={rise} color="#ded2bd" />
      {/* front door + lit window */}
      <B p={[w / 2 - 0.9, 0.58, d + 0.05]} s={[0.72, 1.16, 0.1]} c="#6e5233" r={0.7} />
      <WindowPane p={[w / 2 + 1.0, 1.25, d]} facing="south" w={0.7} h={0.6} />
      {/* stump and firewood by the walls */}
      <Cyl p={[-0.5, 0.4, 0.6]} rTop={0.16} rBottom={0.2} h={0.8} c="#8a5f3a" />
      <Cyl p={[w + 0.35, 0.18, 2.7]} rTop={0.14} rBottom={0.14} h={0.9} c="#8a5f3a" rotX={Math.PI / 2} />
      <Cyl p={[w + 0.35, 0.44, 2.7]} rTop={0.13} rBottom={0.13} h={0.8} c="#7a5334" rotX={Math.PI / 2} />
    </>
  );
}

function Wheel({ p }: { p: [number, number, number] }) {
  return (
    <group position={p} rotation={[0, 0, Math.PI / 2]}>
      <mesh castShadow>
        <cylinderGeometry args={[0.34, 0.34, 0.26, 24]} />
        <meshStandardMaterial color="#2e3338" roughness={0.9} />
      </mesh>
      <mesh>
        <cylinderGeometry args={[0.16, 0.16, 0.28, 16]} />
        <meshStandardMaterial color="#c9ced3" roughness={0.4} />
      </mesh>
    </group>
  );
}

/** The family wagon parked on its pad, nose pointing south (+z). */
function CarBody({ hovered }: { hovered: boolean }) {
  const { w, d } = SITE_INTERIOR;
  const body = hovered ? '#8fb6d4' : '#7fa8c9';
  const cabin = hovered ? '#8fb6d4' : '#6b93b4';
  const glass = '#cfe6f2';
  return (
    <group position={[w / 2, 0, d / 2]} scale={1.15}>
      {/* body */}
      <B p={[0, 0.62, 0]} s={[1.7, 0.6, 4.1]} c={body} r={0.45} />
      {/* cabin */}
      <B p={[0, 1.18, -0.25]} s={[1.5, 0.62, 2.3]} c={cabin} r={0.45} />
      {/* windows */}
      <B p={[0, 1.2, 0.95]} s={[1.36, 0.44, 0.06]} c={glass} r={0.15} opacity={0.85} />
      <B p={[0.73, 1.2, -0.25]} s={[0.06, 0.42, 2.0]} c={glass} r={0.15} opacity={0.85} />
      <B p={[-0.73, 1.2, -0.25]} s={[0.06, 0.42, 2.0]} c={glass} r={0.15} opacity={0.85} />
      {/* headlights + taillights */}
      <B p={[0.55, 0.62, 2.06]} s={[0.3, 0.14, 0.05]} c="#fff3c4" emissive="#ffe9a3" emissiveIntensity={0.8} />
      <B p={[-0.55, 0.62, 2.06]} s={[0.3, 0.14, 0.05]} c="#fff3c4" emissive="#ffe9a3" emissiveIntensity={0.8} />
      <B p={[0.55, 0.62, -2.06]} s={[0.3, 0.14, 0.05]} c="#d04f3a" emissive="#d04f3a" emissiveIntensity={0.5} />
      <B p={[-0.55, 0.62, -2.06]} s={[0.3, 0.14, 0.05]} c="#d04f3a" emissive="#d04f3a" emissiveIntensity={0.5} />
      {/* roof rails */}
      <B p={[0.6, 1.53, -0.25]} s={[0.07, 0.07, 2.1]} c="#9aa1a8" />
      <B p={[-0.6, 1.53, -0.25]} s={[0.07, 0.07, 2.1]} c="#9aa1a8" />
      <Wheel p={[0.85, 0.34, 1.35]} />
      <Wheel p={[-0.85, 0.34, 1.35]} />
      <Wheel p={[0.85, 0.34, -1.35]} />
      <Wheel p={[-0.85, 0.34, -1.35]} />
    </group>
  );
}

export function SiteBuilding(props: SiteBuildingProps) {
  switch (props.site.def.kind) {
    case 'apartment':
      return (
        <SiteBase {...props} palette={PALETTES.apartment} wallH={2.4}>
          <ApartmentShell wallH={2.4} />
        </SiteBase>
      );
    case 'cottage':
      return (
        <SiteBase {...props} palette={PALETTES.cottage} wallH={2.2}>
          <CottageShell wallH={2.2} />
        </SiteBase>
      );
    case 'storage':
      return (
        <SiteBase {...props} palette={PALETTES.storage} wallH={2.5}>
          <StorageShell wallH={2.5} />
        </SiteBase>
      );
    case 'car':
      return <SiteBase {...props} palette={PALETTES.car} wallH={1.9} body={(hovered) => <CarBody hovered={hovered} />} />;
    default:
      return (
        <SiteBase {...props} palette={PALETTES.cabin} wallH={2.1}>
          <CabinShell wallH={2.1} />
        </SiteBase>
      );
  }
}
