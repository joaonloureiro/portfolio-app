# JoaoLoureiro.Portfolio Backend

This is the backend for my portfolio application. It is a simple .NET 8 API that handles sending contact messages from the frontend.

## Technologies

*   .NET 8
*   ASP.NET Core
*   Docker
*   Serilog for logging
*   FluentValidation for request validation
*   AutoMapper for object mapping

## Project Structure

The solution follows a clean architecture pattern:

*   `JoaoLoureiro.Portfolio.Api`: The main API project, containing the endpoint for sending emails.
*   `JoaoLoureiro.Portfolio.Application`: Contains the core business logic and interfaces.
*   `JoaoLoureiro.Portfolio.Domain`: Contains the domain entities.
*   `JoaoLoureiro.Portfolio.Infrastructure`: Contains the implementation of services, such as the email sender.

## Getting Started

### Prerequisites

*   .NET 8 SDK
*   An SMTP server for sending emails.

### Running the application

1.  Clone the repository.
2.  Navigate to the `backend` directory.
3.  Configure the `SmtpSettings` in `JoaoLoureiro.Portfolio.Api/appsettings.json`.
4.  Run the application using the following command:

    ```bash
    dotnet run --project JoaoLoureiro.Portfolio.Api
    ```

## API Endpoints

### POST /api/email/send

This endpoint accepts a contact form submission and sends it as an email.

**Request Body:**

```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "message": "Hello, I would like to get in touch with you."
}
```

**Responses:**

*   `200 OK`: If the message was sent successfully.
*   `400 Bad Request`: If the request is invalid.
*   `500 Internal Server Error`: If an unexpected error occurs.

### GET /health

This endpoint returns the health of the application, including the status of the SMTP connection.

## Configuration

The application is configured using the `appsettings.json` file in the `JoaoLoureiro.Portfolio.Api` project.

*   `SmtpSettings`: Configuration for the SMTP server.
*   `CorsOrigins`: A comma-separated list of allowed origins for CORS.
*   `ProxyIP`: The IP address of a trusted proxy.
*   `Serilog`: Configuration for logging.
