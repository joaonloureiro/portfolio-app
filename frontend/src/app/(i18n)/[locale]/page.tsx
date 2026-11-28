import {setRequestLocale} from 'next-intl/server';
import Hero from '@/app/components/sections/Hero';
import Projects from '@/app/components/sections/Projects';
import Contact from '@/app/components/sections/Contact';
import {getPublicProjectList} from '@/lib/public-project-api';

export const dynamic = 'force-dynamic';

type HomeProps = {
  params: Promise<{locale: 'en' | 'pt'}>;
};

export default async function Home({params}: HomeProps) {
  const {locale} = await params;
  setRequestLocale(locale);
  const projects = await getPublicProjectList(locale);

  return (
    <>
      <Hero />
      <Projects
        projects={projects.status === 'success' ? projects.data.items : []}
        unavailable={projects.status !== 'success'}
      />
      <Contact />
    </>
  );
}
