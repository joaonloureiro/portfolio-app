import {useTranslations} from 'next-intl';

const skillsData = {
  languages: ["JavaScript (ES6+)", "TypeScript", "HTML5", "CSS3", "Python"],
  frameworks: ["React", "Next.js", "Node.js", "Express", "Tailwind CSS", "tRPC"],
  tools: ["Git", "GitHub", "Docker", "VS Code", "Figma", "Postman", "Vercel"],
  databases: ["PostgreSQL", "MongoDB", "Redis", "Prisma ORM"]
};

const SkillCategory = ({ title, skills }: { title: string, skills: string[] }) => (
  <div className="bg-[var(--color-card)] p-6 rounded-lg border border-[var(--color-border)]">
    <h3 className="text-[var(--color-primary)] mb-4">{title}</h3>
    <div className="flex flex-wrap gap-2">
      {skills.map(skill => (
        <span key={skill} className="bg-[var(--color-border)]/50 text-[var(--color-text-secondary)] px-3 py-1 rounded-full text-sm font-medium">
          {skill}
        </span>
      ))}
    </div>
  </div>
);

export default function Skills() {
  const t = useTranslations('skills');

  return (
    <section id="skills" className="container mx-auto py-24 md:py-32 lg:py-48">
      <h2 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] mb-12">{t('title')}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        <SkillCategory title={t('languages')} skills={skillsData.languages} />
        <SkillCategory title={t('frameworks')} skills={skillsData.frameworks} />
        <SkillCategory title={t('tools')} skills={skillsData.tools} />
        <SkillCategory title={t('databases')} skills={skillsData.databases} />
      </div>
    </section>
  );
}