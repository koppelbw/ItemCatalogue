import { Html } from '@react-three/drei';
import { useState } from 'react';
import { HALF_WALL_HEIGHT, SLAB_THICKNESS, WALL_THICKNESS, containerFacing, containerShapeFor, itemSpots, type Rect } from '../layout';
import type { PlacedDoor, PlacedRoom } from '../model';
import { WALL_EAST, WALL_NORTH, WALL_SOUTH, WALL_WEST, type Selection } from '../types';
import { ContainerShape } from './ContainerShapes';
import { ItemMarker } from './ItemMarker';
import { lighten } from './primitives';

interface RoomBoxProps {
  placed: PlacedRoom;
  selection: Selection;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  onSelectContainer: (id: number) => void;
  /** room tags are shown one floor at a time to avoid label clutter */
  showTag: boolean;
  /** ghosted floors are not interactive; clicks fall through to the active floor */
  interactive: boolean;
}

/** Wall pieces left standing once the door openings are cut out. */
interface WallPiece {
  start: number;
  len: number;
  /** piece bottom (0 for full-height segments, door height for headers) */
  y: number;
  h: number;
}

/**
 * Split a wall of `length` × `height` into solid pieces around its door
 * openings: full-height segments between doors plus a header over each opening.
 */
function cutWall(length: number, height: number, doors: PlacedDoor[]): WallPiece[] {
  const sorted = [...doors].sort((a, b) => a.offset - b.offset);
  const pieces: WallPiece[] = [];
  let cursor = 0;
  for (const door of sorted) {
    const start = Math.max(cursor, door.offset);
    const end = Math.min(length, door.offset + door.w);
    if (start > cursor) pieces.push({ start: cursor, len: start - cursor, y: 0, h: height });
    if (end > start && door.h < height - 0.08) {
      pieces.push({ start, len: end - start, y: door.h, h: height - door.h });
    }
    cursor = Math.max(cursor, end);
  }
  if (cursor < length) pieces.push({ start: cursor, len: length - cursor, y: 0, h: height });
  return pieces;
}

