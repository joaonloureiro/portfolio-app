import {Barlow, Oswald} from 'next/font/google';
import Header from '@/app/components/Header';
import Footer from '@/app/components/Footer';
import PortfolioToaster from '@/app/components/PortfolioToaster';
import '@/app/globals.css';

import {hasLocale, Locale, NextIntlClientProvider} from 'next-intl';
import {ThemeProvider} from '@/configuration/ThemeContext';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import {ReactNode} from 'react';
import {routing} from '@/i18n/routing';
import {notFound} from 'next/navigation';

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

const directionContract = `
THESIS: Engineering breadth is presented as one complete system; this surface refuses the generic card-grid portfolio.
OWN-WORLD: Near-black identity rail, a responsive illustrated systems atlas, cream paper chapters, restrained apricot signals, and condensed editorial type.
STORY: Visitors see end-to-end craft, inspect real work without inflated claims, discover the future field-notes space, and contact João.
FIRST VIEWPORT: A 13rem identity rail anchors a full-bleed illustrated systems atlas with live localized copy and one clear project action.
FORM: Living Atlas, first-ranked C-family direction, seed 5ad2562d.
FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
`.trim();

type Props = {
  children: ReactNode;
  params: Promise<{locale: Locale}>;
};

export function generateStaticParams() {
  return routing.locales.map((locale) => ({locale}));
}

export async function generateMetadata(props: Omit<Props, 'children'>) {
  const {locale} = await props.params;
  const t = await getTranslations({locale, namespace: 'metadata'});

  return {
    title: t('home_title'),
    description: t('home_description')
  };
}

export default async function RootLayout({children, params}: Props) {
  const {locale} = await params;
  if (!hasLocale(routing.locales, locale)) {
    notFound();
  }

  setRequestLocale(locale);
  const contractScript = `document.body.prepend(document.createComment(${JSON.stringify(directionContract)}));`;

  return (
    <html
      lang={locale}
      className={`${bodyFont.variable} ${displayFont.variable} scroll-smooth`}
      suppressHydrationWarning
    >
      <body>
        <script dangerouslySetInnerHTML={{__html: contractScript}} />
        <ThemeProvider>
          <NextIntlClientProvider locale={locale}>
            <Header />
            <main className="site-main">{children}</main>
            <Footer />
            <PortfolioToaster />
          </NextIntlClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
