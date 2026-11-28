'use client';

import {useEffect, useRef} from 'react';
import {useTheme} from '@/configuration/ThemeContext';

/**
 * Renders the V4 3D perspective terrain mesh with flowing signal pulses along ridge lines.
 * Falls back to a single static frame when prefers-reduced-motion is active.
 */
export default function SignalTerrainCanvas() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const {theme} = useTheme();
  const themeRef = useRef(theme);

  useEffect(() => {
    themeRef.current = theme;
  }, [theme]);

  useEffect(() => {
    const canvas = canvasRef.current;
    const host = canvas?.parentElement;
    if (!canvas || !host) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    let reducedMotion = motionQuery.matches;
    let width = 0;
    let height = 0;
    let pointerX = 0.5;
    let pointerY = 0.5;
    let tick = 0;
    let frameId = 0;

    const resize = () => {
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      width = host.clientWidth;
      height = host.clientHeight;
      canvas.width = Math.max(1, Math.floor(width * dpr));
      canvas.height = Math.max(1, Math.floor(height * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const project = (xIdx: number, zIdx: number, cols: number) => {
      const fov = 440;
      const tiltX = (pointerY - 0.5) * 0.22;
      const tiltY = (pointerX - 0.5) * 0.34;
      const wx = (xIdx - cols / 2) * 58;
      const wz = zIdx * 54 + 130;
      const wave = Math.sin(xIdx * 0.36 + tick) * 26 + Math.cos(zIdx * 0.44 + tick * 0.8) * 20;
      const wy = 148 + wave + tiltX * wz * 0.32;
      const rx = wx + tiltY * wz * 0.38;
      const scale = fov / wz;
      return {
        x: width * 0.62 + rx * scale,
        y: height * 0.48 + wy * scale,
        s: scale
      };
    };

    const draw = () => {
      if (width === 0 || height === 0) return;
      ctx.clearRect(0, 0, width, height);
      const isDark = themeRef.current === 'dark';
      const cols = 30;
      const rows = 16;

      for (let z = 0; z < rows; z++) {
        const isSignalRow = z % 3 === 1;
        ctx.beginPath();
        const rowPoints: Array<{x: number; y: number; s: number}> = [];

        for (let x = 0; x < cols; x++) {
          const point = project(x, z, cols);
          rowPoints.push(point);
          if (x === 0) ctx.moveTo(point.x, point.y);
          else ctx.lineTo(point.x, point.y);
        }

        ctx.strokeStyle = isSignalRow
          ? (isDark ? 'rgba(56, 189, 248, 0.18)' : 'rgba(3, 105, 161, 0.16)')
          : (isDark ? 'rgba(243, 245, 248, 0.065)' : 'rgba(11, 17, 29, 0.065)');
        ctx.lineWidth = isSignalRow ? 1.3 : 0.9;
        ctx.stroke();

        for (let x = 0; x < cols; x++) {
          const point = rowPoints[x];
          const isNode = (x + z) % 8 === 0;
          ctx.beginPath();
          ctx.arc(point.x, point.y, Math.max(0.8, isNode ? 2.2 * point.s + 0.6 : 1.15 * point.s), 0, Math.PI * 2);
          ctx.fillStyle = isNode
            ? (x % 2 === 0 ? (isDark ? '#10B981' : '#047857') : (isDark ? '#38BDF8' : '#0369A1'))
            : (isDark ? 'rgba(243, 245, 248, 0.18)' : 'rgba(11, 17, 29, 0.16)');
          ctx.fill();
        }

        if (!reducedMotion && isSignalRow) {
          const speed = 4.2 + (z % 4) * 0.7;
          const pos = (tick * speed + z * 5.3) % (cols - 1);
          const i0 = Math.floor(pos);
          const frac = pos - i0;
          const p0 = rowPoints[i0];
          const p1 = rowPoints[Math.min(cols - 1, i0 + 1)];
          const px = p0.x + (p1.x - p0.x) * frac;
          const py = p0.y + (p1.y - p0.y) * frac;

          ctx.beginPath();
          ctx.arc(px, py, 3.6 * p0.s + 1.4, 0, Math.PI * 2);
          ctx.fillStyle = z % 2 === 1 ? (isDark ? '#34D399' : '#047857') : (isDark ? '#38BDF8' : '#0369A1');
          ctx.fill();
        }
      }
    };

    const loop = () => {
      draw();
      if (!reducedMotion) {
        tick += 0.02;
        frameId = window.requestAnimationFrame(loop);
      }
    };

    const handlePointerMove = (event: PointerEvent) => {
      if (event.pointerType === 'touch' || reducedMotion) return;
      const rect = canvas.getBoundingClientRect();
      pointerX = (event.clientX - rect.left) / Math.max(1, rect.width);
      pointerY = (event.clientY - rect.top) / Math.max(1, rect.height);
    };

    const handleMotionChange = (event: MediaQueryListEvent) => {
      reducedMotion = event.matches;
      window.cancelAnimationFrame(frameId);
      loop();
    };

    resize();
    loop();

    const observer = typeof ResizeObserver !== 'undefined'
      ? new ResizeObserver(() => {
          resize();
          if (reducedMotion) draw();
        })
      : null;
    observer?.observe(host);

    const handleWindowResize = () => {
      resize();
      if (reducedMotion) draw();
    };

    window.addEventListener('resize', handleWindowResize);
    host.addEventListener('pointermove', handlePointerMove, {passive: true});
    motionQuery.addEventListener('change', handleMotionChange);

    return () => {
      window.cancelAnimationFrame(frameId);
      observer?.disconnect();
      window.removeEventListener('resize', handleWindowResize);
      host.removeEventListener('pointermove', handlePointerMove);
      motionQuery.removeEventListener('change', handleMotionChange);
    };
  }, []);

  return <canvas ref={canvasRef} className="signal-terrain-canvas" aria-hidden="true" />;
}
