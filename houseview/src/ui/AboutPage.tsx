import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import type { SceneModel } from '../model';
import { SocialFooter } from './SocialFooter';
import { TopNav, type View } from './TopNav';

// "About" - the story of the application: why it exists, how the backend is
// built, and how this UI came to be. Content mirrors the solution README.

interface AboutPageProps {
  model: SceneModel;
  live: boolean;
  onNavigate: (view: View) => void;
}

const STACK = [
  '.NET 10 / C#',
  'ASP.NET Core Web API',
  'EF Core 10',
  'SQL Server',
  'FluentValidation',
  'Azure Blob Storage',
  'OpenTelemetry',
  'OpenAPI',
  'SSDT (.sqlproj)',
  'Aspire Dashboard',
  'React 18',
  'Three.js / @react-three/fiber',
  'GSAP',
  'React Query',
  'React Hook Form + Zod',
  'TypeScript + Vite',
];

const PATTERNS: { title: string; body: string }[] = [
  {
    title: 'Repository pattern',
    body: 'A generic base plus per-entity adapters. Read access is AsNoTracking with display includes; update access is tracked with no includes — two deliberate shapes, not one compromise.',
  },
  {
    title: 'DTOs at the boundary',
    body: 'Request and response records at the API edge; entities never leak out of the Application layer. Mapping is explicit static extension methods — no AutoMapper, no magic strings.',
  },
  {
    title: 'Soft vs. hard delete',
    body: 'Only Items are soft-deleted (IsDeleted + a DeletedReason, so history survives and the removal is written to the item’s event log); every other entity is hard-deleted via ExecuteDeleteAsync.',
  },
  {
    title: 'Per-item event log',
    body: 'Each Item carries an ItemEvent timeline — created, updated, soft-deleted — so an item’s history is a queryable audit trail rather than something that scrolls off the screen.',
  },
  {
    title: 'Optimistic concurrency',
    body: 'A SQL ROWVERSION token round-trips through every DTO. A stale write raises ConcurrencyConflictException and returns HTTP 409 instead of silently clobbering data.',
  },
  {
    title: 'Pagination envelope',
    body: 'Every list endpoint is paginated (PageRequest → PagedResult) and clamped through a single factory. There is no unbounded list query anywhere in the API.',
  },
  {
    title: 'Centralised exception handling',
    body: 'An IExceptionHandler chain maps domain exceptions to RFC 9457 ProblemDetails: not-found → 404, validation → 400, concurrency and in-use conflicts → 409. No per-controller try/catch.',
  },
  {
    title: 'Graceful FK-restrict handling',
    body: 'A hard delete blocked by a foreign key (SqlException 547) is translated to EntityInUseException → HTTP 409, rather than surfacing as a raw 500.',
  },
  {
    title: 'Auditing interceptor',
    body: 'A SaveChanges interceptor stamps CreatedDate / LastModifiedDate on every IAuditable entity, so auditing lives in one place instead of every service.',
  },
  {
    title: 'Source-generated logging',
    body: 'LoggerMessage source generators per layer (ServiceLog, RepositoryLog) - structured, allocation-free logging without string interpolation in the hot path.',
  },
];

const DECISIONS: { title: string; body: string }[] = [
  {
    title: 'Ports & adapters where it pays off',
    body: 'Repository interfaces live in Domain; their EF Core implementations live in Persistence. The core never depends on infrastructure - dependencies point inward.',
  },
  {
    title: 'Schema-first database',
    body: 'The schema is source-controlled as raw SQL in an SSDT project with idempotent post-deployment seeds. The database is the source of truth - not EF migrations.',
  },
  {
    title: 'Per-layer DI modules',
    body: 'Each layer owns an AddXxx() extension (AddApplication, AddPersistence, AddObservability, AddGlobalExceptionHandling) so Program.cs stays a thin composition root.',
  },
  {
    title: 'Vendor-neutral observability',
    body: 'OpenTelemetry traces, metrics and logs with auto-instrumentation. Only the exporter is environment-specific: Application Insights in Azure, OTLP to a local Aspire dashboard, or quietly nothing.',
  },
  {
    title: 'Storage behind a port',
    body: 'Picture bytes go to Azure Blob Storage through an IImageStorage port defined in Application; the Azure SDK lives only in a separate Infrastructure project. Uploads proxy through the API (magic-byte sniffed before they reach storage) and are read back via short-lived SAS URLs.',
  },
  {
    title: 'Async bulk import',
    body: 'A CSV of items uploads in one request and gets a 202 straight back: the payload is claim-checked into Blob Storage, one queue message per 25-row chunk fans out to a queue-triggered Azure Function, and a unique chunk-marker index makes redelivered messages idempotent. Job progress is derived from the markers and polled live from the Manage › Import tab.',
  },
  {
    title: 'Built to run as a public demo',
    body: 'A background service resets the database to its seed baseline on a schedule, rate limiting guards the endpoints, and CORS is scoped to the deployed Static Web App. Shipped to Azure through a GitHub Actions pipeline.',
  },
];