// One room: floor slab, the two cutaway walls (north + west) with real door
// openings cut into them, stairs, and a name tag. The only objects drawn inside
// are the room's own top-level measured containers and markers for its
// Furniture-tagged room-level items — nothing nested deeper renders. Local
// origin is the room's north-west floor corner (rotation, when set, turns the
// whole group around it). Interior walls — ones with another room's floor
// space behind them — are Sims-style half walls so the camera can always see in.
export function RoomBox({ placed, selection, onSelectItem, onSelectRoom, onSelectContainer, showTag, interactive }: RoomBoxProps) {
  const { room, rect, colors, wallHeight, doors, containers, stairs, items, furnishings } = placed;
  const [hovered, setHovered] = useState(false);
  const isSelected = selection?.kind === 'room' && selection.roomId === room.id;
  const spots = itemSpots(rect, furnishings.length);
  const northH = placed.northInterior ? Math.min(HALF_WALL_HEIGHT, wallHeight) : wallHeight;
  const westH = placed.westInterior ? Math.min(HALF_WALL_HEIGHT, wallHeight) : wallHeight;

  const wallTint = hovered || isSelected ? '#ffffff' : colors.wallColor;
  const floorTint = hovered || isSelected ? lighten(colors.floorColor) : colors.floorColor;

  const northDoors = doors.filter((d) => d.wall === WALL_NORTH);
  const westDoors = doors.filter((d) => d.wall === WALL_WEST);
  const openDoors = doors.filter((d) => d.wall === WALL_SOUTH || d.wall === WALL_EAST);

  // door openings are cut into half walls too — a doorway taller than the half
  // wall leaves a clean gap (cutWall only adds a header when the door is shorter)
  const northPieces = cutWall(rect.w, northH, northDoors);
  const westPieces = cutWall(rect.d, westH, westDoors);
  // cap rails cover only the top-reaching segments, so gaps stay open
  const topsOf = (pieces: WallPiece[], h: number) => pieces.filter((p) => p.y + p.h >= h - 0.001);

  return (
    <group position={[rect.x, 0, rect.z]} rotation={[0, (-placed.rotation * Math.PI) / 180, 0]}>
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

      {/* north wall (z = 0), split around its door openings */}
      {northPieces.map((piece, i) => (
        <mesh key={`n${i}`} position={[piece.start + piece.len / 2, piece.y + piece.h / 2, WALL_THICKNESS / 2]} castShadow receiveShadow>
          <boxGeometry args={[piece.len, piece.h, WALL_THICKNESS]} />
          <meshStandardMaterial color={wallTint} roughness={0.95} />
        </mesh>
      ))}
      {/* west wall (x = 0) */}
      {westPieces.map((piece, i) => (
        <mesh key={`w${i}`} position={[WALL_THICKNESS / 2, piece.y + piece.h / 2, piece.start + piece.len / 2]} castShadow receiveShadow>
          <boxGeometry args={[WALL_THICKNESS, piece.h, piece.len]} />
          <meshStandardMaterial color={wallTint} roughness={0.95} />
        </mesh>
      ))}
      {/* cap rails on half walls, segment by segment so doorways stay open */}
      {placed.northInterior &&
        topsOf(northPieces, northH).map((p, i) => (
          <mesh key={`nc${i}`} position={[p.start + p.len / 2, northH + 0.03, WALL_THICKNESS / 2]} castShadow>
            <boxGeometry args={[p.len + 0.04, 0.06, WALL_THICKNESS + 0.08]} />
            <meshStandardMaterial color="#b8a78c" roughness={0.7} />
          </mesh>
        ))}
      {placed.westInterior &&
        topsOf(westPieces, westH).map((p, i) => (
          <mesh key={`wc${i}`} position={[WALL_THICKNESS / 2, westH + 0.03, p.start + p.len / 2]} castShadow>
            <boxGeometry args={[WALL_THICKNESS + 0.08, 0.06, p.len + 0.04]} />
            <meshStandardMaterial color="#b8a78c" roughness={0.7} />
          </mesh>
        ))}

      {/* doors on the open (south/east) cutaway sides render as thresholds on the floor */}
      {openDoors.map((d) => {
        const alongX = d.wall === WALL_SOUTH;
        const cx = alongX ? d.offset + d.w / 2 : rect.w - 0.02;
        const cz = alongX ? rect.d - 0.02 : d.offset + d.w / 2;
        return (
          <group key={d.door.id}>
            <mesh position={[cx, 0.04, cz]}>
              <boxGeometry args={alongX ? [d.w, 0.08, 0.24] : [0.24, 0.08, d.w]} />
              <meshStandardMaterial color="#9c7a52" roughness={0.6} />
            </mesh>
            {/* jamb posts so the opening reads as a doorway */}
            <mesh position={[alongX ? d.offset : cx, d.h / 2, alongX ? cz : d.offset]} castShadow>
              <boxGeometry args={[0.1, d.h, 0.1]} />
              <meshStandardMaterial color="#9c7a52" roughness={0.7} />
            </mesh>
            <mesh position={[alongX ? d.offset + d.w : cx, d.h / 2, alongX ? cz : d.offset + d.w]} castShadow>
              <boxGeometry args={[0.1, d.h, 0.1]} />
              <meshStandardMaterial color="#9c7a52" roughness={0.7} />
            </mesh>
          </group>
        );
      })}

      {/* measured top-level containers, clickable */}
      {containers.map((pc) => (
        <ContainerBox key={pc.container.id} pc={pc} rect={rect} selection={selection} interactive={interactive} onSelectContainer={onSelectContainer} />
      ))}

      {/* stairs climbing out of this room */}
      {stairs.map((ps) => (
        <group key={ps.stair.id} position={[ps.x, 0, ps.z]} rotation={[0, (-ps.rotation * Math.PI) / 180, 0]}>
          {Array.from({ length: ps.steps }, (_, i) => (
            <mesh
              key={i}
              position={[((i + 0.5) * ps.run) / ps.steps, ((i + 0.5) * ps.rise) / ps.steps, ps.w / 2]}
              castShadow
              receiveShadow
            >
              <boxGeometry args={[ps.run / ps.steps, ps.rise / ps.steps, ps.w]} />
              <meshStandardMaterial color="#b8a78c" roughness={0.8} />
            </mesh>
          ))}
        </group>
      ))}

      {/* skirting glow when active */}
      {(hovered || isSelected) && (
        <mesh position={[rect.w / 2, 0.03, rect.d / 2]} rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[Math.min(rect.w, rect.d) * 0.36, Math.min(rect.w, rect.d) * 0.4, 40]} />
          <meshBasicMaterial color={colors.accent} transparent opacity={0.5} depthWrite={false} />
        </mesh>
      )}

      {/* room name tag pinned above the back corner */}
      {showTag && (
        <Html position={[0.4, Math.max(northH, westH) + 0.25, 0.4]} zIndexRange={[12, 0]} style={{ pointerEvents: 'none' }}>
          <div className={`room-tag${hovered || isSelected ? ' room-tag-active' : ''}`}>
            <span>{room.name}</span>
            {items.length > 0 && <em>{items.length}</em>}
          </div>
        </Html>
      )}

      {furnishings.map((resolved, i) => (
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

/** Yaw that turns a south-authored shape to face each compass direction. */
const FACING_YAW = { S: 0, E: Math.PI / 2, N: Math.PI, W: -Math.PI / 2 } as const;

function ContainerBox({
  pc,
  rect,
  selection,
  interactive,
  onSelectContainer,
}: {
  pc: PlacedRoom['containers'][number];
  rect: Rect;
  selection: Selection;
  interactive: boolean;
  onSelectContainer: (id: number) => void;
}) {
  const [hovered, setHovered] = useState(false);
  const isSelected = selection?.kind === 'container' && selection.id === pc.container.id;
  const active = hovered || isSelected;
  const kind = containerShapeFor(pc.container.containerType, pc.container.name);
  // shapes are authored facing south; when the row carries no explicit rotation,
  // turn the piece so its back sits against the nearest wall and it opens into
  // the room. The turn is about the footprint centre with width/depth swapped
  // for east/west facings, so the catalogued footprint stays exactly where it is.
  const facing = pc.rotation ? 'S' : containerFacing(kind, rect.w, rect.d, pc.x, pc.z, pc.w, pc.d);
  const swap = facing === 'E' || facing === 'W';
  const sw = swap ? pc.d : pc.w;
  const sd = swap ? pc.w : pc.d;
  return (
    <group
      position={[pc.x, pc.y, pc.z]}
      rotation={[0, (-pc.rotation * Math.PI) / 180, 0]}
      onClick={(e) => {
        if (!interactive) return;
        e.stopPropagation();
        onSelectContainer(pc.container.id);
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
      <group position={[pc.w / 2, 0, pc.d / 2]} rotation={[0, FACING_YAW[facing], 0]}>
        <group position={[-sw / 2, 0, -sd / 2]}>
          <ContainerShape kind={kind} w={sw} h={pc.h} d={sd} color={pc.color} active={active} />
        </group>
      </group>
      {/* invisible envelope keeps the whole footprint clickable even for leggy shapes */}
      <mesh position={[pc.w / 2, pc.h / 2, pc.d / 2]}>
        <boxGeometry args={[pc.w, pc.h, pc.d]} />
        <meshBasicMaterial transparent opacity={0} depthWrite={false} />
      </mesh>
      {active && (
        <Html position={[pc.w / 2, pc.h + 0.5, pc.d / 2]} center zIndexRange={[15, 0]} style={{ pointerEvents: 'none' }}>
          <div className="marker-tip">
            <span className="marker-tip-dot" style={{ background: pc.color }} />
            <span className="marker-tip-name">{pc.container.name}</span>
            {pc.itemCount > 0 && <span className="marker-tip-type">{pc.itemCount} item{pc.itemCount === 1 ? '' : 's'}</span>}
          </div>
        </Html>
      )}
    </group>
  );
}

