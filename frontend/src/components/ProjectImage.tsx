/* eslint-disable @next/next/no-img-element -- Arbitrary HTTPS project images entered via Admin cannot use a fixed remotePatterns allowlist. */
import Image from 'next/image';
import {safeMarkdownUrl} from '@/components/SafeMarkdown';

type ProjectImageProps = {
  src: string;
  alt: string;
  sizes?: string;
  priority?: boolean;
  className?: string;
};

/**
 * Renders local project images through Next.js Image optimization and arbitrary Admin HTTPS URLs safely.
 */
export default function ProjectImage({src, alt, sizes, priority = false, className = ''}: ProjectImageProps) {
  const trimmed = src.trim();
  if (trimmed.startsWith('/') && !trimmed.startsWith('//')) {
    return (
      <Image
        src={trimmed}
        alt={alt}
        fill
        preload={priority}
        sizes={sizes}
        className={className}
      />
    );
  }

  const safeExternalUrl = safeMarkdownUrl(trimmed, 'src');
  if (!safeExternalUrl) return null;

  return (
    <img
      src={safeExternalUrl}
      alt={alt}
      loading={priority ? 'eager' : 'lazy'}
      decoding="async"
      referrerPolicy="no-referrer"
      className={`signal-project-img-raw ${className}`.trim()}
    />
  );
}
