using System.Text.Json;
using System.Text.Json.Nodes;

namespace JoaoLoureiro.Portfolio.Core.Locales;

/// <summary>
/// The reviewed, known-good catalog used to bootstrap storage and to complete public
/// responses with the structured values that are intentionally not Admin-editable.
/// Keep this copy in sync with frontend/src/locales/en.json and pt.json.
/// </summary>
public static class LocaleCatalogSeedData
{
    private const string EnglishJson = """
{
  "metadata": {
    "home_title": "João Loureiro | Full-Stack Developer",
    "home_description": "Full-stack developer building web applications and backend systems with .NET, relational databases, RabbitMQ, Angular, and React.",
    "projects_title": "Projects | João Loureiro",
    "projects_description": "Explore a selection of projects built by João Loureiro, showcasing skills in front-end and back-end development.",
    "about_title": "About Me | João Loureiro",
    "about_description": "Learn more about João Loureiro's journey into software development, his technical philosophy, and personal interests."
  },
  "engagement": {
    "meta_title": "João Loureiro | Freelance Full-Stack Developer",
    "meta_description": "Hire João Loureiro for full-stack development, .NET integrations, or deployment automation. Monthly contracts and fixed-price tasks in USD.",
    "skip": "Skip to content",
    "headline": "Ship your next",
    "headline_accent": "release.",
    "intro": "I’m João, an independent full-stack developer. I help teams build web applications, connect APIs, and get software into production.",
    "start": "Discuss a project",
    "view_work": "See my work",
    "rates": "Rates",
    "monthly": "Monthly contract",
    "monthly_price": "$3,000–$3,500",
    "monthly_unit": "per month",
    "monthly_summary": "Ongoing development and delivery for your team.",
    "task": "One-off task",
    "task_price": "$150",
    "task_unit": "per task",
    "task_summary": "A single fix, integration, or change with a defined scope.",
    "work": "Work",
    "work_heading": "Selected work",
    "work_intro": "Applications and infrastructure I’ve built, with a closer look at the decisions behind each project.",
    "work_empty": "Projects will appear here as they’re published.",
    "about_heading": "Where I can help",
    "about_intro": "I work across the application, its integrations, and the infrastructure that runs it.",
    "backend_title": "APIs & integrations",
    "backend_description": "Build .NET services, connect external systems, and work with relational databases.",
    "frontend_title": "Web applications",
    "frontend_description": "Build responsive interfaces and integrate them with backend services.",
    "delivery_title": "Delivery & automation",
    "delivery_description": "Containerize applications, automate workflows, and set up deployment pipelines.",
    "contact_heading": "What are you building?",
    "contact_intro": "Tell me what you need, your timeline, and any technical context. I’ll reply by email so we can scope the work.",
    "form_engagement": "Engagement",
    "form_unsure": "Let’s discuss"
  },
  "navigation": {
    "home": "Home",
    "overview": "Overview",
    "interfaces": "Interfaces",
    "services": "Services",
    "automation": "Automation",
    "infrastructure": "Infrastructure",
    "about": "About",
    "tech": "Technologies",
    "projects": "Projects",
    "posts": "Posts",
    "notes": "Notes",
    "contact": "Contact",
    "atlas_label": "System layers",
    "primary_label": "Primary navigation",
    "secondary_label": "Page shortcuts",
    "social_label": "Social links and contact",
    "language": "Change language",
    "switch_to_light": "Use light theme",
    "switch_to_dark": "Use dark theme",
    "open_menu": "Open menu",
    "close_menu": "Close menu"
  },
  "hero": {
    "greeting": "Hello, I'm",
    "title": "Software Developer & Web Enthusiast",
    "subtitle": "Software developer focused on .NET, relational databases, RabbitMQ, Angular, and React—currently building with AI agents and self-hosting services in my homelab.",
    "role": "Full Stack Developer",
    "rail_intro": "I build complete systems — from interface to infrastructure.",
    "manifesto_label": "Manifesto",
    "headline": "João Loureiro",
    "manifesto": "Clarity in code. Discipline in process. Confidence in the result.",
    "cta_button": "View projects"
  },
  "atlas": {
    "interfaces": {
      "title": "Interfaces — clear experiences",
      "points": ["Routes", "Components", "States", "Accessibility", "Performance"]
    },
    "services": {
      "title": "Services — rules and integrations",
      "points": ["Authentication", "Validation", "Business rules", "Integrations", "Clear domain"]
    },
    "automation": {
      "title": "Automation — flows and processes",
      "points": ["Events", "Orchestration", "Schedules", "Idempotency", "Traceability"]
    },
    "infrastructure": {
      "title": "Infrastructure — delivery and observability",
      "points": ["Containers", "CI/CD", "Logs and metrics", "Alerts", "Operations"]
    }
  },
  "notes": {
    "title": "Field notes",
    "status": "Coming soon",
    "description": "Reflections on software, infrastructure, and the craft of building systems.",
    "published_status": "Published",
    "published_description": "Notes on software, infrastructure, and the decisions behind complete systems.",
    "published_link": "Read all notes",
    "purpose_title": "Next chapter",
    "purpose": "An editorial archive prepared for articles, technical decisions, and project lessons."
  },
  "posts": {
    "title": "Field notes",
    "description": "Published notes on software, infrastructure, and the decisions behind complete systems.",
    "list_label": "Published field notes",
    "loading": "Loading published notes…",
    "empty": "There are no published notes yet.",
    "unavailable": "Field notes are unavailable right now. Please try again later.",
    "back_to_posts": "Back to field notes",
    "related_title": "Related notes"
  },
  "about": {
    "title": "About Me",
    "statement": "Technical breadth with end-to-end responsibility.",
    "paragraph1": "I am a Full Stack Developer driven by a passion for building, automating, and scaling robust applications. My journey into technology began with a desire to understand how complex systems work, and it has evolved into a career dedicated to crafting elegant, high-performance solutions.",
    "paragraph2": "With a strong foundation in the .NET and JavaScript ecosystems, I specialize in creating modern web applications using technologies like .NET, Angular, Next.js, and React. However, my curiosity extends beyond code. As you can see from my Homelab project, I am deeply engaged with the full development lifecycle, including CI/CD, containerization with Docker, and infrastructure management.",
    "paragraph3": "I thrive on challenges and am constantly exploring new technologies, from database administration with MariaDB and Redis to workflow automation with N8N and message queuing with RabbitMQ. This hands-on approach to DevOps and self-hosting allows me to build not just features, but resilient and scalable systems.",
    "principles": [
      {"title": "Clarity before complexity", "description": "Architecture and code should make decisions visible, not hide them."},
      {"title": "Systems, not isolated features", "description": "Interface, service, automation, and operations are part of the same outcome."},
      {"title": "Evidence-led evolution", "description": "Measurement, observability, and learning guide the next step."}
    ]
  },
  "skills": {
    "title": "Technical field",
    "backend": "Backend & Languages",
    "frontend": "Frontend",
    "databases": "Databases & Data",
    "cloud": "Cloud & DevOps"
  },
  "projects": {
    "title": "Projects",
    "intro": "Applications, APIs, and infrastructure experiments I've built.",
    "entry_label": "Project",
    "tech_used": "Technologies Used",
    "page_link": "See Details",
    "live_link": "Live Demo",
    "repo_link": "View Code",
    "back_to_projects": "Back to Projects",
    "about_project": "About this Project",
    "not_found": "Project not found",
    "unavailable": "Projects are temporarily unavailable. Please try again shortly."
  },
  "contact": {
    "title": "Get in touch",
    "subtitle": "Have a role, technical challenge, or project in mind? Send a message and I'll get back to you by email.",
    "direct_label": "Direct email",
    "form_name": "Name",
    "form_email": "Email",
    "form_message": "Message",
    "field_required": "This field is required.",
    "privacy_note": "Your details will only be used to reply to this message.",
    "submit_button": "Send message",
    "status_sending": "Sending your message...",
    "status_success": "Message sent successfully. Thank you!",
    "status_error_failed": "Failed to send message. Please try again.",
    "status_error_all_fields": "All fields are required.",
    "status_error_invalid_email": "Please enter a valid email address.",
    "status_error_generic": "An unexpected error occurred. Please try again later.",
    "server_unexpected_error": "An unexpected server error occurred. Please try again later.",
    "smtp_auth_failed": "Authentication with the mail server failed.",
    "smtp_connection_failed": "Could not connect to the mail server.",
    "smtp_invalid_recipient": "Invalid recipient email address.",
    "smtp_generic_error": "A mail-sending error occurred.",
    "contact_invalid_input": "Check the form fields and try again.",
    "contact_payload_too_large": "Your message is too large. Shorten it and try again.",
    "contact_throttled": "Too many attempts. Please wait a moment and try again.",
    "contact_delivery_disabled": "Contact delivery is temporarily disabled. Please try again later.",
    "contact_delivery_timeout": "The message took too long to send. Please try again.",
    "contact_delivery_unavailable": "Contact delivery is temporarily unavailable. Please try again later."
  },
  "notifications": {
    "context": "Contact message",
    "success": "Sent",
    "error": "Needs attention",
    "loading": "Sending",
    "update": "Update",
    "dismiss": "Dismiss notification"
  },
  "footer": {
    "copyright": "All rights reserved.",
    "socials_title": "Find me on",
    "credit": "Designed and developed by João Loureiro."
  }
}
""";

