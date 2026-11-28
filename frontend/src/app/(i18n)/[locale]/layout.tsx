import {JetBrains_Mono, Manrope, Sora} from 'next/font/google';
import {hasLocale, NextIntlClientProvider} from 'next-intl';
import type {Locale} from 'next-intl';
import {getTranslations, setRequestLocale} from 'next-intl/server';
import {notFound} from 'next/navigation';
import type {ReactNode} from 'react';
import Header from '@/app/components/Header';
import Footer from '@/app/components/Footer';
import PortfolioToaster from '@/app/components/PortfolioToaster';
import {ThemeProvider} from '@/configuration/ThemeContext';
import {routing} from '@/i18n/routing';
import '@/app/portfolio.css';

const displayFont = Sora({
  subsets: ['latin'],
  weight: ['600', '700', '800'],
  variable: '--font-display',
  adjustFontFallback: false
});

const bodyFont = Manrope({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-body',
  adjustFontFallback: false
});

const monoFont = JetBrains_Mono({
  subsets: ['latin'],
  weight: ['400', '500', '600'],
  variable: '--font-mono',
  adjustFontFallback: false
});

type Props = {
  children: ReactNode;
  params: Promise<{locale: Locale}>;
};

export function generateStaticParams() {
  return routing.locales.map((locale) => ({locale}));
}

export async function generateMetadata({params}: Omit<Props, 'children'>) {
  const {locale} = await params;
  const t = await getTranslations({locale, namespace: 'metadata'});

  return {title: t('home_title'), description: t('home_description')};
}

export default async function PublicLayout({children, params}: Props) {
  const {locale} = await params;
  if (!hasLocale(routing.locales, locale)) notFound();

  setRequestLocale(locale);
  const engagement = await getTranslations('engagement');

  return (
    <html
      lang={locale}
      className={`${bodyFont.variable} ${displayFont.variable} ${monoFont.variable} signal-site dark`}
      suppressHydrationWarning
    >
      <body>
        <a href="#main-content" className="signal-skip">{engagement('skip')}</a>
        <ThemeProvider>
          <NextIntlClientProvider locale={locale}>
            <Header />
            <main id="main-content" tabIndex={-1}>{children}</main>
            <Footer />
            <PortfolioToaster />
          </NextIntlClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
