namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

internal sealed record ProjectSeed(
    int Id,
    int SortOrder,
    string ImageReference,
    string? RepositoryUrl,
    string? LiveUrl,
    IReadOnlyList<string> Technologies,
    ProjectSeedLocale English,
    ProjectSeedLocale Portuguese)
{
    private static readonly DateTime SeedUpdatedAt = new(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);

    public ProjectRecord ToRecord() => new()
    {
        Id = Id,
        SortOrder = SortOrder,
        Status = "Published",
        ImageReference = ImageReference,
        RepositoryUrl = RepositoryUrl,
        LiveUrl = LiveUrl,
        PublishedAt = null,
        UpdatedAt = SeedUpdatedAt,
        Revision = 1
    };

    public IEnumerable<ProjectLocalizedContentRecord> ToLocalizedContentRecords()
    {
        yield return English.ToRecord(Id, "en");
        yield return Portuguese.ToRecord(Id, "pt");
    }

    public IEnumerable<ProjectFeatureRecord> ToFeatureRecords()
    {
        foreach (var (locale, content) in new[] { ("en", English), ("pt", Portuguese) })
        {
            foreach (var (value, position) in content.Features.Select((value, index) => (value, index + 1)))
            {
                yield return new ProjectFeatureRecord
                {
                    ProjectId = Id,
                    Locale = locale,
                    Position = position,
                    Value = value
                };
            }
        }
    }

    public IEnumerable<ProjectTechnologyRecord> ToTechnologyRecords()
    {
        return Technologies.Select((value, index) => new ProjectTechnologyRecord
        {
            ProjectId = Id,
            Position = index + 1,
            Value = value
        });
    }
}

internal sealed record ProjectSeedLocale(
    string Title,
    string Summary,
    string Details,
    string FeatureHeading,
    IReadOnlyList<string> Features)
{
    public ProjectLocalizedContentRecord ToRecord(int projectId, string locale) => new()
    {
        ProjectId = projectId,
        Locale = locale,
        Title = Title,
        Summary = Summary,
        Details = Details,
        FeatureHeading = FeatureHeading
    };
}