    private const string PortugueseJson = """
{
  "metadata": {
    "home_title": "João Loureiro | Desenvolvedor Full-Stack",
    "home_description": "Desenvolvedor full-stack focado em aplicações web e back-end com .NET, bancos relacionais, RabbitMQ, Angular e React.",
    "projects_title": "Projetos | João Loureiro",
    "projects_description": "Explore uma seleção de projetos construídos por João Loureiro, demonstrando habilidades em desenvolvimento front-end e back-end.",
    "about_title": "Sobre Mim | João Loureiro",
    "about_description": "Saiba mais sobre a jornada de João Loureiro na programação, sua filosofia técnica e interesses pessoais."
  },
  "engagement": {
    "meta_title": "João Loureiro | Desenvolvedor Full Stack Freelancer",
    "meta_description": "Contrate João Loureiro para desenvolvimento full stack, integrações .NET e automação de deploy. Contratos mensais e tarefas avulsas em dólares.",
    "skip": "Pular para o conteúdo",
    "headline": "Do código",
    "headline_accent": "à produção.",
    "intro": "Sou João, desenvolvedor full stack independente. Ajudo equipes a criar aplicações web, integrar APIs e colocar software em produção.",
    "start": "Vamos conversar",
    "view_work": "Ver projetos",
    "rates": "Preços",
    "monthly": "Contrato mensal",
    "monthly_price": "$3.000–$3.500",
    "monthly_unit": "por mês",
    "monthly_summary": "Desenvolvimento contínuo junto à sua equipe.",
    "task": "Tarefa avulsa",
    "task_price": "$150",
    "task_unit": "por tarefa",
    "task_summary": "Uma correção, integração ou mudança com escopo definido.",
    "work": "Projetos",
    "work_heading": "Projetos selecionados",
    "work_intro": "Aplicações e infraestrutura que desenvolvi, com as decisões por trás de cada projeto.",
    "work_empty": "Os projetos aparecem aqui à medida que são publicados.",
    "about_heading": "Como posso ajudar",
    "about_intro": "Atuo na aplicação, nas integrações e na infraestrutura que mantém o sistema rodando.",
    "backend_title": "APIs e integrações",
    "backend_description": "Desenvolvo serviços .NET, integro sistemas externos e trabalho com bancos de dados relacionais.",
    "frontend_title": "Aplicações web",
    "frontend_description": "Crio interfaces responsivas e conecto o front-end aos serviços do back-end.",
    "delivery_title": "Deploy e automação",
    "delivery_description": "Conteinerizo aplicações, automatizo fluxos e configuro pipelines de deploy.",
    "contact_heading": "O que você quer construir?",
    "contact_intro": "Conte o que você precisa, o prazo e o contexto técnico. Respondo por e-mail para alinharmos o escopo.",
    "form_engagement": "Modalidade",
    "form_unsure": "Quero conversar"
  },
  "navigation": {
    "home": "Início",
    "overview": "Visão geral",
    "interfaces": "Interfaces",
    "services": "Serviços",
    "automation": "Automação",
    "infrastructure": "Infraestrutura",
    "about": "Sobre",
    "tech": "Tecnologias",
    "projects": "Projetos",
    "posts": "Notas",
    "notes": "Notas",
    "contact": "Contato",
    "atlas_label": "Camadas do sistema",
    "primary_label": "Navegação principal",
    "secondary_label": "Atalhos da página",
    "social_label": "Redes e contato",
    "language": "Alterar idioma",
    "switch_to_light": "Usar tema claro",
    "switch_to_dark": "Usar tema escuro",
    "open_menu": "Abrir menu",
    "close_menu": "Fechar menu"
  },
  "hero": {
    "greeting": "Olá, eu sou o",
    "title": "Desenvolvedor de Software & Entusiasta Web",
    "subtitle": "Desenvolvedor de software com foco em .NET, bancos relacionais, RabbitMQ, Angular e React — hoje explorando agentes de IA e serviços auto-hospedados no meu homelab.",
    "role": "Desenvolvedor Full Stack",
    "rail_intro": "Construo sistemas completos — da interface à infraestrutura.",
    "manifesto_label": "Manifesto",
    "headline": "João Loureiro",
    "manifesto": "Clareza no código. Disciplina nos processos. Confiança no resultado.",
    "cta_button": "Ver projetos"
  },
  "atlas": {
    "interfaces": {
      "title": "Interfaces — experiências claras",
      "points": ["Rotas", "Componentes", "Estados", "Acessibilidade", "Performance"]
    },
    "services": {
      "title": "Serviços — regras e integrações",
      "points": ["Autenticação", "Validação", "Regras de negócio", "Integrações", "Domínio claro"]
    },
    "automation": {
      "title": "Automação — fluxos e processos",
      "points": ["Eventos", "Orquestração", "Agendamentos", "Idempotência", "Rastreabilidade"]
    },
    "infrastructure": {
      "title": "Infraestrutura — deploy e observabilidade",
      "points": ["Containers", "CI/CD", "Logs e métricas", "Alertas", "Operação"]
    }
  },
  "notes": {
    "title": "Notas de campo",
    "status": "Em breve",
    "description": "Reflexões sobre software, infraestrutura e o ofício de construir sistemas.",
    "published_status": "Publicadas",
    "published_description": "Notas sobre software, infraestrutura e as decisões por trás de sistemas completos.",
    "published_link": "Ler todas as notas",
    "purpose_title": "Próximo capítulo",
    "purpose": "Um arquivo editorial preparado para artigos, decisões técnicas e aprendizados de projeto."
  },
  "posts": {
    "title": "Notas de campo",
    "description": "Notas publicadas sobre software, infraestrutura e as decisões por trás de sistemas completos.",
    "list_label": "Notas de campo publicadas",
    "loading": "Carregando notas publicadas…",
    "empty": "Ainda não há notas publicadas.",
    "unavailable": "As notas de campo estão indisponíveis no momento. Tente novamente mais tarde.",
    "back_to_posts": "Voltar para as notas de campo",
    "related_title": "Notas relacionadas"
  },
  "about": {
    "title": "Sobre Mim",
    "statement": "Amplitude técnica com responsabilidade de ponta a ponta.",
    "paragraph1": "Sou um Desenvolvedor Full Stack movido pela paixão de construir, automatizar e escalar aplicações robustas. Minha jornada na tecnologia começou com o desejo de entender como sistemas complexos funcionam e evoluiu para uma carreira dedicada a criar soluções elegantes e de alta performance.",
    "paragraph2": "Com uma base sólida nos ecossistemas .NET e JavaScript, minha especialidade é a criação de aplicações web modernas utilizando tecnologias como .NET, Angular, Next.js e React. No entanto, minha curiosidade vai além do código. Como você pode ver no meu projeto de Homelab, estou profundamente envolvido com todo o ciclo de vida de desenvolvimento, incluindo CI/CD, conteinerização com Docker e gerenciamento de infraestrutura.",
    "paragraph3": "Eu prospero em desafios e estou constantemente explorando novas tecnologias, desde a administração de bancos de dados com MariaDB e Redis até a automação de fluxos de trabalho com N8N e filas de mensagens com RabbitMQ. Essa abordagem prática em DevOps e auto-hospedagem me permite construir não apenas funcionalidades, mas sistemas resilientes e escaláveis.",
    "principles": [
      {"title": "Clareza antes da complexidade", "description": "Arquitetura e código devem tornar decisões visíveis, não escondê-las."},
      {"title": "Sistema, não só funcionalidade", "description": "Interface, serviço, automação e operação fazem parte do mesmo resultado."},
      {"title": "Evolução com evidência", "description": "Medição, observabilidade e aprendizado orientam o próximo passo."}
    ]
  },
  "skills": {
    "title": "Campo técnico",
    "backend": "Backend & Linguagens",
    "frontend": "Frontend",
    "databases": "Bancos de Dados & Dados",
    "cloud": "Cloud & DevOps"
  },
  "projects": {
    "title": "Projetos",
    "intro": "Aplicações, APIs e experimentos de infraestrutura que desenvolvi.",
    "entry_label": "Projeto",
    "tech_used": "Tecnologias Utilizadas",
    "page_link": "Ver Detalhes",
    "live_link": "Ver ao Vivo",
    "repo_link": "Ver Código",
    "back_to_projects": "Voltar para Projetos",
    "about_project": "Sobre este Projeto",
    "not_found": "Projeto não encontrado",
    "unavailable": "Os projetos estão temporariamente indisponíveis. Tente novamente em instantes."
  },
  "contact": {
    "title": "Fale comigo",
    "subtitle": "Tem uma oportunidade, desafio técnico ou projeto em mente? Envie uma mensagem e respondo por e-mail.",
    "direct_label": "E-mail direto",
    "form_name": "Nome",
    "form_email": "E-mail",
    "form_message": "Mensagem",
    "field_required": "Este campo é obrigatório.",
    "privacy_note": "Seus dados serão usados apenas para responder a esta mensagem.",
    "submit_button": "Enviar mensagem",
    "status_sending": "Enviando sua mensagem...",
    "status_success": "Mensagem enviada com sucesso. Obrigado!",
    "status_error_failed": "Falha ao enviar a mensagem. Por favor, tente novamente.",
    "status_error_all_fields": "Todos os campos são obrigatórios.",
    "status_error_invalid_email": "Por favor, insira um email válido.",
    "status_error_generic": "Ocorreu um erro inesperado. Tente novamente mais tarde.",
    "server_unexpected_error": "Ocorreu um erro inesperado no servidor. Por favor, tente novamente mais tarde.",
    "smtp_auth_failed": "Falha na autenticação com o servidor de email.",
    "smtp_connection_failed": "Não foi possível conectar ao servidor de email.",
    "smtp_invalid_recipient": "Endereço de email do destinatário inválido.",
    "smtp_generic_error": "Ocorreu um erro ao enviar o email.",
    "contact_invalid_input": "Confira os campos do formulário e tente novamente.",
    "contact_payload_too_large": "Sua mensagem é grande demais. Reduza o texto e tente novamente.",
    "contact_throttled": "Muitas tentativas. Aguarde um pouco e tente novamente.",
    "contact_delivery_disabled": "O envio de contatos está temporariamente desativado. Tente novamente mais tarde.",
    "contact_delivery_timeout": "O envio demorou demais. Tente novamente.",
    "contact_delivery_unavailable": "O envio de contatos está temporariamente indisponível. Tente novamente mais tarde."
  },
  "notifications": {
    "context": "Mensagem de contato",
    "success": "Enviada",
    "error": "Requer atenção",
    "loading": "Enviando",
    "update": "Atualização",
    "dismiss": "Dispensar notificação"
  },
  "footer": {
    "copyright": "Todos os direitos reservados.",
    "socials_title": "Me encontre em",
    "credit": "Projetado e desenvolvido por João Loureiro."
  }
}
""";

