import { Html } from '@react-three/drei';
import { useFrame } from '@react-three/fiber';
import { useRef, useState } from 'react';
import type { Group as ThreeGroup } from 'three';
import { primaryType } from '../model';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES, type ResolvedItem } from '../types';
import { B, Cyl } from './primitives';

// A database item rendered as a small holographic exhibit: a glowing pedestal
// ring, a light beam, and a floating glyph shaped by the item's primary type.

function Glyph({ type, color }: { type: number; color: string }) {
  switch (type) {
    case 0: // Electronics - little laptop
      return (
        <group>
          <B p={[0, 0, 0]} s={[0.42, 0.04, 0.3]} c="#3a3f44" />
          <group position={[0, 0.02, -0.15]} rotation={[-1.95, 0, 0]}>
            <B p={[0, 0.15, 0]} s={[0.42, 0.3, 0.03]} c="#23262b" emissive={color} emissiveIntensity={0.9} />
          </group>
        </group>
      );
    case 1: // Bathroom - soap pump bottle
      return (
        <group>
          <Cyl p={[0, 0.12, 0]} rTop={0.11} rBottom={0.13} h={0.3} c={color} />
          <Cyl p={[0, 0.32, 0]} rTop={0.03} rBottom={0.03} h={0.12} c="#f7f4ee" />
          <B p={[0.05, 0.38, 0]} s={[0.14, 0.04, 0.05]} c="#f7f4ee" />
        </group>
      );
    case 2: // Cleaning supplies - spray bottle
      return (
        <group>
          <Cyl p={[0, 0.12, 0]} rTop={0.09} rBottom={0.12} h={0.28} c={color} />
          <B p={[0, 0.32, 0]} s={[0.09, 0.12, 0.09]} c="#3a3f44" />
          <B p={[0.08, 0.34, 0]} s={[0.1, 0.05, 0.06]} c="#3a3f44" />
        </group>
      );
    case 3: // Bedding - folded blanket stack
      return (
        <group>
          <B p={[0, 0.05, 0]} s={[0.4, 0.1, 0.3]} c={color} r={0.95} />
          <B p={[0, 0.15, 0]} s={[0.36, 0.1, 0.27]} c="#e9ddf6" r={0.95} />
          <B p={[0, 0.24, 0]} s={[0.32, 0.08, 0.24]} c={color} r={0.95} />
        </group>
      );
    default: // Books - small stack
      return (
        <group>
          <B p={[0, 0.04, 0]} s={[0.36, 0.07, 0.26]} c={color} />
          <B p={[0.02, 0.11, 0]} s={[0.33, 0.07, 0.24]} c="#5b8def" rotY={0.18} />
          <B p={[-0.01, 0.18, 0]} s={[0.3, 0.07, 0.22]} c="#f2c14e" rotY={-0.12} />
        </group>
      );
  }
}

interface ItemMarkerProps {
  resolved: ResolvedItem;
  /** local position of the pedestal base on the room floor */
  position: [number, number, number];
  selected: boolean;
  onSelect: (id: number) => void;
  /** markers on ghosted floors ignore the pointer */
  interactive?: boolean;
}

export function ItemMarker({ resolved, position, selected, onSelect, interactive = true }: ItemMarkerProps) {
  const { item } = resolved;
  const type = primaryType(item);
  const color = ITEM_TYPE_COLORS[type % ITEM_TYPE_COLORS.length];
  const floatRef = useRef<ThreeGroup>(null);
  const [hovered, setHovered] = useState(false);
  const phase = (item.id % 17) * 0.7;

  useFrame(({ clock }) => {
    const g = floatRef.current;
    if (!g) return;
    const t = clock.getElapsedTime();
    g.position.y = 0.95 + Math.sin(t * 1.6 + phase) * 0.07;
    g.rotation.y = t * 0.55 + phase;
    const target = hovered || selected ? 1.45 : 1;
    const s = g.scale.x + (target - g.scale.x) * 0.12;
    g.scale.setScalar(s);
  });

  return (
    <group
      position={position}
      onClick={(e) => {
        if (!interactive) return;
        e.stopPropagation();
        onSelect(item.id);
      }}
      onPointerOver={(e) => {
        if (!interactive) return;
        e.stopPropagation();
        setHovered(true);
        document.body.style.cursor = 'pointer';
      }}
      onPointerOut={() => {
        setHovered(false);
        document.body.style.cursor = 'auto';
      }}
    >
      {/* pedestal ring */}
      <mesh position={[0, 0.025, 0]}>
        <cylinderGeometry args={[0.3, 0.34, 0.05, 28]} />
        <meshStandardMaterial color={color} emissive={color} emissiveIntensity={selected ? 1.2 : 0.45} roughness={0.4} />
      </mesh>
      {/* light beam */}
      <mesh position={[0, 0.65, 0]}>
        <cylinderGeometry args={[0.16, 0.26, 1.25, 20, 1, true]} />
        <meshBasicMaterial color={color} transparent opacity={hovered || selected ? 0.28 : 0.13} depthWrite={false} />
      </mesh>
      {/* floating glyph */}
      <group ref={floatRef} position={[0, 0.95, 0]}>
        <Glyph type={type} color={color} />
      </group>
      {(hovered || selected) && (
        <Html position={[0, 1.7, 0]} center zIndexRange={[40, 0]} style={{ pointerEvents: 'none' }}>
          <div className="marker-tip">
            <span className="marker-tip-dot" style={{ background: color }} />
            <span className="marker-tip-name">{item.name}</span>
            <span className="marker-tip-type">{ITEM_TYPE_NAMES[type % ITEM_TYPE_NAMES.length]}</span>
          </div>
        </Html>
      )}
    </group>
  );
}
