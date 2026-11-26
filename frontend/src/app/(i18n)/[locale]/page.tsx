import {setRequestLocale} from 'next-intl/server';
import Hero from '@/app/components/sections/Hero';
import Projects from '@/app/components/sections/Projects';
import About from '@/app/components/sections/About';
import Contact from '@/app/components/sections/Contact';
import {getPublicProjectList} from '@/lib/public-project-api';

type HomeProps = {
  params: Promise<{locale: 'en' | 'pt'}>;
  searchParams: Promise<{engagement?: string | string[]}>;
};

export default async function Home({params, searchParams}: HomeProps) {
  const {locale} = await params;
  setRequestLocale(locale);
  const {engagement} = await searchParams;
  const selected = engagement === 'monthly' || engagement === 'task' ? engagement : '';
  const projects = await getPublicProjectList(locale);

  return (
    <>
      <Hero />
      <Projects
        projects={projects.status === 'success' ? projects.data.items : []}
        unavailable={projects.status !== 'success'}
      />
      <About />
      <Contact initialEngagement={selected} />
    </>
  );
}
