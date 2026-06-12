import { Html } from '@react-three/drei';
import gsap from 'gsap';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import type { Group as ThreeGroup } from 'three';
import { SITE_INTERIOR, WALL_THICKNESS, itemSpots } from '../layout';
import { furnitureForRoom, type Site } from '../model';
import type { Selection } from '../types';
import { Furniture } from './Furniture';
import { ItemMarker } from './ItemMarker';
import { B, Blob, Cyl, Group } from './primitives';

// Every database Location is its own little diorama in the neighbourhood.
// Each building shares the same bones (pad, cutaway interior furnished after
// the location's referenced room, item markers, nameplate) and gets a shell
// that gives it a silhouette of its own.

export interface SiteBuildingProps {
  site: Site;
  index: number;
  active: boolean;
  selection: Selection;
  onSelectItem: (id: number) => void;
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
};

interface SiteBaseProps extends SiteBuildingProps {
  palette: ShellPalette;
  wallH: number;
  /** decorative shell rendered around/above the interior */
  children?: ReactNode;
}

function SiteBase({ site, index, active, selection, onSelectItem, onSelectSite, palette, wallH, children }: SiteBaseProps) {
  const { w, d } = SITE_INTERIOR;
  const groupRef = useRef<ThreeGroup>(null);
  const [hovered, setHovered] = useState(false);
  const furniture = furnitureForRoom(site.featuredRoom);
  const spots = itemSpots(SITE_INTERIOR, site.items.length);

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
      {/* interior floor */}
      <B p={[w / 2, 0.22, d / 2]} s={[w, 0.16, d]} c={palette.floor} r={0.9} />

      {/* active halo on the lawn */}
      {active && (
        <mesh position={[w / 2, 0.02, d / 2]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[Math.max(w, d) * 0.82, Math.max(w, d) * 0.9, 48]} />
          <meshBasicMaterial color={palette.accent} transparent opacity={0.45} depthWrite={false} />
        </mesh>
      )}

      {/* cutaway interior: north + west walls, furniture, items */}
      <group position={[0, 0.3, 0]}>
        <mesh position={[w / 2, wallH / 2, WALL_THICKNESS / 2]} castShadow receiveShadow>
          <boxGeometry args={[w, wallH, WALL_THICKNESS]} />
          <meshStandardMaterial color={wallTint} roughness={0.95} />
        </mesh>
        <mesh position={[WALL_THICKNESS / 2, wallH / 2, d / 2]} castShadow receiveShadow>
          <boxGeometry args={[WALL_THICKNESS, wallH, d]} />
          <meshStandardMaterial color={wallTint} roughness={0.95} />
        </mesh>
        <Furniture kind={furniture} rect={SITE_INTERIOR} />
        {site.items.map((resolved, i) => (
          <ItemMarker
            key={resolved.item.id}
            resolved={resolved}
            position={[spots[i][0], 0, spots[i][1]]}
            selected={selection?.kind === 'item' && selection.id === resolved.item.id}
            onSelect={onSelectItem}
          />
        ))}
        {children}
      </group>

      {/* nameplate */}
      <Html position={[0.3, wallH + 1.0, 0.3]} zIndexRange={[30, 0]} style={{ pointerEvents: 'none' }}>
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
      {/* set-back upper storey along the north edge */}
      <B p={[w / 2, wallH + 1.05, 0.95]} s={[w + 0.24, 2.1, 1.9]} c="#cdd9e4" r={0.8} />
      {/* glowing window band on the upper storey */}
      <B p={[w / 2, wallH + 1.05, 1.92]} s={[w - 0.6, 0.7, 0.06]} c="#2e3338" emissive="#ffe9a3" emissiveIntensity={0.55} />
      {/* roof lip + rooftop AC unit */}
      <B p={[w / 2, wallH + 2.16, 0.95]} s={[w + 0.44, 0.12, 2.1]} c="#aebccb" />
      <B p={[w / 2 + 1.2, wallH + 2.45, 0.8]} s={[0.6, 0.45, 0.6]} c="#9aa1a8" r={0.5} />
      {/* entry awning on the west face */}
      <B p={[-0.35, wallH - 0.55, d - 1.0]} s={[0.8, 0.08, 1.3]} c="#3ec6b8" rotY={0} />
    </>
  );
}

function CottageShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const halfDepth = d * 0.55;
  const rise = 1.15;
  const angle = Math.atan2(rise, halfDepth);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.4;
  return (
    <>
      {/* half roof over the back of the cottage */}
      <mesh position={[w / 2, wallH + rise / 2, halfDepth / 2]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.7, 0.12, panelLen]} />
        <meshStandardMaterial color="#b86b4b" roughness={0.85} />
      </mesh>
      {/* chimney */}
      <Group p={[w - 1.0, wallH + 0.55, 0.9]}>
        <B p={[0, 0.4, 0]} s={[0.5, 1.1, 0.5]} c="#b08a8a" />
        <B p={[0, 1.0, 0]} s={[0.62, 0.12, 0.62]} c="#8d6d6d" />
      </Group>
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
  return (
    <>
      {/* corrugated ribs on the outside of the north wall */}
      {Array.from({ length: ribs }, (_, i) => (
        <B key={i} p={[0.5 + i * 0.62, wallH / 2, -0.05]} s={[0.12, wallH, 0.08]} c="#aeb4ba" />
      ))}
      {/* flat metal roof over the back half */}
      <B p={[w / 2, wallH + 0.07, d * 0.3]} s={[w + 0.5, 0.12, d * 0.62]} c="#8e959c" />
      {/* part-open roll-up door hanging over the east opening */}
      <B p={[w - 0.02, wallH - 0.45, d / 2]} s={[0.1, 0.9, d - 0.4]} c="#dde1e5" />
      <B p={[w - 0.02, wallH + 0.06, d / 2]} s={[0.22, 0.22, d - 0.2]} c="#e8893c" />
      {/* trim stripe along the wall tops */}
      <B p={[w / 2, wallH + 0.06, 0.07]} s={[w + 0.1, 0.14, 0.2]} c="#e8893c" />
    </>
  );
}

function CabinShell({ wallH }: { wallH: number }) {
  const { w, d } = SITE_INTERIOR;
  const halfDepth = d * 0.5;
  const rise = 0.9;
  const angle = Math.atan2(rise, halfDepth);
  const panelLen = Math.sqrt(halfDepth * halfDepth + rise * rise) + 0.35;
  return (
    <>
      <mesh position={[w / 2, wallH + rise / 2, halfDepth / 2]} rotation={[-angle, 0, 0]} castShadow>
        <boxGeometry args={[w + 0.5, 0.12, panelLen]} />
        <meshStandardMaterial color="#8a7355" roughness={0.9} />
      </mesh>
      <Cyl p={[-0.5, 0.4, 0.6]} rTop={0.16} rBottom={0.2} h={0.8} c="#8a5f3a" />
    </>
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
    default:
      return (
        <SiteBase {...props} palette={PALETTES.cabin} wallH={2.1}>
          <CabinShell wallH={2.1} />
        </SiteBase>
      );
  }
}
