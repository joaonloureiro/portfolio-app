import type { Config } from "tailwindcss";

const config: Config = {
  // O modo dark é automático na v4, mas pode manter para clareza
  darkMode: "class", 
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  // REMOVA A CHAVE 'theme' INTEIRA.
  // plugins: [], // Plugins, se você tiver, continuam aqui.
};
export default config;