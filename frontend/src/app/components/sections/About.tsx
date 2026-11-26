import {useTranslations} from 'next-intl';

const capabilities = [
  {key: 'backend', stack: 'C# · .NET · SQL Server'},
  {key: 'frontend', stack: 'React · Next.js · Angular · TypeScript'},
  {key: 'delivery', stack: 'Docker · CI/CD · n8n'}
] as const;

export default function About() {
  const t = useTranslations('engagement');

  return (
    <section id="about" className="contract-about" aria-labelledby="about-title">
      <div className="contract-wrap">
        <header className="contract-section-heading">
          <h2 id="about-title">{t('about_heading')}</h2>
          <p>{t('about_intro')}</p>
        </header>
        <dl className="contract-capabilities">
          {capabilities.map(({key, stack}) => (
            <div key={key}>
              <dt>{t(`${key}_title`)}</dt>
              <dd>{t(`${key}_description`)}</dd>
              <dd className="contract-tech">{stack}</dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
