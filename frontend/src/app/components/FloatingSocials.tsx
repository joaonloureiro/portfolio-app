import Link from 'next/link';
import { FaGithub, FaLinkedin } from 'react-icons/fa';
import { HiOutlineMail } from 'react-icons/hi';

const socialLinks = [
    { href: 'mailto:contato@joaoloureiro.dev.br', icon: <HiOutlineMail/>, label: 'Email' },
    { href: process.env.NEXT_PUBLIC_GITHUB_URL || '#', icon: <FaGithub/>, label: 'GitHub' },
    { href: process.env.NEXT_PUBLIC_LINKEDIN_URL || '#', icon: <FaLinkedin/>, label: 'LinkedIn' },
];

export default function FloatingSocials() {
    return (
        <div className="hidden md:block fixed left-0 top-1/2 -translate-y-1/2 z-30">
            <div className="flex flex-col items-center space-y-1 bg-[var(--color-card)]/50 border border-[var(--color-border)] p-2 rounded-r-lg backdrop-blur-sm">
                {socialLinks.map(link => (
                    <Link
                        key={link.label}
                        href={link.href}
                        target="_blank"
                        rel="noopener noreferrer"
                        aria-label={link.label}
                        className="p-2 text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-background)] rounded-md transition-colors"
                    >
                        <div className="h-4 w-4">{link.icon}</div>
                    </Link>
                ))}
            </div>
        </div>
    );
}