export function AboutPage({ model, live, onNavigate }: AboutPageProps) {
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;
    gsap.fromTo(root, { autoAlpha: 0 }, { autoAlpha: 1, duration: 0.45, ease: 'power2.out' });
    gsap.fromTo(
      root.querySelectorAll('.index-reveal'),
      { y: 34, autoAlpha: 0 },
      { y: 0, autoAlpha: 1, duration: 0.7, stagger: 0.07, ease: 'power3.out', delay: 0.1 },
    );
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onNavigate('house');
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onNavigate]);

  return (
    <div className="index-page about-page" ref={rootRef}>
      <div className="index-inner">
        <header className="index-header index-reveal">
          <button className="page-brand" onClick={() => onNavigate('house')}>
            Habitat
          </button>
          <div className="about-header-right">
            <TopNav current="about" onNavigate={onNavigate} />
            <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
          </div>
        </header>

        <h1 className="index-title index-reveal">About</h1>
        <p className="index-sub index-reveal">
          What Habitat is, why it exists, and how it was built.
        </p>

        <section className="about-section index-reveal">
          <h2>Why this exists</h2>
          <p>
            <strong>ItemCatalogue</strong> is a personal catalogue for the physical things you own — track <em>what</em> an
            item is, <em>where</em> it lives (which building, which floor, which room, right down to the drawer it&rsquo;s
            inside), <em>who</em> owns it, what it&rsquo;s worth, and whether it&rsquo;s been disposed of and why. Right now
            it&rsquo;s tracking{' '}
            <strong>{model.totalItems} item{model.totalItems === 1 ? '' : 's'}</strong> across{' '}
            <strong>{model.sites.length} place{model.sites.length === 1 ? '' : 's'}</strong>.
          </p>
          <p>
            It is deliberately a small, real-world domain used as a vehicle for practicing <strong>Clean / Hexagonal
            Architecture</strong> and modern ASP.NET Core — &ldquo;production patterns at hobby scale&rdquo; rather than
            big-system over-engineering. The whole codebase doubles as a learning log: it is heavily commented with the{' '}
            <em>why</em> behind each decision, and it is being built hands-on with AI tooling (Claude Code) as a learning
            exercise.
          </p>
        </section>

        <section className="about-section index-reveal">
          <h2>The backend</h2>
          <p>
            A JSON REST API — around a dozen resources spanning a spatial model (<code>Location</code>,{' '}
            <code>Floor</code>, <code>Room</code>, <code>Container</code>, <code>Item</code>, plus the <code>Door</code>s
            and <code>Stair</code>s that join rooms up) and the things that describe them (<code>Person</code>,{' '}
            <code>Tag</code>, <code>Collection</code>, <code>ItemEvent</code>, <code>Picture</code>) — arranged as five
            projects with dependencies pointing inward toward the domain:
          </p>
          <div className="arch-diagram" aria-label="Architecture diagram">
            <div className="arch-row">
              <div className="arch-box">
                <strong>ItemCatalogueAPI</strong>
                <span>composition root · HTTP</span>
              </div>
              <i>──►</i>
              <div className="arch-box">
                <strong>Application</strong>
                <span>use cases · DTOs · ports</span>
              </div>
              <i>──►</i>
              <div className="arch-box arch-core">
                <strong>Domain</strong>
                <span>entities · rules · ports</span>
              </div>
              <i>◄──</i>
              <div className="arch-box">
                <strong>Persistence</strong>
                <span>EF Core adapters</span>
              </div>
            </div>
            <div className="arch-row arch-row-db">
              <div className="arch-box arch-db">
                <strong>Infrastructure</strong>
                <span>Azure Blob adapter</span>
              </div>
              <div className="arch-box arch-db">
                <strong>Database (.sqlproj)</strong>
                <span>owns the SQL Server schema — raw SQL, not EF migrations</span>
              </div>
            </div>
          </div>
          <div className="stack-chips">
            {STACK.map((s) => (
              <span key={s} className="chip chip-muted">
                {s}
              </span>
            ))}
          </div>
        </section>

        <section className="about-section index-reveal">
          <h2>The domain model</h2>
          <p>
            What began as four flat entities is now a small, <em>measured</em> world. Everything hangs off a{' '}
            <strong>Location → Floor → Room → Container → Item</strong> hierarchy, and it carries real dimensions: floors,
            rooms, containers, doors and stairs all store inch-accurate sizes and positions. That geometry is exactly what
            lets the neighborhood render as a truthful cutaway of the actual space rather than a decorative cartoon.
          </p>
          <p>
            An <code>Item</code> lives directly in a <code>Room</code> or nested inside a <code>Container</code> (which can
            itself sit inside another container), is owned by a <code>Person</code>, can be labeled with{' '}
            <code>Tag</code>s, gathered into <code>Collection</code>s, photographed with <code>Picture</code>s, and keeps a
            running <code>ItemEvent</code> history. <code>Door</code>s and <code>Stair</code>s connect rooms so the scene
            knows how the stories join together.
          </p>
        </section>

        <section className="about-section index-reveal">
          <h2>Architectural decisions</h2>
          <div className="pattern-grid">
            {DECISIONS.map((d) => (
              <article key={d.title} className="pattern-card">
                <h3>{d.title}</h3>
                <p>{d.body}</p>
              </article>
            ))}
          </div>
        </section>

        <section className="about-section index-reveal">
          <h2>Design patterns</h2>
          <div className="pattern-grid">
            {PATTERNS.map((p) => (
              <article key={p.title} className="pattern-card">
                <h3>{p.title}</h3>
                <p>{p.body}</p>
              </article>
            ))}
          </div>
        </section>

        <section className="about-section index-reveal">
          <h2>Quality &amp; testing</h2>
          <p>
            Five test projects (xUnit v3 + NSubstitute + Shouldly) cover the Domain, Application, Persistence, API and
            Infrastructure layers — the integration tiers stand up real dependencies in Docker via Testcontainers: a SQL
            Server deployed from the same SSDT dacpac that owns the schema, and Azurite for blob storage. A schema-drift
            gate keeps the EF model honest against that dacpac.
          </p>
        </section>

        <section className="about-section index-reveal">
          <h2>How this UI was made</h2>
          <p>
            The front-end you are looking at — the isometric 3D neighborhood, the searchable Index, the full Manage
            workspace, and this page — was designed and built by <strong>Claude Fable&nbsp;5</strong>, Anthropic&rsquo;s
            frontier model, working inside <strong>Claude Code</strong>: from the first <code>npm install</code> through
            the camera choreography, the Sims-style cutaway dollhouse, half-wall sightline logic, ghost floors, and every
            GSAP transition, verified against the running app along the way.
          </p>
          <p>
            It is a Vite + React + TypeScript app: <strong>Three.js</strong> via <code>@react-three/fiber</code> renders
            the neighborhood (every database Location is its own building, furnished from primitive boxes, cylinders and
            spheres — no 3D asset files), <strong>GSAP</strong> drives the cinematics, and <strong>React Query</strong>{' '}
            talks to the API — falling back to a bundled mirror of the seed data when the backend is asleep, which is why
            this page works either way.
          </p>
          <p>
            It is not read-only. The <strong>Manage</strong> page is a complete CRUD workspace over every entity —
            locations, floors, rooms, containers, doors, stairs, items, people, tags and collections — with{' '}
            <strong>React Hook Form + Zod</strong> forms, an explorer tree, and paginated tables; edits round-trip through
            the same concurrency tokens and validation the API enforces, and quietly switch off when only demo data is
            present.
          </p>
          <div className="about-credit">
            <span className="about-credit-mark">✳</span>
            <div>
              <strong>Built with Claude Fable 5</strong>
              <span>UI conceived, written and verified end-to-end by Claude Fable 5 in Claude Code · 2026</span>
            </div>
          </div>
        </section>

        <footer className="index-footer about-footer">
          <button className="index-back" onClick={() => onNavigate('house')}>
            ← Explore the neighborhood
          </button>
          <button className="index-back" onClick={() => onNavigate('index')}>
            Browse the Index →
          </button>
        </footer>

        <SocialFooter />
      </div>
    </div>
  );
}
