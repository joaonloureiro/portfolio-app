using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "ImageReference", "LiveUrl", "PublishedAt", "RepositoryUrl", "Revision", "SortOrder", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "/Portfolio.png", "https://joaoloureiro.dev.br", null, "https://github.com/joaonloureiro/portfolio-app", 1L, 1, "Published", new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "/ProxmoxServer.png", null, null, null, 1L, 2, "Published", new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "/Happy.png", "https://happy.joaoloureiro.dev.br/", null, "https://github.com/joaonloureiro/happy-app", 1L, 3, "Published", new DateTime(2026, 9, 9, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProjectLocalizedContent",
                columns: new[] { "Locale", "ProjectId", "Details", "FeatureHeading", "Summary", "Title" },
                values: new object[,]
                {
                    { "en", 1, "This portfolio is a full-stack application designed to showcase my skills and projects. It features a modern, decoupled architecture with a frontend in Next.js and a backend in .NET 8. The project is automatically deployed via Gitea Actions, with PM2 managing the processes on the server for zero-downtime restarts.", "Key Features:", "The very site you are on now! A full-stack application built with Next.js, Tailwind CSS, and a .NET 8 backend to showcase my work and skills.", "Personal Portfolio" },
                    { "pt", 1, "Este portfólio é uma aplicação full-stack projetada para mostrar minhas habilidades e projetos. Possui uma arquitetura moderna e desacoplada, com um frontend em Next.js e um backend em .NET 8. O projeto é implantado automaticamente via Gitea Actions, com o PM2 gerenciando os processos no servidor para reinicializações sem tempo de inatividade.", "Principais Características:", "O próprio site em que você está agora! Uma aplicação full-stack construída com Next.js, Tailwind CSS e um backend .NET 8 para exibir meu trabalho e habilidades.", "Portfólio Pessoal" },
                    { "en", 2, "This project is my personal Homelab, a self-hosted cloud environment that serves as a practical playground for exploring DevOps concepts and hosting production-level services. It demonstrates my ability to manage a full-stack ecosystem, from networking and security to CI/CD and automation.", "Hosted Services & Skills:", "A self-hosted infrastructure running on Proxmox, showcasing a wide range of DevOps and backend services.", "Homelab & DevOps Playground" },
                    { "pt", 2, "Este projeto é o meu Homelab pessoal, um ambiente de nuvem auto-hospedado que serve como um playground prático para explorar conceitos de DevOps e hospedar serviços de nível de produção. Ele demonstra minha capacidade de gerenciar um ecossistema full-stack, desde redes e segurança até CI/CD e automação.", "Serviços Hospedados & Habilidades:", "Uma infraestrutura auto-hospedada rodando em Proxmox, demonstrando uma vasta gama de serviços de DevOps e backend.", "Homelab & Playground DevOps" },
                    { "en", 3, "Happy is a full-stack web application designed to connect people with orphanages, making it easier to schedule visits. Developed as part of Rocketseat's Next Level Week, this project showcases a complete technology stack, from a modern frontend to a robust backend and database.", "Core Functionalities:", "A full-stack application to connect people with orphanages, built with React, Node.js, and a self-hosted MariaDB.", "Happy" },
                    { "pt", 3, "Happy é uma aplicação web full-stack projetada para conectar pessoas a orfanatos, facilitando o agendamento de visitas. Desenvolvido como parte da Next Level Week da Rocketseat, este projeto exibe uma stack de tecnologia completa, desde um frontend moderno até um backend e banco de dados robustos.", "Funcionalidades Principais:", "Uma aplicação full-stack para conectar pessoas a orfanatos, construída com React, Node.js e um MariaDB auto-hospedado.", "Happy" }
                });

            migrationBuilder.InsertData(
                table: "ProjectTechnologies",
                columns: new[] { "Position", "ProjectId", "Value" },
                values: new object[,]
                {
                    { 1, 1, "Next.js" },
                    { 2, 1, ".NET 8" },
                    { 3, 1, "TypeScript" },
                    { 4, 1, "Tailwind CSS" },
                    { 1, 2, "Proxmox" },
                    { 2, 2, "Docker" },
                    { 3, 2, "Traefik" },
                    { 4, 2, "Observability" },
                    { 1, 3, "React" },
                    { 2, 3, "Node.js" },
                    { 3, 3, "TypeScript" },
                    { 4, 3, "MariaDB" }
                });

            migrationBuilder.InsertData(
                table: "ProjectFeatures",
                columns: new[] { "Locale", "Position", "ProjectId", "Value" },
                values: new object[,]
                {
                    { "en", 1, 1, "**Internationalization (i18n):** Fully translated in English and Portuguese with `next-intl`." },
                    { "en", 2, 1, "**Responsive Design:** Optimized for all devices using Tailwind CSS." },
                    { "en", 3, 1, "**Interactive UI:** Includes a theme switcher (dark/light mode) and toast notifications." },
                    { "en", 4, 1, "**Robust Backend:** A .NET 8 API handles contact form submissions securely." },
                    { "pt", 1, 1, "**Internacionalização (i18n):** Totalmente traduzido para inglês e português com `next-intl`." },
                    { "pt", 2, 1, "**Design Responsivo:** Otimizado para todos os dispositivos usando Tailwind CSS." },
                    { "pt", 3, 1, "**UI Interativa:** Inclui um seletor de tema (modo claro/escuro) e notificações para feedback de formulários." },
                    { "pt", 4, 1, "**Backend Robusto:** Uma API .NET 8 lida com os envios do formulário de contato de forma segura usando o MailKit." },
                    { "en", 1, 2, "**CI/CD & Version Control:** Self-hosted **Gitea** for source code management and continuous integration." },
                    { "en", 2, 2, "**Containerization & Orchestration:** **Docker** for containerizing applications, managed within a **Proxmox** virtualized environment." },
                    { "en", 3, 2, "**Networking & Security:** **Traefik** as a reverse proxy for routing traffic, with **Authelia** for single sign-on (SSO) and **Cloudflare Tunnels** for secure external access." },
                    { "en", 4, 2, "**Databases:** Hosting and management of **MariaDB** and **Redis** for various applications." },
                    { "en", 5, 2, "**Monitoring & Observability:** A full monitoring stack with **Prometheus** for data collection, **Grafana** for visualization, and **Pi-hole** for network-wide ad blocking." },
                    { "en", 6, 2, "**Automation & Messaging:** **N8N** for workflow automation and **RabbitMQ** for message queuing between services." },
                    { "en", 7, 2, "**AI & Machine Learning:** Self-hosted **Ollama** for experimenting with large language models locally." },
                    { "pt", 1, 2, "**CI/CD & Controle de Versão:** **Gitea** auto-hospedado para gerenciamento de código-fonte e integração contínua." },
                    { "pt", 2, 2, "**Conteinerização & Orquestração:** **Docker** para conteinerizar aplicações, gerenciado em um ambiente virtualizado **Proxmox**." },
                    { "pt", 3, 2, "**Redes & Segurança:** **Traefik** como proxy reverso para roteamento de tráfego, com **Authelia** para single sign-on (SSO) e **Cloudflare Tunnels** para acesso externo seguro." },
                    { "pt", 4, 2, "**Bancos de Dados:** Hospedagem e gerenciamento de **MariaDB** e **Redis** para diversas aplicações." },
                    { "pt", 5, 2, "**Monitoramento & Observabilidade:** Uma stack completa de monitoramento com **Prometheus** para coleta de dados, **Grafana** para visualização e **Pi-hole** para bloqueio de anúncios em toda a rede." },
                    { "pt", 6, 2, "**Automação & Mensageria:** **N8N** para automação de fluxos de trabalho e **RabbitMQ** para enfileiramento de mensagens entre serviços." },
                    { "pt", 7, 2, "**IA & Machine Learning:** **Ollama** auto-hospedado para experimentar com grandes modelos de linguagem localmente." },
                    { "en", 1, 3, "**Interactive Map:** Users can view registered orphanages on a dynamic Google Map." },
                    { "en", 2, 3, "**Detailed Information:** Each orphanage has a dedicated page with photos and visiting hours." },
                    { "en", 3, 3, "**User Contributions:** A complete form allows users to register new orphanages." },
                    { "en", 4, 3, "**Modern Stack:** Built with **React 18** for a fast, responsive UI and a **Node.js/Express** backend connected to a self-hosted **MariaDB**." },
                    { "pt", 1, 3, "**Mapa Interativo:** Os usuários podem visualizar orfanatos cadastrados em um mapa dinâmico do Google." },
                    { "pt", 2, 3, "**Informações Detalhadas:** Cada orfanato possui uma página dedicada com fotos e horários de visita." },
                    { "pt", 3, 3, "**Contribuições de Usuários:** Um formulário completo permite que os usuários cadastrem novos orfanatos." },
                    { "pt", 4, 3, "**Stack Moderna:** Construído com **React 18** para uma UI rápida e responsiva, e um backend **Node.js/Express** conectado a um **MariaDB** auto-hospedado." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 1, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 2, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 3, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 4, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 1, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 2, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 3, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 4, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 1, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 2, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 3, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 4, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 5, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 6, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 7, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 1, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 2, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 3, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 4, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 5, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 6, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 7, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 1, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 2, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 3, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "en", 4, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 1, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 2, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 3, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectFeatures",
                keyColumns: new[] { "Locale", "Position", "ProjectId" },
                keyValues: new object[] { "pt", 4, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 4, 1 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 2, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 3, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 4, 2 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 3, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectTechnologies",
                keyColumns: new[] { "Position", "ProjectId" },
                keyValues: new object[] { 4, 3 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "en", 1 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "pt", 1 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "en", 2 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "pt", 2 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "en", 3 });

            migrationBuilder.DeleteData(
                table: "ProjectLocalizedContent",
                keyColumns: new[] { "Locale", "ProjectId" },
                keyValues: new object[] { "pt", 3 });

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
