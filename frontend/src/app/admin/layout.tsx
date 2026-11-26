import {Barlow, Oswald} from 'next/font/google';
import type {Metadata} from 'next';
import type {ReactNode} from 'react';
import {ADMIN_DOCUMENT_LANGUAGE} from './admin-constants';

const bodyFont = Barlow({
  subsets: ['latin'],
  weight: ['400', '500', '600'],
  variable: '--font-body',
  display: 'swap'
});

const displayFont = Oswald({
  subsets: ['latin'],
  weight: ['300', '400', '500', '600'],
  variable: '--font-display',
  display: 'swap'
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
    <html lang={ADMIN_DOCUMENT_LANGUAGE} className={`${bodyFont.variable} ${displayFont.variable} admin-document`}>
      <body>{children}</body>
    </html>
  );
}
