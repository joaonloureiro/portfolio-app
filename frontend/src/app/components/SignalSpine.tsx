'use client';

import {useEffect, useRef} from 'react';
import type {ReactNode} from 'react';

/**
 * Renders the vertical Signal Spine track and advances the runner pill proportionally on scroll.
 */
export default function SignalSpine({children}: {children: ReactNode}) {
  const layoutRef = useRef<HTMLDivElement>(null);
  const runnerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const layout = layoutRef.current;
    const runner = runnerRef.current;
    if (!layout || !runner) return;

    const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    let rafId = 0;

    const update = () => {
      rafId = 0;
      if (motionQuery.matches) {
        runner.style.transform = 'translateY(0px)';
        return;
      }
      const rect = layout.getBoundingClientRect();
      const travel = Math.max(0, rect.height - 80);
      const progress = Math.min(1, Math.max(0, (window.innerHeight * 0.5 - rect.top) / Math.max(1, travel)));
      runner.style.transform = `translateY(${progress * travel}px)`;
    };

    const scheduleUpdate = () => {
      if (rafId === 0) rafId = window.requestAnimationFrame(update);
    };

    update();
    window.addEventListener('scroll', scheduleUpdate, {passive: true});
    window.addEventListener('resize', scheduleUpdate, {passive: true});

    return () => {
      if (rafId !== 0) window.cancelAnimationFrame(rafId);
      window.removeEventListener('scroll', scheduleUpdate);
      window.removeEventListener('resize', scheduleUpdate);
    };
  }, []);

  return (
    <div className="signal-spine-layout" ref={layoutRef}>
      <div className="signal-spine-rail" aria-hidden="true">
        <div className="signal-spine-runner" ref={runnerRef} />
      </div>
      {children}
    </div>
  );
}
