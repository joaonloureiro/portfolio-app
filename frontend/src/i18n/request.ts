import {hasLocale} from 'next-intl';
import {getRequestConfig} from 'next-intl/server';
import {routing} from './routing';

export default getRequestConfig(async ({requestLocale}) => {
  const requested = await requestLocale;
  const locale = hasLocale(routing.locales, requested)
    ? requested
    : routing.defaultLocale;

  return {
    locale: locale,
    messages: (await import(`../locales/${locale}.json`)).default,
    // Set a default time zone if you plan to use time-related formatting.
    // timeZone: 'America/Sao_Paulo',
    // Set a default now value if you plan to use relative time formatting.
    // now: new Date(),
    // Set a default onError handler to make debugging easier.
    onError: (error) => {
      if (
        error.message.includes('MISSING_MESSAGE') ||
        error.message.includes('INVALID_MESSAGE')
      ) {
        // Potentially log missing messages
        console.warn(error.message);
      } else {
        console.error(JSON.stringify(error.message));
      }
    },
    getMessageFallback: ({namespace, key, error}) => {
      const path = [namespace, key].filter((part) => part!= null).join('.');
      if (error.code === 'MISSING_MESSAGE') {
        return `${path} is not yet translated`;
      } else {
        return `Dear translator, please fix this message: ${path}`;
      }
    }
  };
});