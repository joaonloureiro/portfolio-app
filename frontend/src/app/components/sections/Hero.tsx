import {useTranslations} from 'next-intl';
import {ArrowDownIcon, ArrowUpRightIcon} from '@heroicons/react/24/outline';
import SignalTerrainCanvas from '@/app/components/SignalTerrainCanvas';

export default function Hero() {
  const t = useTranslations('hero');
  const nav = useTranslations('navigation');

  return (
    <section className="signal-hero" aria-labelledby="hero-title">
      <SignalTerrainCanvas />
      <div className="signal-wrap">
        <div className="signal-hero-copy">
          <h1 id="hero-title">{t('headline')}</h1>
          <p className="signal-hero-lead">{t('subtitle')}</p>
          <div className="signal-hero-actions">
            <a href="#projects" className="signal-btn-primary">
              <span>{t('cta_button')}</span>
              <ArrowDownIcon aria-hidden="true" />
            </a>
            <a href="#contact" className="signal-btn-secondary">
              <span>{nav('contact')}</span>
              <ArrowUpRightIcon aria-hidden="true" />
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}
