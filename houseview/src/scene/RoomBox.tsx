import { Html } from '@react-three/drei';
import { useState } from 'react';
import { HALF_WALL_HEIGHT, SLAB_THICKNESS, WALL_HEIGHT, WALL_THICKNESS, itemSpots } from '../layout';
import type { PlacedRoom } from '../model';
import { ItemMarker } from './ItemMarker';
import { Furniture } from './Furniture';
import type { Selection } from '../types';

interface RoomBoxProps {
  placed: PlacedRoom;
  selection: Selection;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  /** room tags are shown one floor at a time to avoid label clutter */
  showTag: boolean;
  /** ghosted floors are not interactive; clicks fall through to the active floor */
  interactive: boolean;
}

// One room: floor slab, the two cutaway walls (north + west), furniture,
// a name tag, and the item markers that live in it. Local origin is the
// room's north-west floor corner. Walls shared with a neighbouring room are
// Sims-style half walls so the camera can always see in.
export function RoomBox({ placed, selection, onSelectItem, onSelectRoom, showTag, interactive }: RoomBoxProps) {
  const { def, room, items } = placed;
  const { rect } = def;
  const [hovered, setHovered] = useState(false);
  const isSelected = selection?.kind === 'room' && selection.roomId === room.id;
  const spots = itemSpots(rect, items.length);
  // the attic sits under the roof, so it only gets short knee walls
  const isAttic = def.furniture === 'attic';
  const northH = isAttic ? 1.45 : placed.northInterior ? HALF_WALL_HEIGHT : WALL_HEIGHT;
  const westH = isAttic ? 1.45 : placed.westInterior ? HALF_WALL_HEIGHT : WALL_HEIGHT;

  const wallTint = hovered || isSelected ? '#ffffff' : def.wallColor;
  const floorTint = hovered || isSelected ? lighten(def.floorColor) : def.floorColor;

  return (
    <group position={[rect.x, 0, rect.z]}>
      {/* floor slab (clickable) */}
      <mesh
        position={[rect.w / 2, -SLAB_THICKNESS / 2, rect.d / 2]}
        receiveShadow
        onClick={(e) => {
          if (!interactive) return;
          e.stopPropagation();
          onSelectRoom(room.id);
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
        <boxGeometry args={[rect.w, SLAB_THICKNESS, rect.d]} />
        <meshStandardMaterial color={floorTint} roughness={0.9} />
      </mesh>

      {/* north wall (z = 0) */}
      <mesh position={[rect.w / 2, northH / 2, WALL_THICKNESS / 2]} castShadow receiveShadow>
        <boxGeometry args={[rect.w, northH, WALL_THICKNESS]} />
        <meshStandardMaterial color={wallTint} roughness={0.95} />
      </mesh>
      {/* west wall (x = 0) */}
      <mesh position={[WALL_THICKNESS / 2, westH / 2, rect.d / 2]} castShadow receiveShadow>
        <boxGeometry args={[WALL_THICKNESS, westH, rect.d]} />
        <meshStandardMaterial color={wallTint} roughness={0.95} />
      </mesh>
      {/* cap rails on half walls */}
      {!isAttic && placed.northInterior && (
        <mesh position={[rect.w / 2, northH + 0.03, WALL_THICKNESS / 2]} castShadow>
          <boxGeometry args={[rect.w + 0.04, 0.06, WALL_THICKNESS + 0.08]} />
          <meshStandardMaterial color="#b8a78c" roughness={0.7} />
        </mesh>
      )}
      {!isAttic && placed.westInterior && (
        <mesh position={[WALL_THICKNESS / 2, westH + 0.03, rect.d / 2]} castShadow>
          <boxGeometry args={[WALL_THICKNESS + 0.08, 0.06, rect.d + 0.04]} />
          <meshStandardMaterial color="#b8a78c" roughness={0.7} />
        </mesh>
      )}
      {/* skirting glow when active */}
      {(hovered || isSelected) && (
        <mesh position={[rect.w / 2, 0.03, rect.d / 2]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[Math.min(rect.w, rect.d) * 0.36, Math.min(rect.w, rect.d) * 0.4, 40]} />
          <meshBasicMaterial color={def.accent} transparent opacity={0.5} depthWrite={false} />
        </mesh>
      )}

      <Furniture kind={def.furniture} rect={rect} />

      {/* room name tag pinned above the back corner */}
      {showTag && (
        <Html position={[0.4, Math.max(northH, westH) + 0.25, 0.4]} zIndexRange={[30, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`room-tag${hovered || isSelected ? ' room-tag-active' : ''}`}>
            <span>{room.name}</span>
            {items.length > 0 && <em>{items.length}</em>}
          </div>
        </Html>
      )}

      {items.map((resolved, i) => (
        <ItemMarker
          key={resolved.item.id}
          resolved={resolved}
          position={[spots[i][0], 0, spots[i][1]]}
          selected={selection?.kind === 'item' && selection.id === resolved.item.id}
          onSelect={onSelectItem}
          interactive={interactive}
        />
      ))}
    </group>
  );
}

function lighten(hex: string): string {
  const n = parseInt(hex.slice(1), 16);
  const r = Math.min(255, ((n >> 16) & 0xff) + 24);
  const g = Math.min(255, ((n >> 8) & 0xff) + 24);
  const b = Math.min(255, (n & 0xff) + 24);
  return `rgb(${r},${g},${b})`;
}
