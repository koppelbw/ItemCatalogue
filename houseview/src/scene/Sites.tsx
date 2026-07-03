import { Html } from '@react-three/drei';
import gsap from 'gsap';
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Shape, type Group as ThreeGroup } from 'three';
import { SITE_INTERIOR } from '../layout';
import type { Site } from '../model';
import { B, Blob, Cyl, Group, lighten } from './primitives';

// Every database Location is its own little diorama in the neighborhood.
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
  apartment: { wall: '#ece4d4', floor: '#d9c2a0', accent: '#d9705c' },
  cottage: { wall: '#f3e6cf', floor: '#d9c2a0', accent: '#ef6f6c' },
  townhouse: { wall: '#9d5f4c', floor: '#d9c2a0', accent: '#2e6f63' },
  storage: { wall: '#c9cdd2', floor: '#b6bac0', accent: '#e8893c' },
  cabin: { wall: '#b7cdd9', floor: '#cdc3b2', accent: '#c94f4a' },
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

  const wallTint = hovered || active ? lighten(palette.wall, 36) : palette.wall;

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

/**
 * Small modern apartment block in warm tones: two storeys of windows with
 * mustard-fronted balconies, a coral stair tower over the entrance, and a
 * set-back penthouse with a glowing clerestory on the flat roof.
 */
function ApartmentShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const coral = '#d9705c';
  const mustard = '#e9b44c';
  const trim = '#f7f4ee';
  return (
    <>
      {/* flat roof slab + parapet lip */}
      <B p={[w / 2, wallH + 0.05, d / 2]} s={[w + 0.3, 0.12, d + 0.3]} c="#b6afa2" />
      <B p={[w / 2, wallH + 0.15, d / 2]} s={[w + 0.44, 0.1, d + 0.44]} c="#9d968a" />
      {/* set-back penthouse with a glowing clerestory + rooftop AC unit */}
      <B p={[w / 2, wallH + 0.9, 1.0]} s={[w - 0.7, 1.4, 1.9]} c="#ded4c3" r={0.8} />
      <B p={[w / 2, wallH + 1.0, 1.97]} s={[w - 1.4, 0.55, 0.06]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.55} />
      <B p={[w / 2, wallH + 1.66, 1.0]} s={[w - 0.5, 0.1, 2.1]} c="#9d968a" />
      <B p={[w - 1.0, wallH + 1.95, 0.9]} s={[0.55, 0.4, 0.55]} c="#9aa1a8" r={0.5} />
      {/* coral stair tower with the entrance, canopy and stacked landing windows */}
      <B p={[0.9, wallH / 2 + 0.2, d + 0.07]} s={[1.15, wallH + 0.4, 0.14]} c={coral} />
      <B p={[0.9, 0.62, d + 0.16]} s={[0.8, 1.24, 0.08]} c="#37474f" r={0.6} />
      <B p={[0.9, 1.52, d + 0.35]} s={[1.25, 0.07, 0.55]} c={mustard} />
      <B p={[0.9, 2.05, d + 0.16]} s={[0.55, 0.38, 0.05]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.5} />
      <B p={[0.9, 2.85, d + 0.16]} s={[0.55, 0.38, 0.05]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.5} />
      {/* two storeys of south windows; the upper pair opens onto balconies */}
      <WindowPane p={[2.55, 1.15, d]} facing="south" w={0.72} h={0.62} />
      <WindowPane p={[3.75, 1.15, d]} facing="south" w={0.72} h={0.62} />
      <WindowPane p={[2.55, 2.45, d]} facing="south" w={0.72} h={0.62} />
      <WindowPane p={[3.75, 2.45, d]} facing="south" w={0.72} h={0.62} />
      {[2.55, 3.75].map((x) => (
        <Group key={`balc${x}`} p={[x, 0, 0]}>
          <B p={[0, 2.02, d + 0.28]} s={[1.0, 0.08, 0.62]} c={trim} />
          <B p={[0, 2.22, d + 0.56]} s={[1.0, 0.34, 0.06]} c={mustard} />
          <B p={[-0.47, 2.22, d + 0.28]} s={[0.06, 0.34, 0.56]} c={trim} />
          <B p={[0.47, 2.22, d + 0.28]} s={[0.06, 0.34, 0.56]} c={trim} />
        </Group>
      ))}
      {/* mustard awnings over the ground-floor windows */}
      <B p={[2.55, 1.58, d + 0.2]} s={[0.98, 0.06, 0.44]} c={mustard} />
      <B p={[3.75, 1.58, d + 0.2]} s={[0.98, 0.06, 0.44]} c={mustard} />
      {/* east face window grid */}
      <WindowPane p={[w, 1.15, 1.3]} facing="east" w={0.72} h={0.62} />
      <WindowPane p={[w, 1.15, 2.9]} facing="east" w={0.72} h={0.62} />
      <WindowPane p={[w, 2.45, 1.3]} facing="east" w={0.72} h={0.62} />
      <WindowPane p={[w, 2.45, 2.9]} facing="east" w={0.72} h={0.62} />
      {/* concrete plinth skirt */}
      <B p={[w / 2, 0.19, d / 2]} s={[w + 0.12, 0.38, d + 0.12]} c="#a99f8f" />
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

/**
 * Two-storey brick townhouse: flat parapet roof with a dentilled cornice, tall
 * lit windows over stone sills, an east bay window and a stooped front door
 * with iron railings.
 */
function TownhouseShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const trim = '#e9dcc3';
  const iron = '#2b2f33';
  const stone = '#9a8b7c';
  return (
    <>
      {/* stone water table around the base */}
      <B p={[w / 2, 0.26, d / 2]} s={[w + 0.14, 0.52, d + 0.14]} c="#8a7a6b" />
      {/* dentilled cornice + brick parapet with stone coping */}
      {Array.from({ length: 9 }, (_, i) => (
        <B key={`dent${i}`} p={[0.45 + i * 0.47, wallH - 0.1, d + 0.08]} s={[0.16, 0.12, 0.12]} c={trim} />
      ))}
      <B p={[w / 2, wallH + 0.06, d / 2]} s={[w + 0.55, 0.13, d + 0.55]} c={trim} />
      <B p={[-0.02, wallH + 0.3, d / 2]} s={[0.24, 0.36, d + 0.2]} c="#8a4f3f" />
      <B p={[w + 0.02, wallH + 0.3, d / 2]} s={[0.24, 0.36, d + 0.2]} c="#8a4f3f" />
      <B p={[w / 2, wallH + 0.3, -0.02]} s={[w + 0.2, 0.36, 0.24]} c="#8a4f3f" />
      <B p={[w / 2, wallH + 0.3, d + 0.02]} s={[w + 0.2, 0.36, 0.24]} c="#8a4f3f" />
      <B p={[-0.02, wallH + 0.52, d / 2]} s={[0.34, 0.08, d + 0.34]} c={trim} />
      <B p={[w + 0.02, wallH + 0.52, d / 2]} s={[0.34, 0.08, d + 0.34]} c={trim} />
      <B p={[w / 2, wallH + 0.52, -0.02]} s={[w + 0.34, 0.08, 0.34]} c={trim} />
      <B p={[w / 2, wallH + 0.52, d + 0.02]} s={[w + 0.34, 0.08, 0.34]} c={trim} />
      {/* recessed roof deck inside the parapet */}
      <B p={[w / 2, wallH + 0.16, d / 2]} s={[w, 0.09, d]} c="#6f665c" />
      {/* chimney + roof hatch tucked behind the parapet */}
      <Group p={[w - 0.85, wallH + 0.45, 0.85]}>
        <B p={[0, 0.25, 0]} s={[0.46, 1.1, 0.46]} c="#7e4638" />
        <B p={[0, 0.84, 0]} s={[0.58, 0.1, 0.58]} c={trim} />
      </Group>
      <B p={[1.0, wallH + 0.45, 1.0]} s={[0.7, 0.5, 0.7]} c="#8a7a6b" r={0.6} />
      {/* upper-storey windows with sills and lintels */}
      {[0.95, 2.3, 3.65].map((x) => (
        <Group key={`up${x}`} p={[x, 2.52, 0]}>
          <WindowPane p={[0, 0, d]} facing="south" w={0.62} h={0.8} />
          <B p={[0, -0.52, d + 0.05]} s={[0.86, 0.09, 0.14]} c={stone} />
          <B p={[0, 0.53, d + 0.04]} s={[0.86, 0.11, 0.1]} c={stone} />
        </Group>
      ))}
      <WindowPane p={[w, 2.52, 1.3]} facing="east" w={0.62} h={0.8} />
      <WindowPane p={[w, 2.52, 2.9]} facing="east" w={0.62} h={0.8} />
      {/* ground-floor bay window on the east face */}
      <B p={[w + 0.26, 1.05, 2.1]} s={[0.52, 2.1, 1.5]} c="#9d5f4c" />
      <B p={[w + 0.26, 2.15, 2.1]} s={[0.66, 0.1, 1.64]} c={trim} />
      <WindowPane p={[w + 0.52, 1.42, 2.1]} facing="east" w={1.0} h={0.85} />
      {/* wide parlour window with a planted window box */}
      <WindowPane p={[3.2, 1.5, d]} facing="south" w={1.1} h={0.95} />
      <B p={[3.2, 0.9, d + 0.05]} s={[1.34, 0.09, 0.14]} c={stone} />
      <B p={[3.2, 1.0, d + 0.12]} s={[1.2, 0.16, 0.18]} c={iron} />
      <Blob p={[2.85, 1.12, d + 0.12]} r={0.11} c="#6fae72" />
      <Blob p={[3.2, 1.14, d + 0.12]} r={0.12} c="#ef6f6c" />
      <Blob p={[3.55, 1.12, d + 0.12]} r={0.11} c="#6fae72" />
      {/* trimmed front door with transom, brass knob and a porch lantern */}
      <B p={[0.68, 1.1, d + 0.03]} s={[0.14, 1.6, 0.08]} c={trim} />
      <B p={[1.62, 1.1, d + 0.03]} s={[0.14, 1.6, 0.08]} c={trim} />
      <B p={[1.15, 1.97, d + 0.04]} s={[1.1, 0.14, 0.1]} c={trim} />
      <B p={[1.15, 1.08, d + 0.05]} s={[0.82, 1.32, 0.1]} c="#2e6f63" r={0.6} />
      <B p={[1.43, 1.05, d + 0.11]} s={[0.07, 0.07, 0.05]} c="#f2c14e" />
      <B p={[1.15, 1.85, d + 0.06]} s={[0.78, 0.22, 0.05]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.55} />
      <B p={[1.95, 1.62, d + 0.08]} s={[0.13, 0.22, 0.1]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.8} />
      {/* stone stoop stepping down to the lawn, flanked by iron railings */}
      <B p={[1.15, 0.06, d + 0.35]} s={[1.4, 0.72, 0.7]} c={stone} />
      <B p={[1.15, -0.02, d + 0.85]} s={[1.34, 0.56, 0.36]} c={stone} />
      <B p={[1.15, -0.09, d + 1.16]} s={[1.34, 0.42, 0.3]} c={stone} />
      {[0.48, 1.82].map((x) => (
        <Group key={`rail${x}`} p={[x, 0, 0]}>
          <B p={[0, 0.62, d + 0.16]} s={[0.06, 0.44, 0.06]} c={iron} />
          <B p={[0, 0.55, d + 0.92]} s={[0.06, 0.6, 0.06]} c={iron} />
          <B p={[0, 0.82, d + 0.55]} s={[0.05, 0.05, 0.9]} c={iron} />
        </Group>
      ))}
      {/* street tree by the west wall */}
      <Cyl p={[-0.7, 0.35, d - 0.6]} rTop={0.09} rBottom={0.12} h={1.1} c="#8a5f3a" />
      <Blob p={[-0.7, 1.25, d - 0.6]} r={0.52} c="#6fae72" scale={[1, 1.15, 1]} />
    </>
  );
}

function StorageShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const orange = '#e8893c';
  const slat = '#c9702a';
  const ribs = Math.floor(w / 0.62);
  const doorH = wallH - 0.55;
  return (
    <>
      {/* corrugated ribs on the north wall */}
      {Array.from({ length: ribs }, (_, i) => (
        <B key={`n${i}`} p={[0.5 + i * 0.62, wallH / 2, -0.05]} s={[0.12, wallH, 0.08]} c="#aeb4ba" />
      ))}
      {/* flat metal roof with an accent fascia band under it */}
      <B p={[w / 2, wallH + 0.07, d / 2]} s={[w + 0.5, 0.12, d + 0.5]} c="#8e959c" />
      <B p={[w / 2, wallH - 0.06, d / 2]} s={[w + 0.2, 0.16, d + 0.2]} c={orange} />
      {/* row of framed orange unit doors with number plaques on the south face */}
      {[0.95, 2.3, 3.65].map((x, i) => (
        <Group key={`unit${i}`} p={[x, 0, d]}>
          <B p={[0, 0.78, 0.03]} s={[1.14, 1.56, 0.06]} c="#eef1f4" />
          <B p={[0, 0.75, 0.07]} s={[0.98, 1.44, 0.06]} c={orange} />
          {Array.from({ length: 4 }, (_, j) => (
            <B key={`sl${j}`} p={[0, 0.3 + j * 0.32, 0.11]} s={[0.98, 0.05, 0.02]} c={slat} />
          ))}
          <B p={[0, 1.7, 0.05]} s={[0.34, 0.2, 0.05]} c="#eef1f4" />
        </Group>
      ))}
      {/* big roll-up door on the east face, flanked by bollards */}
      <B p={[w + 0.05, doorH / 2, d / 2]} s={[0.1, doorH, 2.8]} c={orange} />
      {Array.from({ length: 4 }, (_, j) => (
        <B key={`slat${j}`} p={[w + 0.11, 0.45 + j * 0.42, d / 2]} s={[0.02, 0.05, 2.8]} c={slat} />
      ))}
      <B p={[w + 0.12, 0.26, d / 2]} s={[0.04, 0.08, 0.6]} c="#7d838a" />
      <Cyl p={[w + 0.5, 0.3, d / 2 - 1.7]} rTop={0.09} rBottom={0.09} h={0.6} c={orange} />
      <Cyl p={[w + 0.5, 0.3, d / 2 + 1.7]} rTop={0.09} rBottom={0.09} h={0.6} c={orange} />
    </>
  );
}

