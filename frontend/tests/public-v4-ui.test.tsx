import {fireEvent, render, screen, waitFor} from '@testing-library/react';
import {NextIntlClientProvider} from 'next-intl';
import type {ReactNode} from 'react';
import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import Footer from '@/app/components/Footer';
import Header from '@/app/components/Header';
import Contact from '@/app/components/sections/Contact';
import Hero from '@/app/components/sections/Hero';
import Projects from '@/app/components/sections/Projects';
import {ThemeProvider} from '@/configuration/ThemeContext';
import type {PublicProjectSummary} from '@/lib/public-project-types';
import enMessages from '@/locales/en.json';

const replaceMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({replace: replaceMock}),
  usePathname: () => '/en'
}));

vi.mock('next/image', () => ({
  default: ({
    src,
    alt,
    fill: _fill,
    preload: _preload,
    ...rest
  }: {
    src: string;
    alt: string;
    fill?: boolean;
    preload?: boolean;
    [key: string]: unknown;
  }) => {
    void _fill;
    void _preload;
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={alt} {...rest} />;
  }
}));

vi.mock('@heroicons/react/24/outline', () => {
  const Icon = (props: Record<string, unknown>) => <svg {...props} />;
  return {
    ArrowDownIcon: Icon,
    ArrowTopRightOnSquareIcon: Icon,
    ArrowUpRightIcon: Icon,
    MoonIcon: Icon,
    SunIcon: Icon
  };
});

function renderPublic(ui: ReactNode) {
  return render(
    <ThemeProvider>
      <NextIntlClientProvider locale="en" messages={enMessages}>
        {ui}
      </NextIntlClientProvider>
    </ThemeProvider>
  );
}

const sampleProjects: PublicProjectSummary[] = [
  {
    id: 1,
    sortOrder: 1,
    imageReference: '/Portfolio.png',
    repositoryUrl: 'https://github.com/joaonloureiro/portfolio-app',
    liveUrl: 'https://joaoloureiro.dev.br',
    technologies: ['.NET', 'React', 'Next.js'],
    title: 'Portfolio & Admin',
    summary: 'Bilingual web app backed by a layered .NET API and admin workspace.'
  },
  {
    id: 2,
    sortOrder: 2,
    imageReference: 'https://cdn.example.com/custom-homelab-preview.png',
    repositoryUrl: null,
    liveUrl: null,
    technologies: ['Docker', 'RabbitMQ'],
    title: 'Homelab Services',
    summary: 'Self-hosted container and queue experiments configured from admin.'
  }
];

