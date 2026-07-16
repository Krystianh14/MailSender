# MailSender API

MailSender is a .NET backend application for registering client applications and sending email messages through external email providers.

The project demonstrates a layered Clean Architecture approach, JWT-based authentication, Entity Framework Core persistence, Swagger/OpenAPI documentation, multiple email provider integrations, and a small demo WebClient generated from the OpenAPI specification.

## Features

- Client application registration
- JWT generation and authorization
- Email sending through configurable providers
- Support for Fake, Brevo, and Mailtrap providers
- Email delivery history scoped to the authenticated client
- Successful and failed delivery logging
- Swagger/OpenAPI documentation
- Entity Framework Core repositories
- Demo WebClient generated from the OpenAPI specification

## Technology stack

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- JWT Bearer authentication
- Swagger / OpenAPI
- HttpClientFactory
- Vite
- JavaScript / TypeScript
- `openapi-typescript-codegen`

## Architecture

The solution is divided into four main projects:

```text
MailSender/
├── MailSender.Api/              # HTTP API, controllers, authentication and startup
├── MailSender.Application/      # Use cases, DTOs, interfaces and application services
├── MailSender.Domain/           # Domain entities
├── MailSender.Infrastructure/   # EF Core, repositories, JWT and email providers
└── WebClient/                   # Demo client generated from OpenAPI
```

Dependency direction:

```text
MailSender.Api
├── MailSender.Application
└── MailSender.Infrastructure

MailSender.Infrastructure
├── MailSender.Application
└── MailSender.Domain

MailSender.Application
└── MailSender.Domain

MailSender.Domain
└── no project dependencies
```

## Request flow

```text
1. Register a client application
   POST /client-app/register
            │
            ▼
   Validate registration data
            │
            ▼
   Store the client application
            │
            ▼
   Generate and return a JWT

2. Send an email
   POST /mail/send
            │
            ▼
   Validate the Bearer token
            │
            ▼
   Resolve the authenticated client
            │
            ▼
   Process the message
            │
            ▼
   Send it through the selected provider
            │
            ▼
   Store a success or failure log

3. Read delivery history
   GET /mail-log
            │
            ▼
   Resolve the authenticated client
            │
            ▼
   Return only logs owned by that client
```

The JWT contains the following claims:

- `client_application_id`
- `app_id`
- `app_name`

## API endpoints

| Method | Endpoint               | Authentication | Description                                                |
| ------ | ---------------------- | -------------: | ---------------------------------------------------------- |
| `POST` | `/client-app/register` |             No | Registers a client application and returns a JWT           |
| `POST` | `/mail/send`           |     Bearer JWT | Sends an email through the selected provider               |
| `GET`  | `/mail-log`            |     Bearer JWT | Returns delivery logs for the authenticated client         |
| `GET`  | `/mail-log/{id}`       |     Bearer JWT | Returns one delivery log owned by the authenticated client |

### Register a client application

```http
POST /client-app/register
Content-Type: application/json
```

```json
{
  "appId": "demo-app",
  "appName": "Demo App",
  "pass": "dwa13"
}
```

Example response:

```json
{
  "appId": "demo-app",
  "appName": "Demo App",
  "key": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Send an email

```http
POST /mail/send
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "to": "recipient@example.com",
  "subject": "Status question?",
  "body": "Hello from MailSender."
}
```

### Read delivery logs

```http
GET /mail-log
Authorization: Bearer <token>
```

```http
GET /mail-log/{id}
Authorization: Bearer <token>
```

## Message processing rules

Before an email is sent, the application applies two demonstration rules:

- If the subject ends with `?`, the `[Q]` prefix is added.
- Configured surnames found in the body can be wrapped with a marker.

These rules are included to demonstrate application-layer processing before provider execution.

## Email providers

The active provider is selected through `MailProvider:SelectedProvider`.

| Value      | Implementation               | Behaviour                                                     |
| ---------- | ---------------------------- | ------------------------------------------------------------- |
| `Fake`     | `FakeMailSenderProvider`     | Does not send a real email; writes the message to the console |
| `Brevo`    | `BrevoMailSenderProvider`    | Sends email through the Brevo API                             |
| `Mailtrap` | `MailtrapMailSenderProvider` | Sends email through Mailtrap sandbox or production API        |

`Fake` is the recommended provider for initial local testing.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js and npm, only for the demo WebClient
- Optional Brevo or Mailtrap account for real email delivery

### Clone and run the API

```bash
git clone https://github.com/Krystianh14/MailSender.git
cd MailSender

