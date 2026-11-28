import type {Metadata} from 'next';
import type {ReactNode} from 'react';
import {bodyFont, displayFont, monoFont} from '@/app/fonts';
import {ADMIN_DOCUMENT_LANGUAGE} from './admin-constants';

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
