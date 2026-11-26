import createMiddleware from 'next-intl/middleware';
import {routing} from './i18n/routing';

const proxy = createMiddleware(routing);

export default proxy;

export const config = {
  // Keep the non-prefixed Admin surface outside public locale negotiation.
  matcher: '/((?!api|trpc|_next|_vercel|admin(?:/|$)|auth(?:/|$)|.*\\..*).*)'
};
