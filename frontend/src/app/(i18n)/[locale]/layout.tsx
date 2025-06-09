import { Inter } from "next/font/google";
import Header from "@/app/components/Header"; // Usando o Header que forneci
import Footer from "@/app/components/Footer";
import "@/app/globals.css"; // Importando o CSS global

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
          </NextIntlClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}