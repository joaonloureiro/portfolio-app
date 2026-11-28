import localFont from 'next/font/local';

export const displayFont = localFont({
  src: './fonts/sora-latin.woff2',
  weight: '600 800',
  style: 'normal',
  display: 'swap',
  variable: '--font-display'
});

export const bodyFont = localFont({
  src: './fonts/manrope-latin.woff2',
  weight: '400 700',
  style: 'normal',
  display: 'swap',
  variable: '--font-body'
});

export const monoFont = localFont({
  src: './fonts/jetbrains-mono-latin.woff2',
  weight: '400 600',
  style: 'normal',
  display: 'swap',
  variable: '--font-mono'
});
