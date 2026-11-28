import {JetBrains_Mono, Manrope, Sora} from 'next/font/google';
import type {Metadata} from 'next';
import type {ReactNode} from 'react';
import {ADMIN_DOCUMENT_LANGUAGE} from './admin-constants';

const bodyFont = Manrope({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-body',
  adjustFontFallback: false
});

const displayFont = Sora({
  subsets: ['latin'],
  weight: ['600', '700', '800'],
  variable: '--font-display',
  adjustFontFallback: false
});

const monoFont = JetBrains_Mono({
  subsets: ['latin'],
  weight: ['400', '500', '600'],
  variable: '--font-mono',
  adjustFontFallback: false
});

export const metadata: Metadata = {
  title: 'Admin | João Loureiro',
  robots: {
    index: false,
    follow: false
  }
};

export default function AdminLayout({children}: {children: ReactNode}) {
  return (
    <html
      lang={ADMIN_DOCUMENT_LANGUAGE}
      className={`${bodyFont.variable} ${displayFont.variable} ${monoFont.variable} admin-document`}
    >
      <body>{children}</body>
    </html>
  );
}
