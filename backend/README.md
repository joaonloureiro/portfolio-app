# Portfolio API Backend

This is a simple and efficient backend server built with Node.js and Express. Its primary purpose is to handle the contact form submissions from the portfolio frontend, sending emails via SMTP using Nodemailer.

## ✨ Features

* **Email Service**: Securely sends emails from the contact form.
* **Input Validation**: Ensures required fields (`name`, `email`, `message`) are present and the email format is valid.
* **CORS Enabled**: Configured with CORS to only accept requests from the frontend application.
* **Specific Error Handling**: Returns structured error keys for different SMTP and server issues, allowing the frontend to display translated, user-friendly messages.
* **Health Check**: Includes a `/api/health` endpoint to easily verify if the server is running.

## 🛠️ Tech Stack

* **Runtime**: [Node.js](https://nodejs.org/)
* **Framework**: [Express.js](https://expressjs.com/)
* **Email**: [Nodemailer](https://nodemailer.com/)
* **Environment Variables**: [Dotenv](https://github.com/motdotla/dotenv)
* **Cross-Origin Requests**: [CORS](https://github.com/expressjs/cors)

## 🚀 Getting Started

### Prerequisites

* Node.js (v18 or later)
* npm

### Installation & Setup

1.  **Navigate to the backend directory:**
    ```bash
    cd backend
    ```

2.  **Install dependencies:**
    ```bash
    npm install
    ```

3.  **Create an environment file:**
    Create a file named `.env` in the `backend` directory and populate it with your credentials. **Do not commit this file to version control.**

    **.env.example**
    ```env
    # The port for the backend server
    BACKEND_PORT=3001

    # Your frontend URL for CORS
    FRONTEND_URL=http://localhost:3000

    # Nodemailer SMTP Configuration
    SMTP_HOST=smtp.example.com
    SMTP_PORT=587
    SMTP_SECURE=false
    SMTP_USER=your-email@example.com
    SMTP_PASS=your-email-password
    YOUR_RECEIVING_EMAIL=your-personal-email@example.com
    ```

4.  **Run the server:**
    ```bash
    npm start
    ```
    The server will start on the port defined in your `.env` file (e.g., `3001`).

## 📝 API Endpoints

### Health Check

* **GET** `/api/health`
    * **Description**: Checks the server status.
    * **Success Response (200)**:
        ```json
        { "status": "UP", "message": "Backend is running" }
        ```

### Send Email

* **POST** `/api/email/send`
    * **Description**: Processes and sends a contact form submission.
    * **Request Body**:
        ```json
        {
          "name": "string",
          "email": "string",
          "message": "string"
        }
        ```
    * **Success Response (200)**:
        ```json
        { "message": "Message sent successfully!" }
        ```
    * **Error Responses (4xx/5xx)**: Returns a JSON object with an `errorKey` for the frontend to translate.
        ```json
        { "errorKey": "smtp_auth_failed" }
        ```