internal static class ProjectSeedData
{
    public static IReadOnlyList<ProjectSeed> Projects { get; } =
    [
        new(
            1,
            1,
            "/Portfolio.png",
            "https://github.com/joaonloureiro/portfolio-app",
            "https://joaoloureiro.dev.br",
            ["Next.js", ".NET 8", "TypeScript", "Tailwind CSS"],
            new(
                "Personal Portfolio",
                "The very site you are on now! A full-stack application built with Next.js, Tailwind CSS, and a .NET 8 backend to showcase my work and skills.",
                "This portfolio is a full-stack application designed to showcase my skills and projects. It features a modern, decoupled architecture with a frontend in Next.js and a backend in .NET 8. The project is automatically deployed via Gitea Actions, with PM2 managing the processes on the server for zero-downtime restarts.",
                "Key Features:",
                [
                    "**Internationalization (i18n):** Fully translated in English and Portuguese with `next-intl`.",
                    "**Responsive Design:** Optimized for all devices using Tailwind CSS.",
                    "**Interactive UI:** Includes a theme switcher (dark/light mode) and toast notifications.",
                    "**Robust Backend:** A .NET 8 API handles contact form submissions securely."
                ]),
            new(
                "Portfólio Pessoal",
                "O próprio site em que você está agora! Uma aplicação full-stack construída com Next.js, Tailwind CSS e um backend .NET 8 para exibir meu trabalho e habilidades.",
                "Este portfólio é uma aplicação full-stack projetada para mostrar minhas habilidades e projetos. Possui uma arquitetura moderna e desacoplada, com um frontend em Next.js e um backend em .NET 8. O projeto é implantado automaticamente via Gitea Actions, com o PM2 gerenciando os processos no servidor para reinicializações sem tempo de inatividade.",
                "Principais Características:",
                [
                    "**Internacionalização (i18n):** Totalmente traduzido para inglês e português com `next-intl`.",
                    "**Design Responsivo:** Otimizado para todos os dispositivos usando Tailwind CSS.",
                    "**UI Interativa:** Inclui um seletor de tema (modo claro/escuro) e notificações para feedback de formulários.",
                    "**Backend Robusto:** Uma API .NET 8 lida com os envios do formulário de contato de forma segura usando o MailKit."
                ])),
        new(
            2,
            2,
            "/ProxmoxServer.png",
            null,
            null,
            ["Proxmox", "Docker", "Traefik", "Observability"],
            new(
                "Homelab & DevOps Playground",
                "A self-hosted infrastructure running on Proxmox, showcasing a wide range of DevOps and backend services.",
                "This project is my personal Homelab, a self-hosted cloud environment that serves as a practical playground for exploring DevOps concepts and hosting production-level services. It demonstrates my ability to manage a full-stack ecosystem, from networking and security to CI/CD and automation.",
                "Hosted Services & Skills:",
                [
                    "**CI/CD & Version Control:** Self-hosted **Gitea** for source code management and continuous integration.",
                    "**Containerization & Orchestration:** **Docker** for containerizing applications, managed within a **Proxmox** virtualized environment.",
                    "**Networking & Security:** **Traefik** as a reverse proxy for routing traffic, with **Authelia** for single sign-on (SSO) and **Cloudflare Tunnels** for secure external access.",
                    "**Databases:** Hosting and management of **MariaDB** and **Redis** for various applications.",
                    "**Monitoring & Observability:** A full monitoring stack with **Prometheus** for data collection, **Grafana** for visualization, and **Pi-hole** for network-wide ad blocking.",
                    "**Automation & Messaging:** **N8N** for workflow automation and **RabbitMQ** for message queuing between services.",
                    "**AI & Machine Learning:** Self-hosted **Ollama** for experimenting with large language models locally."
                ]),
            new(
                "Homelab & Playground DevOps",
                "Uma infraestrutura auto-hospedada rodando em Proxmox, demonstrando uma vasta gama de serviços de DevOps e backend.",
                "Este projeto é o meu Homelab pessoal, um ambiente de nuvem auto-hospedado que serve como um playground prático para explorar conceitos de DevOps e hospedar serviços de nível de produção. Ele demonstra minha capacidade de gerenciar um ecossistema full-stack, desde redes e segurança até CI/CD e automação.",
                "Serviços Hospedados & Habilidades:",
                [
                    "**CI/CD & Controle de Versão:** **Gitea** auto-hospedado para gerenciamento de código-fonte e integração contínua.",
                    "**Conteinerização & Orquestração:** **Docker** para conteinerizar aplicações, gerenciado em um ambiente virtualizado **Proxmox**.",
                    "**Redes & Segurança:** **Traefik** como proxy reverso para roteamento de tráfego, com **Authelia** para single sign-on (SSO) e **Cloudflare Tunnels** para acesso externo seguro.",
                    "**Bancos de Dados:** Hospedagem e gerenciamento de **MariaDB** e **Redis** para diversas aplicações.",
                    "**Monitoramento & Observabilidade:** Uma stack completa de monitoramento com **Prometheus** para coleta de dados, **Grafana** para visualização e **Pi-hole** para bloqueio de anúncios em toda a rede.",
                    "**Automação & Mensageria:** **N8N** para automação de fluxos de trabalho e **RabbitMQ** para enfileiramento de mensagens entre serviços.",
                    "**IA & Machine Learning:** **Ollama** auto-hospedado para experimentar com grandes modelos de linguagem localmente."
                ])),
        new(
            3,
            3,
            "/Happy.png",
            "https://github.com/joaonloureiro/happy-app",
            "https://happy.joaoloureiro.dev.br/",
            ["React", "Node.js", "TypeScript", "MariaDB"],
            new(
                "Happy",
                "A full-stack application to connect people with orphanages, built with React, Node.js, and a self-hosted MariaDB.",
                "Happy is a full-stack web application designed to connect people with orphanages, making it easier to schedule visits. Developed as part of Rocketseat's Next Level Week, this project showcases a complete technology stack, from a modern frontend to a robust backend and database.",
                "Core Functionalities:",
                [
                    "**Interactive Map:** Users can view registered orphanages on a dynamic Google Map.",
                    "**Detailed Information:** Each orphanage has a dedicated page with photos and visiting hours.",
                    "**User Contributions:** A complete form allows users to register new orphanages.",
                    "**Modern Stack:** Built with **React 18** for a fast, responsive UI and a **Node.js/Express** backend connected to a self-hosted **MariaDB**."
                ]),
            new(
                "Happy",
                "Uma aplicação full-stack para conectar pessoas a orfanatos, construída com React, Node.js e um MariaDB auto-hospedado.",
                "Happy é uma aplicação web full-stack projetada para conectar pessoas a orfanatos, facilitando o agendamento de visitas. Desenvolvido como parte da Next Level Week da Rocketseat, este projeto exibe uma stack de tecnologia completa, desde um frontend moderno até um backend e banco de dados robustos.",
                "Funcionalidades Principais:",
                [
                    "**Mapa Interativo:** Os usuários podem visualizar orfanatos cadastrados em um mapa dinâmico do Google.",
                    "**Informações Detalhadas:** Cada orfanato possui uma página dedicada com fotos e horários de visita.",
                    "**Contribuições de Usuários:** Um formulário completo permite que os usuários cadastrem novos orfanatos.",
                    "**Stack Moderna:** Construído com **React 18** para uma UI rápida e responsiva, e um backend **Node.js/Express** conectado a um **MariaDB** auto-hospedado."
                ]))
    ];
}
