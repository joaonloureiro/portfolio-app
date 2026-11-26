import ReactMarkdown from 'react-markdown';

export const safeMarkdownUrl = (value: string, key: string): string | null => {
  const url = value.trim();
  if (key === 'href' && url.startsWith('#') && url.length > 1) return url;
  if (key !== 'href' && key !== 'src') return null;

  try {
    const parsed = new URL(url);
    return parsed.protocol === 'https:' ? parsed.toString() : null;
  } catch {
    return null;
  }
};

type SafeMarkdownProps = {
  source: string;
  className?: string;
};

export default function SafeMarkdown({source, className}: SafeMarkdownProps) {
  return (
    <div className={className} data-markdown-content>
      <ReactMarkdown
        skipHtml
        urlTransform={safeMarkdownUrl}
        allowedElements={['p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'em', 'strong', 'ul', 'ol', 'li', 'blockquote', 'pre', 'code', 'a', 'img', 'br']}
        components={{
          a: ({href, children}) => {
            const safeHref = href ? safeMarkdownUrl(href, 'href') : null;
            return safeHref
              ? <a href={safeHref} rel={safeHref.startsWith('https://') ? 'noopener noreferrer' : undefined}>{children}</a>
              : <span>{children}</span>;
          },
          img: ({src, alt}) => {
            const safeSrc = typeof src === 'string' ? safeMarkdownUrl(src, 'src') : null;
            // Markdown image dimensions are author-controlled, so Next Image cannot safely optimize this first-version field.
            return safeSrc ? <img src={safeSrc} alt={alt ?? ''} loading="lazy" /> : null; // eslint-disable-line @next/next/no-img-element
          },
          pre: ({children}) => <pre tabIndex={0}>{children}</pre>
        }}
      >
        {source}
      </ReactMarkdown>
    </div>
  );
}
