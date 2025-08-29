import { Inter, Space_Grotesk } from "next/font/google";
import Header from "@/app/components/Header";
import Footer from "@/app/components/Footer";
import "@/app/globals.css";
import { Toaster } from 'react-hot-toast'; 

import {hasLocale, Locale, NextIntlClientProvider} from 'next-intl';
import { ThemeProvider } from "@/configuration/ThemeContext";
import {getTranslations, setRequestLocale} from 'next-intl/server';
import { ReactNode } from "react";
import { routing } from "@/i18n/routing";
import { notFound } from "next/navigation";
import FloatingSocials from "@/app/components/FloatingSocials";

const inter = Inter({ 
  subsets: ["latin"],
  variable: '--font-inter',
});

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

  return (
    <html lang={locale} className={`${inter.variable} scroll-smooth`}>
      <body className={`${inter.className} bg-background text-primary flex flex-col min-h-screen`}>
        <ThemeProvider>
          <NextIntlClientProvider locale={locale}>
            <FloatingSocials />
            <Header />
            <main className="flex flex-col flex-grow">
              {children}
            </main>
            <Footer />
            <Toaster
              position="bottom-right"
              toastOptions={{
                style: {
                  background: 'var(--color-card)',
                  color: 'var(--color-text-primary)',
                  border: '1px solid var(--color-border)',
                },
                success: {
                  iconTheme: {
                    primary: 'var(--color-primary)',
                    secondary: 'var(--color-card)',
                  },
                },
                error: {
                  iconTheme: {
                    primary: '#ef4444',
                    secondary: 'var(--color-card)',
                  },
                },
              }}
            />
          </NextIntlClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}