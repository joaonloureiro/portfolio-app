import Image from 'next/image';
import Link from 'next/link';
import {useTranslations} from 'next-intl';

export default function Hero() {
  const hero = useTranslations('hero');

  return (
    <section id="home" className="atlas-home" aria-labelledby="hero-title">
      <Image
        src="/atlas-hero-background.png"
        alt=""
        fill
        priority
        sizes="(max-width: 980px) 100vw, calc(100vw - 13rem)"
        className="atlas-hero-image"
      />

      <div className="hero-content">
        <h1 id="hero-title">{hero('role')}</h1>
        <p>{hero('headline')}</p>
        <Link href="#projects" className="manifesto-action">
          {hero('cta_button')}
        </Link>
      </div>
    </section>
  );
}
