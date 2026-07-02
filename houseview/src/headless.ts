// Dev-only hook for driving the scene in a hidden preview window. Browsers
// suspend requestAnimationFrame and ResizeObserver for windows that are never
// shown, so the R3F canvas neither sizes nor paints there. Loading the app
// with ?headless swaps both for timer-driven stand-ins and lets Scene expose
// the R3F state on window.__r3f so tooling can capture the canvas. Inert in
// normal use.
export const HEADLESS = new URLSearchParams(window.location.search).has('headless');

if (HEADLESS) {
  window.requestAnimationFrame = (cb: FrameRequestCallback) => window.setTimeout(() => cb(performance.now()), 16);
  window.cancelAnimationFrame = (id: number) => window.clearTimeout(id);

  class PolledResizeObserver {
    private els = new Set<Element>();
    private timer: number;
    constructor(private cb: ResizeObserverCallback) {
      this.timer = window.setInterval(() => this.report(), 400);
    }
    observe(el: Element) {
      this.els.add(el);
      this.report();
    }
    unobserve(el: Element) {
      this.els.delete(el);
    }
    disconnect() {
      this.els.clear();
      window.clearInterval(this.timer);
    }
    private report() {
      if (this.els.size === 0) return;
      const entries = [...this.els].map((el) => ({
        target: el,
        contentRect: el.getBoundingClientRect(),
        borderBoxSize: [],
        contentBoxSize: [],
        devicePixelContentBoxSize: [],
      })) as unknown as ResizeObserverEntry[];
      this.cb(entries, this as unknown as ResizeObserver);
    }
  }
  window.ResizeObserver = PolledResizeObserver as unknown as typeof ResizeObserver;
}
