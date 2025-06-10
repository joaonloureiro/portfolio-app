import Hero from "@/app/components/sections/Hero";
import About from "@/app/components/sections/About";
import Projects from "@/app/components/sections/Projects";
import Contact from "@/app/components/sections/Contact";
import { Locale } from "next-intl";
import { use } from "react";
import { setRequestLocale } from "next-intl/server";

export default function Home({params}: {
  params: Promise<{locale: Locale}>;
}) {
  setRequestLocale(use(params).locale);

  return (
    <>
      <Hero />
      <About />
      <Projects />
      <Contact />
    </>
  );
}