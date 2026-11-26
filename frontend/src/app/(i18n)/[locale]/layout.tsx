import {Barlow_Condensed, Atkinson_Hyperlegible_Next} from 'next/font/google';
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

const bodyFont = Atkinson_Hyperlegible_Next({subsets: ['latin'], variable: '--font-body', adjustFontFallback: false});
const displayFont = Barlow_Condensed({subsets: ['latin'], weight: ['700', '800', '900'], variable: '--font-display'});

type Props = {
  children: ReactNode;
  params: Promise<{locale: Locale}>;
};

export function generateStaticParams() {
  return routing.locales.map((locale) => ({locale}));
}

export async function generateMetadata({params}: Omit<Props, 'children'>) {
  const {locale} = await params;
  const t = await getTranslations({locale, namespace: 'engagement'});

  return {title: t('meta_title'), description: t('meta_description')};
}

export default async function PublicLayout({children, params}: Props) {
  const {locale} = await params;
  if (!hasLocale(routing.locales, locale)) notFound();

  setRequestLocale(locale);
  const t = await getTranslations('engagement');

  return (
    <html lang={locale} className={`${bodyFont.variable} ${displayFont.variable} contract-site`} suppressHydrationWarning>
      <body>
        <a href="#main-content" className="contract-skip">{t('skip')}</a>
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
