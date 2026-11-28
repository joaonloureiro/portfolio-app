'use client';

import {useEffect, useRef} from 'react';
import type {PointerEvent, ReactNode} from 'react';

/**
 * Adds a restrained 3D pointer tilt around a project showcase frame when mouse-hovered.
 */
export default function TiltFrame({children, className = ''}: {children: ReactNode; className?: string}) {
  const frameRef = useRef<HTMLDivElement>(null);
  const reducedMotionRef = useRef(false);

  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    reducedMotionRef.current = mediaQuery.matches;
    const handleChange = (event: MediaQueryListEvent) => {
      reducedMotionRef.current = event.matches;
      if (event.matches && frameRef.current) {
        frameRef.current.style.transform = 'rotateX(0deg) rotateY(0deg)';
      }
    };
    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, []);

  const handlePointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (reducedMotionRef.current || event.pointerType !== 'mouse') return;
    const frame = frameRef.current;
    if (!frame) return;
    const rect = frame.getBoundingClientRect();
    const rotateX = ((event.clientY - rect.top) / Math.max(1, rect.height) - 0.5) * -8;
    const rotateY = ((event.clientX - rect.left) / Math.max(1, rect.width) - 0.5) * 10;
    frame.style.transform = `rotateX(${rotateX.toFixed(2)}deg) rotateY(${rotateY.toFixed(2)}deg)`;
  };

  const handlePointerLeave = () => {
    if (frameRef.current) {
      frameRef.current.style.transform = 'rotateX(0deg) rotateY(0deg)';
    }
  };

  return (
    <div className="signal-tilt-wrap">
      <div
        ref={frameRef}
        className={`signal-tilt-frame ${className}`.trim()}
        onPointerMove={handlePointerMove}
        onPointerLeave={handlePointerLeave}
      >
        {children}
      </div>
    </div>
  );
}
