'use client'; 
import {useTranslations} from 'next-intl';
import { motion } from 'framer-motion';

// Updated skills based on your professional experience
const skillsData = {
  backend: [".NET (Framework, Core, 6+)", "C#", "Node.js", "Entity Framework"],
  frontend: ["Angular", "TypeScript", "Next.js", "React", "HTML5 & CSS3"],
  databases: ["Oracle", "PostgreSQL", "Elasticsearch", "SQL"],
  cloud: ["Azure", "Docker", "RabbitMQ", "Git", "Kibana"]
};

const SkillPill = ({ skill }: { skill: string }) => (
    <div className="bg-[var(--color-background)] text-[var(--color-text-secondary)] border border-[var(--color-border)] rounded-full px-4 py-2 text-sm font-medium">
      {skill}
    </div>
);

export default function AboutAndSkills() {
  const tAbout = useTranslations('about');
  const tSkills = useTranslations('skills');
  
  return (
    <motion.section 
      id="about" 
      className="container mx-auto py-12 md:py-20"
      initial={{ opacity: 0, y: 20 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.6, ease: "easeOut" }}
    >
      <h2 className="text-3xl md:text-4xl font-bold text-center text-[var(--color-text-primary)] heading-underline">{tAbout('title')}</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-16 items-start">
        {/* About Me Column */}
        <div className="space-y-4 text-lg text-left text-[var(--color-text-secondary)]">
          <p>{tAbout('paragraph1')}</p>
          <p>{tAbout('paragraph2')}</p>
          <p>{tAbout('paragraph3')}</p>
        </div>

        {/* Skills Column - Updated with new categories */}
        <div className="space-y-6">
          <div>
            <h3 className="text-xl font-semibold text-[var(--color-text-primary)] mb-3">{tSkills('backend')}</h3>
            <div className="flex flex-wrap gap-3">
              {skillsData.backend.map(skill => <SkillPill key={skill} skill={skill} />)}
            </div>
          </div>
          <div>
            <h3 className="text-xl font-semibold text-[var(--color-text-primary)] mb-3">{tSkills('frontend')}</h3>
            <div className="flex flex-wrap gap-3">
              {skillsData.frontend.map(skill => <SkillPill key={skill} skill={skill} />)}
            </div>
          </div>
          <div>
            <h3 className="text-xl font-semibold text-[var(--color-text-primary)] mb-3">{tSkills('databases')}</h3>
            <div className="flex flex-wrap gap-3">
              {skillsData.databases.map(skill => <SkillPill key={skill} skill={skill} />)}
            </div>
          </div>
           <div>
            <h3 className="text-xl font-semibold text-[var(--color-text-primary)] mb-3">{tSkills('cloud')}</h3>
            <div className="flex flex-wrap gap-3">
              {skillsData.cloud.map(skill => <SkillPill key={skill} skill={skill} />)}
            </div>
          </div>
        </div>
      </div>
    </motion.section>
  );
}