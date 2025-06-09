import Hero from "@/app/components/sections/Hero";
import About from "@/app/components/sections/About";
import Skills from "@/app/components/sections/Skills";
import Projects from "@/app/components/sections/Projects";
import Contact from "@/app/components/sections/Contact";
import { Locale } from "next-intl";
import { use } from "react";
import { setRequestLocale } from "next-intl/server";

export default function Home({params}: {
  params: Promise<{locale: Locale}>;
}) {

  const {locale} = use(params);
  
    // Enable static rendering
  setRequestLocale(locale);

  return (
    <div className="container mx-auto px-4">
      <Hero />
      <About />
      <Skills />
      <Projects />
      <Contact />
    </div>
  );
}