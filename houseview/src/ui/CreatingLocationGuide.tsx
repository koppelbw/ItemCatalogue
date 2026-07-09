import { Modal } from './forms/Modal';

// In-app help for the Manage page, opened from the "Creating a Location" pill.
// Explains how to use Manage, the top-down build order (the "import" workflow),
// how the entities relate, and how the stored inch dimensions + the
// "Show in 3D view" flag drive what the dollhouse actually renders. Purely
// informational, so it opens on demo data too.

interface CreatingLocationGuideProps {
  onClose: () => void;
}

/** The spatial chain every place is built from, shown as a flow of chips. */
const HIERARCHY = ['Location', 'Floor', 'Room', 'Container', 'Item'] as const;

interface Step {
  title: string;
  body: string;
}

const STEPS: Step[] = [
  {
    title: 'Location',
    body: 'The building itself: your house, a storage unit, a relative’s place. It only needs a name and (optionally) a description; every other spatial thing hangs off it. Each Location becomes its own building in the 3D neighborhood.',
  },
  {
    title: 'Floors',
    body: 'One per story. Give each a level index (basement = -1, ground = 0, upstairs = 1) so they stack in the right order. Ceiling height is optional.',
  },
  {
    title: 'Rooms',
    body: 'Placed on a floor. Give a footprint in inches, an origin (X/Y) plus width × depth, so the room is drawn to scale. Pick a room type for sensible colors and auto-furnishing; wall and floor colors are optional overrides.',
  },
  {
    title: 'Doors & Stairs',
    body: 'Doors join one room to another (or lead outside); stairs join a lower floor to the one above. They make the house read as one connected space instead of floating boxes.',
  },
  {
    title: 'Containers',
    body: 'Furniture and storage that live in a room (a dresser, a toolbox, a wardrobe) and can nest inside each other. Give a position and size in inches so they stand where you measured them.',
  },
  {
    title: 'Items',
    body: 'The actual things you own. Each item lives directly in a room, or tucked inside a container. This is usually the bulk of your catalogue.',
  },
];

export function CreatingLocationGuide({ onClose }: CreatingLocationGuideProps) {
  return (
    <Modal
      title="Creating a Location"
      onClose={onClose}
      footer={
        <button className="btn btn-primary" onClick={onClose}>
          Got it
        </button>
      }
    >
      <div className="guide">
        <p className="guide-lede">
          Habitat turns your catalogue into a to-scale 3D “dollhouse.” Here’s how to build a place, from an empty lawn to a
          furnished house, and how those measurements become what you see in the 3D view.
        </p>

        <section className="guide-section">
          <h4>Using the Manage page</h4>
          <p>
            Each tab along the top is one kind of thing (Items, Locations, Floors, Rooms, and so on), shown as a table with{' '}
            <strong>+ Add</strong>, <strong>Edit</strong> and <strong>Delete</strong>. The <strong>Explore</strong> tab is a
            drill-down tree (Location › Floor › Room › Container › Item) whose “+ Add” buttons pre-fill the parent for you, the
            easiest way to build top-down.
          </p>
          <p className="guide-note">
            Editing only works against <strong>live data</strong> (see the badge, top-right); on demo data the buttons are
            disabled. Forms validate the same way the server does, and anything you save appears in the 3D scene straight away.
          </p>
        </section>

        <section className="guide-section">
          <h4>Building a place, top-down</h4>
          <p>Work from the outside in. Each step slots into the one before it:</p>
          <ol className="guide-steps">
            {STEPS.map((s, i) => (
              <li key={s.title} className="guide-step">
                <span className="guide-step-num" aria-hidden>
                  {i + 1}
                </span>
                <div>
                  <strong>{s.title}</strong>
                  <p>{s.body}</p>
                </div>
              </li>
            ))}
          </ol>
          <div className="guide-callout guide-callout-muted">
            <strong>Bringing in a lot at once?</strong> With your rooms and containers in place, a bulk import brings in many items in one go
            from a spreadsheet (CSV), instead of filling in a form for each one. Like the rest of the editing tools, it works on
            live data and is switched off while you’re viewing demo data.
          </div>
        </section>

        <section className="guide-section">
          <h4>How the pieces fit together</h4>
          <div className="guide-flow" aria-label="Location to Floor to Room to Container to Item">
            {HIERARCHY.map((node, i) => (
              <span key={node} className="guide-flow-node">
                <span className="guide-chip">{node}</span>
                {i < HIERARCHY.length - 1 && (
                  <span className="guide-flow-arrow" aria-hidden>
                    →
                  </span>
                )}
              </span>
            ))}
          </div>
          <ul className="guide-list">
            <li>
              An <strong>Item</strong> lives in a <strong>Room</strong> <em>or</em> a <strong>Container</strong>, never both.
            </li>
            <li>
              A <strong>Container</strong> sits in a room, or nests inside another container.
            </li>
            <li>
              <strong>Doors</strong> connect two rooms (or lead outside); <strong>Stairs</strong> connect two floors.
            </li>
            <li>
              Around the structure: <strong>People</strong> own items, <strong>Tags</strong> and <strong>Collections</strong>{' '}
              group them, <strong>Pictures</strong> show them, and every item keeps a running history.
            </li>
          </ul>
        </section>

        <section className="guide-section">
          <h4>How real dimensions &amp; furniture get rendered</h4>
          <p>
            Every spatial measurement is stored in <strong>inches</strong>, and all of them are optional. You can catalogue
            something before you measure it; it just falls back to a default footprint until you do.
          </p>
          <ul className="guide-list">
            <li>
              <strong>Scale:</strong> Habitat draws <strong>1 unit = 24″</strong> in plan; heights are squashed (1 unit = 40″)
              to keep the open “cutaway” look.
            </li>
            <li>
              <strong>Rooms</strong> are drawn to their exact footprint and rotation; your wall and floor colors win, otherwise
              a palette based on the room type.
            </li>
            <li>
              <strong>Containers</strong> with a size become furniture, shaped from their name or type (bed, sofa, table,
              dresser, wardrobe…) and turned so their back is to the nearest wall.
            </li>
            <li>
              <strong>Items</strong> float as glowing markers in the room they live in, shaped and colored by their type. Doors
              cut openings in the walls; stairs climb between stories.
            </li>
          </ul>
          <div className="guide-callout">
            <strong>“Show in 3D view” is the key switch.</strong> On a <strong>Container</strong> or <strong>Item</strong>, this
            toggle decides whether the piece is actually drawn. Turn it on for the furniture and key pieces that define a room;
            leave it off for everyday clutter. It still counts in totals, it just isn’t rendered. That’s how the dollhouse
            stays readable instead of drowning in boxes.
          </div>
        </section>

        <p className="guide-cta">
          Ready? Start on the <strong>Locations</strong> tab (or <strong>Explore → + Add Location</strong>) and work down.
          Watch the house take shape in the 3D view as you go.
        </p>
      </div>
    </Modal>
  );
}
