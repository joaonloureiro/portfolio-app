import type { NextConfig } from "next";
import createNextIntlPlugin from 'next-intl/plugin';

const nextConfig: NextConfig = {
  output: "standalone",
  images: {
    remotePatterns: [
      {protocol: "https", hostname: "github.com"},
      {protocol: "https", hostname: "joaoloureiro.dev.br"},
      {protocol: "https", hostname: "happy.joaoloureiro.dev.br"}
    ]
  }
};
const withNextIntl = createNextIntlPlugin();

export default withNextIntl(nextConfig);
