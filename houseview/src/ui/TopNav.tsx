// The one navigation bar shared by every view: the 3D neighborhood HUD and
// the Index / Manage / About sheets all render this same pill strip, so moving
// between pages always looks and works the same way.

export type View = 'house' | 'index' | 'about' | 'manage';

interface TopNavProps {
  current: View;
  onNavigate: (view: View) => void;
}

const LINKS: { view: View; label: string; title: string; icon: JSX.Element }[] = [
  {
    view: 'house',
    label: 'Neighborhood',
    title: 'The 3D neighborhood',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M3.5 11 12 4l8.5 7" />
        <path d="M6 9.8V20h12V9.8" />
        <path d="M10 20v-5.4h4V20" />
      </svg>
    ),
  },
  {
    view: 'index',
    label: 'Index',
    title: 'Browse the index',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
        <path d="M8.5 6h12M8.5 12h12M8.5 18h12" />
        <circle cx="3.8" cy="6" r="1.5" fill="currentColor" stroke="none" />
        <circle cx="3.8" cy="12" r="1.5" fill="currentColor" stroke="none" />
        <circle cx="3.8" cy="18" r="1.5" fill="currentColor" stroke="none" />
      </svg>
    ),
  },
  {
    view: 'manage',
    label: 'Manage',
    title: 'Manage locations, rooms, containers and items',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
        <path d="M3.5 8h9M18.9 8h1.6M3.5 16h3M12.9 16h7.6" />
        <circle cx="15.7" cy="8" r="2.6" />
        <circle cx="9.7" cy="16" r="2.6" />
      </svg>
    ),
  },
  {
    view: 'about',
    label: 'About',
    title: 'About Habitat',
    icon: (
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
        <circle cx="12" cy="12" r="9.2" />
        <path d="M12 11v5.4" />
        <circle cx="12" cy="7.4" r="0.6" fill="currentColor" stroke="none" />
      </svg>
    ),
  },
];

export function TopNav({ current, onNavigate }: TopNavProps) {
  return (
    <nav className="top-nav" aria-label="Pages">
      {LINKS.map((l) => (
        <button
          key={l.view}
          className={`top-nav-btn${current === l.view ? ' on' : ''}`}
          onClick={() => onNavigate(l.view)}
          title={l.title}
          aria-current={current === l.view ? 'page' : undefined}
        >
          {l.icon}
          <span>{l.label}</span>
        </button>
      ))}
    </nav>
  );
}
