# Personal Portfolio & Blog - Frontend

This is the frontend for my personal portfolio, built with [Next.js](https://nextjs.org), [Tailwind CSS](https://tailwindcss.com/), and [TypeScript](https://www.typescriptlang.org/). The project is designed to be a modern, responsive, and performant showcase of my skills and projects.

## ✨ Features

* **Internationalization (i18n)**: Supports both English and Portuguese, with language switching capabilities.
* **Dark Mode**: A sleek dark mode that can be toggled by the user, with preferences saved in local storage.
* **Component-Based Architecture**: Built with reusable React components for maintainability and scalability.
* **Responsive Design**: Fully responsive layout that looks great on all devices, from mobile phones to desktop screens.
* **Contact Form**: A functional contact form that communicates with a backend service to send emails.

## 🛠️ Tech Stack

* **Framework**: [Next.js 16](https://nextjs.org/)
* **Styling**: [Tailwind CSS 4](https://tailwindcss.com/)
* **Language**: [TypeScript](https://www.typescriptlang.org/)
* **Internationalization**: [next-intl](https://next-intl-docs.vercel.app/)
* **Icons**: [Heroicons](https://heroicons.com/) & [React Icons](https://react-icons.github.io/react-icons/)
* **Linting**: [ESLint](https://eslint.org/)

## 🚀 Getting Started

For the protected Admin surface, configure these server-only runtime variables in the deployment secret store (Komodo):

* `BACKEND_INTERNAL_URL`, such as `http://portfolio-backend:80`;
* `PUBLIC_BASE_URL` and `BETTER_AUTH_URL`, such as `https://joaoloureiro.dev.br`;
* `BETTER_AUTH_SECRET`, a high-entropy secret of at least 32 characters;
* `AUTH_DATABASE_PATH`, such as `/app/.data/auth.db` on a persistent volume;
* `OIDC_ISSUER`, `OIDC_DISCOVERY_ADDRESS`, `OIDC_CLIENT_ID`, and `OIDC_CLIENT_SECRET`;
* `OIDC_POST_LOGOUT_REDIRECT_URI=https://joaoloureiro.dev.br/`;
* `OIDC_SCOPE=openid profile email offline_access`;

Register `https://joaoloureiro.dev.br/api/auth/callback/authentik` as the Authentik redirect URI. Better Auth performs the Authorization Code + PKCE flow and stores encrypted provider tokens in the server-side SQLite session store. The browser receives only an HttpOnly session cookie; Admin requests use the same-origin `/admin/api/**` BFF and never receive a bearer token or use `localStorage`. The BFF forwards only allowlisted requests to the fixed internal backend URL.

### Post Markdown safety

Post bodies are rendered with `react-markdown` 10.1.0 (MIT), a maintained CommonMark-to-React renderer. The existing Next.js/React rendering path is not a Markdown parser, so a focused dependency is required; no rich-text editor, GFM extension, raw-HTML plugin, or client-side sanitizer is added. The public article renderer stays a Server Component, disables raw HTML, permits only the documented first-version element set, and accepts only HTTPS or fragment destinations. The Admin editor uses the same safe component for its local preview. The package adds the renderer and its parser dependencies to the existing lock file; it is not loaded by the public shell unless a post body is rendered.

To get a local copy up and running, follow these simple steps.

### Prerequisites

* Node.js 22.13 or later (the session store uses the built-in SQLite module)
* npm

### Installation & Development

1.  **Clone the repository:**
    ```bash
    git clone <your-repository-url>
    cd <your-repository-folder>/frontend
    ```

2.  **Install dependencies:**
    ```bash
    npm ci
    ```

3.  **Configure authentication:**
    Copy `.env.example` to `.env.local`, replace the placeholders, and register `http://localhost:3001/api/auth/callback/authentik` as the local Authentik redirect URI. The backend is expected at `http://localhost:3000`.

    `NEXT_PUBLIC_CONTACT_EMAIL` is a public build-time setting for the direct email link. It is separate from the private Admin email-delivery policy and SMTP transport configuration.

4.  **Run the development server:**
    The development server uses Turbopack. Port 3001 avoids colliding with the backend.
    ```bash
    npm run dev -- --port 3001
    ```

5.  **Open your browser:**
    Navigate to [http://localhost:3001](http://localhost:3001) to see the result.

### Building for Production

To create a production-ready build, run:
```bash
npm run build
```