    public static IReadOnlyList<LocaleCatalogEntry> CreateEntries()
    {
        using var englishDocument = JsonDocument.Parse(EnglishJson);
        using var portugueseDocument = JsonDocument.Parse(PortugueseJson);
        var english = FlattenEditableValues(englishDocument.RootElement);
        var portuguese = FlattenEditableValues(portugueseDocument.RootElement);

        return LocaleCatalogContract.Definitions.Select(definition =>
        {
            if (!english.TryGetValue(definition.Key, out var englishValue) ||
                !portuguese.TryGetValue(definition.Key, out var portugueseValue))
            {
                throw new InvalidOperationException($"The built-in locale catalog is missing '{definition.Key}'.");
            }

            if (englishValue.Type != definition.Type || portugueseValue.Type != definition.Type)
            {
                throw new InvalidOperationException($"The built-in locale catalog has an invalid type for '{definition.Key}'.");
            }

            return new LocaleCatalogEntry(definition.Key, definition.Type, englishValue, portugueseValue);
        }).ToArray();
    }

    public static JsonObject CreateMessages(
        IReadOnlyList<LocaleCatalogEntry> entries,
        LocaleCatalogLocale locale)
    {
        var json = locale == LocaleCatalogLocale.En ? EnglishJson : PortugueseJson;
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("The built-in locale catalog could not be parsed.");

        foreach (var entry in entries)
        {
            var value = locale == LocaleCatalogLocale.En ? entry.English : entry.Portuguese;
            var parts = entry.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            for (var index = 0; index < parts.Length - 1; index++)
            {
                current = current[parts[index]]?.AsObject()
                    ?? throw new InvalidOperationException($"The built-in locale catalog has an invalid path '{entry.Key}'.");
            }

            current[parts[^1]] = value.Type == LocaleCatalogValueType.String
                ? JsonValue.Create(value.Scalar)
                : new JsonArray(value.List!.Select(item => (JsonNode?)JsonValue.Create(item)).ToArray());
        }

        return root;
    }

    private static IReadOnlyDictionary<string, LocaleCatalogValue> FlattenEditableValues(JsonElement root)
    {
        var values = new Dictionary<string, LocaleCatalogValue>(StringComparer.Ordinal);
        Visit(root, string.Empty, values);
        return values;
    }

    private static void Visit(
        JsonElement value,
        string path,
        IDictionary<string, LocaleCatalogValue> values)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                Visit(property.Value, string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}", values);
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            values[path] = LocaleCatalogValue.FromString(value.GetString()!);
            return;
        }

        if (value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String))
        {
            values[path] = LocaleCatalogValue.FromList(value.EnumerateArray().Select(item => item.GetString()!).ToArray());
        }
    }
}