/**
 * Friendly suburban home: slate gabled roof, brick chimney, covered front
 * porch with a red door, shuttered windows and a lit attic window in the
 * east gable.
 */
function HouseShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const halfDepth = d / 2;
  const rise = 1.05;
  const angle = Math.atan2(rise, halfDepth);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.45;
  const roof = '#5f6b74';
  const trim = '#f7f4ee';
  const shutter = '#46687a';
  const deck = '#c7b299';
  return (
    <>
      {/* slate gabled roof */}
      <mesh position={[w / 2, wallH + rise / 2, d / 4]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.6, 0.12, panelLen]} />
        <meshStandardMaterial color={roof} roughness={0.9} />
      </mesh>
      <mesh position={[w / 2, wallH + rise / 2, (3 * d) / 4]} rotation={[angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.6, 0.12, panelLen]} />
        <meshStandardMaterial color={roof} roughness={0.9} />
      </mesh>
      <B p={[w / 2, wallH + rise + 0.04, d / 2]} s={[w + 0.8, 0.14, 0.24]} c="#4d5860" />
      <Gable p={[w - 0.07, wallH, d / 2]} width={d} rise={rise} color="#c2d5df" />
      <Gable p={[-0.07, wallH, d / 2]} width={d} rise={rise} color="#c2d5df" />
      {/* lit attic window in the east gable */}
      <B p={[w + 0.075, wallH + 0.45, d / 2]} s={[0.05, 0.52, 0.52]} c={trim} />
      <B p={[w + 0.11, wallH + 0.45, d / 2]} s={[0.04, 0.38, 0.38]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.5} />
      {/* brick chimney through the north slope */}
      <Group p={[0.8, wallH + 0.5, 1.2]}>
        <B p={[0, 0.45, 0]} s={[0.5, 1.35, 0.5]} c="#9b6a5a" />
        <B p={[0, 1.18, 0]} s={[0.62, 0.12, 0.62]} c="#7d5347" />
      </Group>
      {/* covered porch: deck, step, posts and roof */}
      <B p={[1.35, -0.02, d + 0.5]} s={[2.1, 0.56, 1.0]} c={deck} />
      <B p={[1.35, -0.13, d + 1.1]} s={[1.2, 0.34, 0.24]} c={deck} />
      <B p={[0.45, 0.99, d + 0.9]} s={[0.1, 1.46, 0.1]} c={trim} />
      <B p={[2.25, 0.99, d + 0.9]} s={[0.1, 1.46, 0.1]} c={trim} />
      <B p={[1.35, 1.75, d + 0.5]} s={[2.3, 0.08, 1.15]} c={roof} />
      <B p={[1.35, 1.67, d + 1.05]} s={[2.3, 0.07, 0.07]} c={trim} />
      {/* red front door under the porch + a small side light */}
      <B p={[0.95, 0.88, d + 0.05]} s={[0.78, 1.24, 0.1]} c="#c94f4a" r={0.6} />
      <B p={[1.22, 0.85, d + 0.11]} s={[0.07, 0.07, 0.05]} c="#f2c14e" />
      <WindowPane p={[1.85, 1.0, d]} facing="south" w={0.5} h={0.5} />
      {/* shuttered windows on the south and east faces */}
      <WindowPane p={[3.5, 1.15, d]} facing="south" w={0.95} h={0.75} />
      <B p={[2.87, 1.15, d + 0.03]} s={[0.16, 0.9, 0.06]} c={shutter} />
      <B p={[4.13, 1.15, d + 0.03]} s={[0.16, 0.9, 0.06]} c={shutter} />
      <WindowPane p={[w, 1.15, 1.5]} facing="east" w={0.8} h={0.7} />
      <B p={[w + 0.03, 1.15, 0.92]} s={[0.06, 0.85, 0.16]} c={shutter} />
      <B p={[w + 0.03, 1.15, 2.08]} s={[0.06, 0.85, 0.16]} c={shutter} />
      {/* garden bushes by the porch */}
      <Blob p={[3.0, 0.25, d + 0.35]} r={0.3} c="#6fae72" scale={[1.2, 0.7, 1]} />
      <Blob p={[3.0, 0.44, d + 0.4]} r={0.1} c="#ef6f6c" />
      <Blob p={[4.2, 0.22, d + 0.3]} r={0.26} c="#549058" scale={[1.1, 0.7, 1]} />
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
        <SiteBase {...props} palette={PALETTES.apartment} wallH={3.2}>
          <ApartmentShell wallH={3.2} />
        </SiteBase>
      );
    case 'cottage':
      return (
        <SiteBase {...props} palette={PALETTES.cottage} wallH={2.2}>
          <CottageShell wallH={2.2} />
        </SiteBase>
      );
    case 'townhouse':
      return (
        <SiteBase
          {...props}
          palette={PALETTES.townhouse}
          wallH={3.4}
          body={(lit) => (
            <mesh position={[SITE_INTERIOR.w / 2, 1.7, SITE_INTERIOR.d / 2]} castShadow receiveShadow>
              <boxGeometry args={[SITE_INTERIOR.w, 3.4, SITE_INTERIOR.d]} />
              <meshStandardMaterial color={lit ? lighten(PALETTES.townhouse.wall, 34) : PALETTES.townhouse.wall} roughness={0.95} />
            </mesh>
          )}
        >
          <TownhouseShell wallH={3.4} />
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
        <SiteBase {...props} palette={PALETTES.cabin} wallH={2.2}>
          <HouseShell wallH={2.2} />
        </SiteBase>
      );
  }
}
