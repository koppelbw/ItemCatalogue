import gsap from 'gsap';
import { useEffect, useRef, useState } from 'react';

// Full-screen intro curtain. Shows while the catalogue loads, then GSAP sweeps
// it away and unmounts it.
export function Splash({ ready }: { ready: boolean }) {
  const ref = useRef<HTMLDivElement>(null);
  const [gone, setGone] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const letters = el.querySelectorAll('.splash-letter');
    gsap.fromTo(
      letters,
      { y: 40, autoAlpha: 0 },
      { y: 0, autoAlpha: 1, duration: 0.8, stagger: 0.06, ease: 'power3.out' },
    );
  }, []);

  useEffect(() => {
    const el = ref.current;
    if (!el || !ready) return;
    const tl = gsap.timeline({ onComplete: () => setGone(true) });
    tl.to(el.querySelector('.splash-inner'), { y: -30, autoAlpha: 0, duration: 0.5, ease: 'power2.in', delay: 0.4 });
    tl.to(el, { autoAlpha: 0, duration: 0.6, ease: 'power2.inOut' }, '-=0.2');
    return () => {
      tl.kill();
    };
  }, [ready]);

  if (gone) return null;

  return (
    <div className="splash" ref={ref}>
      <div className="splash-inner">
        <div className="splash-word">
          {'HABITAT'.split('').map((ch, i) => (
            <span key={i} className="splash-letter">
              {ch}
            </span>
          ))}
        </div>
        <p className="splash-sub">building your house from the catalogue…</p>
        <div className="splash-bar">
          <i />
        </div>
      </div>
    </div>
  );
}