dotnet restore
dotnet run --project MailSender.Api
```

The API uses the address configured in `MailSender.Api/Properties/launchSettings.json`.

Default local addresses:

```text
http://localhost:5235
http://localhost:5235/swagger
```

You can also use `MailSender.Api/MailSender.Api.http` to test the main API flow.

## Configuration

The repository should contain only safe placeholder values. Store real secrets in .NET User Secrets or environment variables.

Example `appsettings.json` structure:

```json
{
  "Students": [
    {
      "Surname": "Example",
      "IndexSuffix": "13"
    }
  ],
  "Jwt": {
    "SecretKey": "CHANGE_ME_MINIMUM_32_CHARACTERS",
    "Issuer": "MailSender",
    "Audience": "MailSenderClients",
    "ExpirationDays": 90
  },
  "MailProvider": {
    "SelectedProvider": "Fake"
  },
  "Brevo": {
    "ApiKey": "",
    "SenderEmail": "",
    "SenderName": "MailSender"
  },
  "Mailtrap": {
    "ApiKey": "",
    "SenderEmail": "",
    "SenderName": "MailSender",
    "UseSandbox": true,
    "InboxId": 0
  }
}
```

### Configure local secrets

Initialize User Secrets if necessary:

```bash
dotnet user-secrets init --project MailSender.Api
```

Set the JWT signing key:

```bash
dotnet user-secrets set \
  "Jwt:SecretKey" \
  "your-development-secret-key-minimum-32-characters" \
  --project MailSender.Api
```

Optional Brevo configuration:

```bash
dotnet user-secrets set "Brevo:ApiKey" "your-brevo-api-key" --project MailSender.Api
dotnet user-secrets set "Brevo:SenderEmail" "verified-sender@example.com" --project MailSender.Api
dotnet user-secrets set "Brevo:SenderName" "MailSender" --project MailSender.Api
```

Optional Mailtrap configuration:

```bash
dotnet user-secrets set "Mailtrap:ApiKey" "your-mailtrap-api-key" --project MailSender.Api
dotnet user-secrets set "Mailtrap:SenderEmail" "sender@example.com" --project MailSender.Api
dotnet user-secrets set "Mailtrap:SenderName" "MailSender" --project MailSender.Api
```

Never commit real JWT signing keys, provider API keys, or local development settings.

## Database

The current version uses EF Core InMemory Database to keep local setup simple.

This means:

- no external database is required,
- registered applications and delivery logs are stored only while the API process is running,
- all stored data is lost after an application restart.

Persistent PostgreSQL storage and EF Core migrations are planned improvements.

## Testing with Swagger

1. Run the API.
2. Open `http://localhost:5235/swagger`.
3. Call `POST /client-app/register`.
4. Copy the `key` returned in the response.
5. Click **Authorize**.
6. Enter:

```text
Bearer <your-token>
```

7. Call `POST /mail/send`, `GET /mail-log`, or `GET /mail-log/{id}`.

## Demo WebClient

The `WebClient` directory contains a small browser client for demonstrating the email-sending flow without Swagger.

```text
WebClient/
├── index.html
├── app.js
├── style.css
├── openapi.json
├── package.json
├── package-lock.json
└── tsconfig.generated.json
```

Generated clients are intentionally excluded from the repository:

```text
WebClient/generated-ts/
WebClient/generated-js/
```

### Run the WebClient

First, run the backend in one terminal:

```bash
dotnet run --project MailSender.Api
```

Then open another terminal:

```bash
cd WebClient
npm install
npm run generate:ts
npm run build:generated-js
npm run dev
```

The development client is available at:

```text
http://127.0.0.1:5500
```

The backend must remain available at:

```text
http://localhost:5235
```

### Available npm scripts

| Command                      | Description                                             |
| ---------------------------- | ------------------------------------------------------- |
| `npm run generate:ts`        | Generates the TypeScript API client from `openapi.json` |
| `npm run build:generated-js` | Compiles the generated TypeScript client to JavaScript  |
| `npm run dev`                | Starts the Vite development server                      |
| `npm run build`              | Creates a production frontend build                     |

## Security notes

- Keep JWT signing keys outside the repository.
- Keep Brevo and Mailtrap API keys outside the repository.
- Use sufficiently long JWT signing keys.
- Do not treat the current registration password mechanism as production authentication.
- The API scopes mail logs by the authenticated `client_application_id`.
- Use HTTPS outside local development.
