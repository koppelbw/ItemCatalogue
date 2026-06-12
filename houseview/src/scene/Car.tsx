import { Html } from '@react-three/drei';
import { useState } from 'react';
import { itemSpots } from '../layout';
import type { CarRoom } from '../model';
import { ItemMarker } from './ItemMarker';
import { B, Cyl, Group } from './primitives';
import type { Selection } from '../types';

const BODY = '#7fa8c9';
const BODY_DARK = '#6b93b4';
const GLASS = '#cfe6f2';

function Wheel({ p }: { p: [number, number, number] }) {
  return (
    <group position={p} rotation={[0, 0, Math.PI / 2]}>
      <mesh castShadow>
        <cylinderGeometry args={[0.34, 0.34, 0.26, 24]} />
        <meshStandardMaterial color="#2e3338" roughness={0.9} />
      </mesh>
      <mesh position={[0, 0.0, 0]}>
        <cylinderGeometry args={[0.16, 0.16, 0.28, 16]} />
        <meshStandardMaterial color="#c9ced3" roughness={0.4} />
      </mesh>
    </group>
  );
}

interface CarProps {
  carRooms: CarRoom[];
  selection: Selection;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
}

// The family wagon parked on the driveway. The "Glove box" and "Trunk" rooms
// from the database live here: their items hover over the hood and the boot.
// Local origin: centre of the car at ground level; the car faces south (+z).
export function Car({ carRooms, selection, onSelectItem, onSelectRoom }: CarProps) {
  const [hovered, setHovered] = useState(false);
  const glove = carRooms.find((c) => c.slot === 'glove box');
  const trunk = carRooms.find((c) => c.slot === 'trunk');

  const markerRow = (carRoom: CarRoom, anchorZ: number) => {
    const spots = itemSpots({ x: -0.8, z: 0, w: 1.6, d: 0.01 }, carRoom.items.length);
    return carRoom.items.map((resolved, i) => (
      <ItemMarker
        key={resolved.item.id}
        resolved={resolved}
        position={[-0.8 + spots[i][0], 0.95, anchorZ]}
        selected={selection?.kind === 'item' && selection.id === resolved.item.id}
        onSelect={onSelectItem}
      />
    ));
  };

  const tag = (carRoom: CarRoom | undefined, anchorZ: number) =>
    carRoom && (
      <Html position={[0, 1.75, anchorZ]} center zIndexRange={[30, 0]} style={{ pointerEvents: 'none' }}>
        <div className={`room-tag${hovered ? ' room-tag-active' : ''}`}>
          <span>{carRoom.room.name}</span>
          {carRoom.items.length > 0 && <em>{carRoom.items.length}</em>}
        </div>
      </Html>
    );

  return (
    <group
      onClick={(e) => {
        e.stopPropagation();
        const target = glove ?? trunk;
        if (target) onSelectRoom(target.room.id);
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
      {/* body */}
      <B p={[0, 0.62, 0]} s={[1.7, 0.6, 4.1]} c={hovered ? '#8fb6d4' : BODY} r={0.45} />
      {/* cabin */}
      <B p={[0, 1.18, -0.25]} s={[1.5, 0.62, 2.3]} c={hovered ? '#8fb6d4' : BODY_DARK} r={0.45} />
      {/* windows */}
      <B p={[0, 1.2, 0.95]} s={[1.36, 0.44, 0.06]} c={GLASS} r={0.15} opacity={0.85} />
      <B p={[0.73, 1.2, -0.25]} s={[0.06, 0.42, 2.0]} c={GLASS} r={0.15} opacity={0.85} />
      <B p={[-0.73, 1.2, -0.25]} s={[0.06, 0.42, 2.0]} c={GLASS} r={0.15} opacity={0.85} />
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

      {tag(glove, 1.45)}
      {tag(trunk, -1.7)}
      <Group p={[0, 0, 0]}>
        {glove && markerRow(glove, 1.45)}
        {trunk && markerRow(trunk, -1.7)}
      </Group>
    </group>
  );
}
