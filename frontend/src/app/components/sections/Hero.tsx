import Image from 'next/image';
import Link from 'next/link';
import {useTranslations} from 'next-intl';
import {ArrowDownIcon, ArrowUpRightIcon} from '@heroicons/react/24/outline';

const engagements = ['monthly', 'task'] as const;

export default function Hero() {
  const t = useTranslations('engagement');

  return (
    <>
      <section className="contract-hero" aria-labelledby="contract-title">
        <div className="contract-hero-art" aria-hidden="true">
          <Image
            src="/cobalt-cable.webp"
            alt=""
            fill
            preload
            sizes="(max-width: 720px) 133vw, 76vw"
          />
        </div>
        <div className="contract-wrap contract-pitch">
          <h1 id="contract-title">{t('headline')} <em>{t('headline_accent')}</em></h1>
          <div className="contract-pitch-copy">
            <p>{t('intro')}</p>
            <div className="contract-actions">
              <a href="#contact" className="contract-button">{t('start')}<ArrowUpRightIcon aria-hidden="true" /></a>
              <a href="#projects" className="contract-text-link">{t('view_work')}<ArrowDownIcon aria-hidden="true" /></a>
            </div>
          </div>
        </div>
      </section>

      <section id="rates" className="contract-rates" aria-labelledby="rates-title">
        <div className="contract-wrap contract-rates-grid">
          <header className="contract-rates-heading">
            <h2 id="rates-title">{t('rates')}</h2>
            <span>USD</span>
          </header>
          {engagements.map((mode) => (
            <Link key={mode} href={`?engagement=${mode}#contact`} className="contract-rate">
              <div>
                <h3>{t(mode)}</h3>
                <p className="contract-price">{t(`${mode}_price`)} <small>{t(`${mode}_unit`)}</small></p>
                <p className="contract-rate-summary">{t(`${mode}_summary`)}</p>
              </div>
              <ArrowUpRightIcon className="contract-rate-arrow" aria-hidden="true" />
            </Link>
          ))}
        </div>
      </section>
    </>
  );
}
