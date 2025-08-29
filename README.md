# Full-Stack Portfolio Website

Welcome to my personal portfolio project! This repository contains the complete source code for my website, built as a full-stack application with a modern, decoupled architecture.

[![Portfolio](https://img.shields.io/badge/Live_Demo-joaoloureiro.dev.br-blue?style=for-the-badge&logo=icloud)](https://joaoloureiro.dev.br)

![Deploy badge](https://gitea.joaoloureiro.dev.br/JoaoLoureiro/portfolio-app/actions/workflows/deploy.yml/badge.svg)

 ## 🏛️ Project Architecture
The project is organized into two main components in a monorepo-style structure:

* **`/frontend`**: A responsive and feature-rich Next.js application that serves as the user-facing portfolio.
* **`/backend`**: A lightweight Node.js and Express server that provides API services to the frontend, primarily for handling contact form submissions.

## ✨ Core Features
### Frontend

* **Modern Framework**: Built with **Next.js 15** and **React 19**.
* **Internationalization (i18n)**: Fully translated in English and Portuguese using `next-intl`.
* **Responsive Design**: Optimized for a seamless experience on all devices with **Tailwind CSS**.
* **Interactive UI**: Features a theme switcher for dark/light modes and user-friendly toast notifications for form feedback.

### Backend
* **Robust API**: An Express.js server to handle business logic.
* **Email Service**: Uses **Nodemailer** to securely send emails via an SMTP server.
* **Structured Error Handling**: Sends clear error keys to the frontend for better UX and easier debugging.

## 🚀 Getting Started: Running the Full Stack Locally

To run the entire project locally, you will need to start both the frontend and backend servers concurrently.

### 1. Set Up and Run the Backend

First, get the API server running.

```bash
# 1. Navigate to the backend directory
cd backend

# 2. Install dependencies
npm install

# 3. Create a .env file from the .env.example template
#    and fill in your SMTP credentials:
#    - SMTP_HOST: The SMTP server address (e.g., smtp.example.com)
#    - SMTP_PORT: The port number for the SMTP server (e.g., 587)
#    - SMTP_USER: Your SMTP username
#    - SMTP_PASS: Your SMTP password
# The server will be running on http://localhost:3001 (by default). 
# Note: The port can be changed via the `.env` file or configuration settings.

# 4. Start the backend server
npm start
# The server will be running on http://localhost:3001 (by default)
```

### 2. Set Up and Run the FrontEnd

```bash
# 1. Navigate to the frontend directory
cd frontend

# 2. Install dependencies
npm install

# 3. Create a .env.local file for the frontend
echo "NEXT_PUBLIC_BACKEND_URL=http://localhost:3001" > .env.local

# 4. Start the frontend development server
npm run dev
# The site will be available at http://localhost:3000
```

### ⚙️ Deployment

This project is configured for automated deployment using Gitea Actions. A workflow file at `.gitea/workflows/deploy.yml` orchestrates the build and deployment process. 

On the server, PM2 is used as a process manager to keep both the frontend and backend applications online and to enable zero-downtime restarts.

### 📜 License

This project is licensed under the MIT License. See the LICENSE file for details.