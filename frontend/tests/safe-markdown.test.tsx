import {render, screen} from '@testing-library/react';
import {describe, expect, it} from 'vitest';
import SafeMarkdown, {safeMarkdownUrl} from '@/components/SafeMarkdown';

describe('SafeMarkdown', () => {
  it('renders the supported semantic Markdown elements', () => {
    render(<SafeMarkdown source={'# Heading\n\n**strong** and `code`\n\n- one\n- two\n\n> quote\n\n[read](https://example.com)'} />);

    expect(screen.getByRole('heading', {name: 'Heading'})).toBeInTheDocument();
    expect(screen.getByText('strong')).toBeInTheDocument();
    expect(screen.getByText('code')).toBeInTheDocument();
    expect(screen.getByRole('list')).toBeInTheDocument();
    expect(screen.getByText('quote')).toBeInTheDocument();
    expect(screen.getByRole('link', {name: 'read'})).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('does not emit raw HTML, event handlers, or unsafe destinations', () => {
    const {container} = render(
      <SafeMarkdown source={'<script>alert(1)</script><div onclick="alert(2)">text</div>\n\n[bad](javascript:alert(3))\n\n![bad](data:text/html,<script>alert(4)</script>)'} />
    );

    expect(container.querySelector('script')).toBeNull();
    expect(container.querySelector('[onclick]')).toBeNull();
    expect(container.querySelector('a[href^="javascript:"]')).toBeNull();
    expect(container.querySelector('img[src^="data:"]')).toBeNull();
    expect(container.innerHTML).not.toContain('onclick');
  });

  it('rejects encoded, relative, protocol-relative, and non-HTTPS URLs', () => {
    for (const value of ['javascript:alert(1)', 'JaVaScRiPt:alert(1)', 'java%73cript:alert(1)', 'data:text/html,x', '//evil.example/path', '/relative', 'http://example.com']) {
      expect(safeMarkdownUrl(value, 'href')).toBeNull();
    }
    expect(safeMarkdownUrl('#section', 'href')).toBe('#section');
    expect(safeMarkdownUrl('https://example.com/docs', 'href')).toBe('https://example.com/docs');
  });

  it('keeps malformed and oversized code inert', () => {
    const oversizedCode = `~~~\n${'x'.repeat(60_000)}\n~~~`;
    const {container} = render(<SafeMarkdown source={`${oversizedCode}\n\n[broken](https://example.com`} />);

    expect(container.querySelector('script')).toBeNull();
    expect(container.textContent).toContain('x'.repeat(100));
  });
});
