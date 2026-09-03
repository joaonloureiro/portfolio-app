import {useTranslations} from 'next-intl';

export default function FieldNotes() {
  const t = useTranslations('notes');

  return (
    <section id="notes" className="field-notes" aria-labelledby="notes-title">
      <div className="notes-heading">
        <h2 id="notes-title">{t('title')} <span>— {t('status')}</span></h2>
        <p>{t('description')}</p>
      </div>
    </section>
  );
}
