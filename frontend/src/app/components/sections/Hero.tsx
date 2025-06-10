'use client';
import {useTranslations} from 'next-intl';
import Link from 'next/link';
import { TypeAnimation } from 'react-type-animation';

export default function Hero() {
  const t = useTranslations('hero');

  return (
    <section id="home" className="relative w-full flex flex-col items-center justify-center min-h-[calc(100vh-var(--header-height))]">
        <div className="container mx-auto px-4 text-center flex flex-col items-center">
            <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold text-white">
                {t('greeting')} <span className="text-[var(--color-primary)]">João Loureiro</span>
            </h1>
            <TypeAnimation
              sequence={[
                t('sequence_1'),
                1500,
                t('sequence_2'),
                1500,
                t('sequence_3'),
                1500,
              ]}
              wrapper="p"
              speed={50}
              className="text-xl md:text-2xl lg:text-3xl font-medium text-slate-300 max-w-3xl mx-auto mt-6 mb-8"
              repeat={Infinity}
            />
            <p className="text-lg md:text-xl text-slate-200 max-w-2xl mx-auto mt-6 mb-8">
                {t('subtitle')}
            </p>
            <Link href="#contact" className="bg-[var(--color-primary)] hover:bg-[var(--color-primary)]/90 text-white font-bold py-3 px-8 rounded-full transition-colors">
                {t('cta_button')}
            </Link>
        </div>
      
      <div className="absolute bottom-12">
        <div className="mouse"></div>
      </div>
    </section>
  );
}