describe('V4 Signal Terrain public UI', () => {
  const originalGithub = process.env.NEXT_PUBLIC_GITHUB_URL;
  const originalLinkedin = process.env.NEXT_PUBLIC_LINKEDIN_URL;
  let storageMap: Map<string, string>;

  beforeEach(() => {
    storageMap = new Map<string, string>();
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storageMap.get(key) ?? null,
      setItem: (key: string, value: string) => storageMap.set(key, value),
      removeItem: (key: string) => storageMap.delete(key),
      clear: () => storageMap.clear()
    });
    replaceMock.mockReset();
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn()
      }))
    );
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(null);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    process.env.NEXT_PUBLIC_GITHUB_URL = originalGithub;
    process.env.NEXT_PUBLIC_LINKEDIN_URL = originalLinkedin;
  });

  it('renders floating HUD with JL monogram, theme toggle defaulting to dark, and contact link', () => {
    renderPublic(<Header />);

    expect(screen.getByRole('link', {name: /João Loureiro/i})).toHaveAttribute('href', '/en');
    expect(screen.getByRole('link', {name: /Contact/i})).toHaveAttribute('href', '/en#contact');
    expect(screen.queryByRole('link', {name: /Rates/i})).not.toBeInTheDocument();

    const themeButton = screen.getByRole('button', {name: enMessages.navigation.switch_to_light});
    fireEvent.click(themeButton);
    expect(localStorage.getItem('theme')).toBe('light');
    expect(screen.getByRole('button', {name: enMessages.navigation.switch_to_dark})).toBeInTheDocument();
  });

  it('renders admin-configured hero copy without freelance rate cards or CasaOS/Proxmox mentions', () => {
    renderPublic(<Hero />);

    expect(screen.getByRole('heading', {level: 1, name: enMessages.hero.headline})).toBeInTheDocument();
    expect(screen.getByText(enMessages.hero.subtitle)).toBeInTheDocument();
    expect(screen.getByRole('link', {name: enMessages.hero.cta_button})).toHaveAttribute('href', '#projects');
    expect(document.body.textContent).not.toMatch(/\$3,000|\$150|CasaOS|Proxmox/i);
  });

  it('renders dynamic admin projects including external HTTPS image URLs and icon links', () => {
    renderPublic(<Projects projects={sampleProjects} unavailable={false} />);

    expect(screen.getByRole('heading', {level: 3, name: 'Portfolio & Admin'})).toBeInTheDocument();
    expect(screen.getByRole('heading', {level: 3, name: 'Homelab Services'})).toBeInTheDocument();

    const externalImage = screen.getByRole('img', {name: 'Homelab Services'});
    expect(externalImage).toHaveAttribute('src', 'https://cdn.example.com/custom-homelab-preview.png');

    expect(screen.getByRole('link', {name: 'View Code — Portfolio & Admin'})).toHaveAttribute(
      'href',
      'https://github.com/joaonloureiro/portfolio-app'
    );
    expect(screen.getByRole('link', {name: 'Live Demo — Portfolio & Admin'})).toHaveAttribute(
      'href',
      'https://joaoloureiro.dev.br'
    );
  });

  it('renders empty and unavailable project states safely', () => {
    const {rerender} = renderPublic(<Projects projects={[]} unavailable={false} />);
    expect(screen.getByText(enMessages.engagement.work_empty)).toBeInTheDocument();

    rerender(
      <ThemeProvider>
        <NextIntlClientProvider locale="en" messages={enMessages}>
          <Projects projects={[]} unavailable />
        </NextIntlClientProvider>
      </ThemeProvider>
    );
    expect(screen.getByRole('status')).toHaveTextContent(enMessages.projects.unavailable);
  });

  it('renders contact form without engagement dropdown or portrait slot and posts to /api/email/send', async () => {
    process.env.NEXT_PUBLIC_GITHUB_URL = 'https://github.com/joaonloureiro';
    process.env.NEXT_PUBLIC_LINKEDIN_URL = 'https://linkedin.com/in/joaonloureiro';
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({sent: true})
    });
    vi.stubGlobal('fetch', fetchMock);

    const {container} = renderPublic(<Contact />);

    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/photo/i)).not.toBeInTheDocument();
    expect(container.querySelector('.signal-direct-email')).toBeNull();
    expect(screen.getByRole('link', {name: /me@joaoloureiro\.dev\.br/i})).toHaveClass('signal-email-inline');
    expect(screen.getByRole('link', {name: 'GitHub'}).textContent).toBe('');
    expect(screen.getByRole('link', {name: 'LinkedIn'}).textContent).toBe('');

    fireEvent.change(screen.getByLabelText(enMessages.contact.form_name), {target: {value: 'Alice'}});
    fireEvent.change(screen.getByLabelText(enMessages.contact.form_email), {target: {value: 'alice@example.com'}});
    fireEvent.change(screen.getByLabelText(enMessages.contact.form_message), {target: {value: 'Hello João'}});
    fireEvent.click(screen.getByRole('button', {name: enMessages.contact.submit_button}));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/email/send',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({name: 'Alice', email: 'alice@example.com', message: 'Hello João'})
      })
    );
  });

  it('renders footer profile links strictly as accessible icons with no visible link text', () => {
    process.env.NEXT_PUBLIC_GITHUB_URL = 'https://github.com/joaonloureiro';
    process.env.NEXT_PUBLIC_LINKEDIN_URL = 'https://linkedin.com/in/joaonloureiro';

    renderPublic(<Footer />);

    const socialNav = screen.getByRole('navigation', {name: enMessages.footer.socials_title});
    const links = socialNav.querySelectorAll('a');
    expect(links).toHaveLength(3);

    const githubLink = screen.getByRole('link', {name: 'GitHub'});
    const linkedinLink = screen.getByRole('link', {name: 'LinkedIn'});
    const emailLink = screen.getByRole('link', {name: 'Email'});

    expect(githubLink.textContent).toBe('');
    expect(linkedinLink.textContent).toBe('');
    expect(emailLink.textContent).toBe('');
    expect(githubLink.querySelector('svg')).not.toBeNull();
    expect(linkedinLink.querySelector('svg')).not.toBeNull();
    expect(emailLink.querySelector('svg')).not.toBeNull();
  });
});
