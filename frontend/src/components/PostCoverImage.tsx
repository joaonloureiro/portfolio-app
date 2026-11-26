/* eslint-disable @next/next/no-img-element -- Arbitrary HTTPS sources are not compatible with a safe fixed Next.js image-host allowlist. */
import {safeMarkdownUrl} from './SafeMarkdown';

type Props = {
  src: string | null;
  alt: string | null;
  className?: string;
};

export default function PostCoverImage({src, alt, className}: Props) {
  const safeSrc = src ? safeMarkdownUrl(src, 'src') : null;
  if (!safeSrc) return null;

  return (
    <img
      className={className}
      src={safeSrc}
      alt={alt ?? ''}
      loading="lazy"
      decoding="async"
      referrerPolicy="no-referrer"
    />
  );
}
