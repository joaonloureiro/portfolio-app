const DEFAULT_CONTACT_EMAIL = 'me@joaoloureiro.dev.br';

export const contactEmail = process.env.NEXT_PUBLIC_CONTACT_EMAIL?.trim() || DEFAULT_CONTACT_EMAIL;
