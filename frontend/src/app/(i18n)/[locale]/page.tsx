import {Locale} from 'next-intl';
import {setRequestLocale} from 'next-intl/server';
import Hero from '@/app/components/sections/Hero';
import FieldNotes from '@/app/components/sections/FieldNotes';
import Projects from '@/app/components/sections/Projects';
import About from '@/app/components/sections/About';
import Contact from '@/app/components/sections/Contact';

type HomeProps = {
  params: Promise<{locale: Locale}>;
};

export default async function Home({params}: HomeProps) {
  const {locale} = await params;
  setRequestLocale(locale);

  return (
    <>
      <Hero />
      <div className="work-index">
        <FieldNotes />
        <Projects />
      </div>
      <About />
      <Contact />
    </>
  );
}
