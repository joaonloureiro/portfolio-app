import {useTranslations} from 'next-intl';

const skillGroups = [
  {
    key: 'backend',
    skills: ['.NET', 'C#', 'Node.js', 'Express', 'Entity Framework']
  },
  {
    key: 'frontend',
    skills: ['React', 'Next.js', 'Angular', 'TypeScript', 'HTML', 'CSS']
  },
  {
    key: 'databases',
    skills: ['SQL Server', 'Oracle', 'MariaDB', 'Redis', 'Elasticsearch']
  },
  {
    key: 'cloud',
    skills: ['Azure', 'Docker', 'RabbitMQ', 'Gitea', 'Traefik', 'Prometheus', 'Grafana', 'N8N']
  }
] as const;

export default function About() {
  const t = useTranslations('about');
  const skills = useTranslations('skills');
  const principles = t.raw('principles') as Array<{title: string; description: string}>;

  return (
    <section id="about" className="about-editorial" aria-labelledby="about-title">
      <header className="about-heading">
        <h2 id="about-title">{t('title')}</h2>
        <p>{t('statement')}</p>
      </header>

      <div className="about-narrative">
        <div className="about-copy">
          <p>{t('paragraph1')}</p>
          <p>{t('paragraph2')}</p>
          <p>{t('paragraph3')}</p>
        </div>

        <ol className="principles-list">
          {principles.map((principle, index) => (
            <li key={principle.title}>
              <span aria-hidden="true">{String(index + 1).padStart(2, '0')}</span>
              <div>
                <h3>{principle.title}</h3>
                <p>{principle.description}</p>
              </div>
            </li>
          ))}
        </ol>
      </div>

      <div className="skills-ledger" aria-labelledby="skills-title">
        <h3 id="skills-title">{skills('title')}</h3>
        <dl>
          {skillGroups.map((group) => (
            <div key={group.key}>
              <dt>{skills(group.key)}</dt>
              <dd>{group.skills.join(' · ')}</dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
