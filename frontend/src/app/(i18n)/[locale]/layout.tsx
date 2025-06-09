import { Inter } from "next/font/google";
import Header from "@/app/components/Header"; // Usando o Header que forneci
import Footer from "@/app/components/Footer";
import "@/app/globals.css"; // Importando o CSS global
import { Toaster } from 'react-hot-toast'; 

// Importações importantes do next-intl
import {hasLocale, Locale, NextIntlClientProvider} from 'next-intl';
import { ThemeProvider } from "@/configuration/ThemeContext";
import {getTranslations, setRequestLocale} from 'next-intl/server';
import { ReactNode } from "react";
import { routing } from "@/i18n/routing";
import { notFound } from "next/navigation";

const inter = Inter({ subsets: ["latin"] });

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
    <html lang={locale} className="scroll-smooth">
      <body className={`${inter.className} bg-background text-text-primary`}>
        <ThemeProvider>
          <NextIntlClientProvider locale={locale}>
            
            <Header />
            <main className="flex flex-col items-center">
              {children}
            </main>
            <Footer />
            <Toaster
              position="bottom-right"
              toastOptions={{
                // General styles for all toasts
                style: {
                  background: 'var(--color-card)', //
                  color: 'var(--color-text-primary)', //
                  border: '1px solid var(--color-border)', //
                },
                // Specific styles for success toasts
                success: {
                  iconTheme: {
                    primary: 'var(--color-primary)', //
                    secondary: 'var(--color-card)', //
                  },
                },
                // Specific styles for error toasts
                error: {
                  iconTheme: {
                    primary: '#ef4444', // A standard red color for errors
                    secondary: 'var(--color-card)', //
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