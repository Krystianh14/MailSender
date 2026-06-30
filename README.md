# MailSender API

REST API do wysyłania wiadomości e-mail, zbudowane w ASP.NET Core (.NET 10) z architekturą warstwową (Clean Architecture). Aplikacje klienckie rejestrują się, otrzymują token JWT, a następnie używają go do autoryzowanego wysyłania maili oraz przeglądania historii wysyłek.

---

## Spis treści

- [Jak działa](#jak-działa)
- [Architektura](#architektura)
- [Endpointy API](#endpointy-api)
- [Konfiguracja](#konfiguracja)
- [Dostawcy maili](#dostawcy-maili)
- [Uruchomienie](#uruchomienie)
- [Bezpieczeństwo](#bezpieczeństwo)

---

## Jak działa

```
1. Rejestracja                     2. Wysyłanie maila              3. Historia wysyłek
──────────────────                 ────────────────────             ──────────────────
POST /client-app/register          POST /mail/send                  GET /mail-log
  + AppId, AppName, Pass             + Bearer token (JWT)              + Bearer token (JWT)
        │                                  │                                │
        ▼                                  ▼                                ▼
  Walidacja hasła                Odczyt client_application_id       Odczyt client_application_id
  (lista studentów)                      z tokenu                          z tokenu
        │                                  │                                │
        ▼                                  ▼                                ▼
  Sprawdzenie unikalności      Pobranie aplikacji z bazy (EF)      Pobranie logów danej aplikacji
   AppId i AppName                        │                                │
        │                                  ▼                                ▼
        ▼                       Modyfikacja treści (subject/body)   Zwrot listy / szczegółu logu
  Zapis aplikacji w bazie                  │
        │                                  ▼
        ▼                       Wysyłka przez wybrany provider
  Generowanie JWT               (Fake / Brevo / Mailtrap)
        │                                  │
        ▼                                  ▼
  Zwrot tokenu do klienta          Zapis logu (Success/Failed)
```

Token JWT zawiera trzy claimy: `client_application_id` (Guid — używany do autoryzacji), `app_id` i `app_name`.

---

## Architektura

Projekt jest podzielony na cztery warstwy:

```
MailSender/
├── MailSender.Api/                       # Warstwa prezentacji
│   └── Controllers/
│       ├── ClientAppController.cs        # POST /client-app/register
│       ├── MailController.cs             # POST /mail/send
│       └── MailLogController.cs          # GET /mail-log, GET /mail-log/{id}
│
├── MailSender.Application/               # Logika aplikacji
│   ├── Common/
│   │   └── ServiceResult.cs              # Generyczny wrapper sukces/błąd
│   ├── DTOs/
│   │   ├── ClientApps/                   # Request/Response rejestracji
│   │   ├── Mail/                         # Request/Response wysyłki
│   │   └── MailLogs/                     # DTO logów
│   ├── Interfaces/
│   │   ├── IClientApplicationRepository.cs
│   │   ├── IMailLogRepository.cs
│   │   ├── IMailSenderProvider.cs
│   │   ├── IJwtTokenService.cs
│   │   └── IRegistrationPasswordValidator.cs
│   ├── Services/
│   │   ├── ClientApplicationService.cs   # Logika rejestracji
│   │   ├── MailService.cs                # Logika wysyłki + logowania
│   │   └── MailLogService.cs             # Logika odczytu logów
│   └── Settings/
│       └── StudentSettings.cs
│
├── MailSender.Infrastructure/            # Implementacje zewnętrzne
│   ├── Auth/
│   │   ├── JwtSettings.cs
│   │   └── JwtTokenService.cs            # Generowanie JWT
│   ├── MailProviders/
│   │   ├── FakeMailSenderProvider.cs     # Mock — loguje do konsoli
│   │   ├── BrevoMailSenderProvider.cs    # Wysyłka przez Brevo API
│   │   ├── MailtrapMailSenderProvider.cs # Wysyłka przez Mailtrap API
│   │   ├── BrevoSettings.cs
│   │   ├── MailtrapSettings.cs
│   │   └── MailProviderSettings.cs       # Wybór aktywnego providera
│   ├── Persistence/
│   │   └── MailSenderDbContext.cs        # EF Core DbContext
│   ├── Registration/
│   │   ├── RegistrationSettings.cs
│   │   └── RegistrationPasswordValidator.cs
│   └── Repositories/
│       ├── EfClientApplicationRepository.cs   # Aktywne (EF Core)
│       ├── EfMailLogRepository.cs             # Aktywne (EF Core)
│       └── InMemoryClientApplicationRepository.cs  # Nieużywane, zachowane jako alternatywa
│
└── MailSender.Domain/                    # Encje domenowe
    └── Entities/
        ├── ClientApplication.cs          # Id, AppId, AppName
        ├── MailSendLog.cs                # Log wysyłki (Success/Failed)
        └── MailMessage.cs                # To, Subject, Body
```

### Kluczowe komponenty

| Komponent | Typ rejestracji | Opis |
|---|---|---|
| `ClientApplicationService` | Scoped | Waliduje hasło, sprawdza unikalność AppId/AppName, tworzy aplikację, generuje JWT |
| `MailService` | Scoped | Modyfikuje treść maila, wysyła przez wybrany provider, zapisuje log |
| `MailLogService` | Scoped | Odczytuje historię wysyłek dla danej aplikacji klienckiej |
| `JwtTokenService` | Scoped | Generuje token JWT z claimami `client_application_id`, `app_id`, `app_name` |
| `RegistrationPasswordValidator` | Scoped | Waliduje hasło rejestracyjne na podstawie listy `Students` z konfiguracji |
| `EfClientApplicationRepository` | Scoped | Repozytorium aplikacji klienckich — EF Core, baza in-memory |
| `EfMailLogRepository` | Scoped | Repozytorium logów wysyłek — EF Core, baza in-memory |
| `MailSenderDbContext` | Scoped (DbContext) | Kontekst EF Core z tabelami `ClientApplications` i `MailSendLogs` |
| Provider maila | Scoped / HttpClient | Wybierany dynamicznie: `Fake`, `Brevo` lub `Mailtrap` (patrz [Dostawcy maili](#dostawcy-maili)) |

### Specjalna logika biznesowa (`MailService`)

Przed wysyłką treść maila jest automatycznie modyfikowana:

- Jeśli `Subject` kończy się znakiem `?`, dodawany jest prefiks `[Q] ` na początku tematu.
- Jeśli `Body` zawiera nazwisko któregoś ze studentów skonfigurowanych w `Students` (np. "Haberka"), nazwisko jest otaczane znacznikiem `[student.surname]Haberka[student.surname]`.
---

## Endpointy API

### `POST /client-app/register`

Rejestruje nową aplikację kliencką i zwraca token JWT. **Bez autoryzacji.**

**Request:**
```json
{
  "appId": "moja-aplikacja",
  "appName": "MojaAplikacja",
  "pass": "dwa13"
}
```

**`200 OK`:**
```json
{
  "appId": "moja-aplikacja",
  "appName": "MojaAplikacja",
  "key": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**`403 Forbidden`** — błędne hasło lub duplikat AppId/AppName:
```json
{ "error": "Invalid index-based password. Expected one of suffixes: 13, 28, 02" }
```

---

### `POST /mail/send`

Wysyła maila. **Wymaga:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "to": "odbiorca@example.com",
  "subject": "Pytanie o status?",
  "body": "Pozdrawiam, Haberka"
}
```

**`200 OK`:**
```json
{
  "appId": "moja-aplikacja",
  "appName": "MojaAplikacja",
  "status": "Success",
  "email": {
    "to": "odbiorca@example.com",
    "subject": "[Q] Pytanie o status?",
    "body": "Pozdrawiam, [student.surname]Haberka[student.surname]"
  }
}
```

**`401 Unauthorized`** — brak/błędny token lub aplikacja nieznaleziona.
**`400 Bad Request`** — błąd wysyłki (np. provider zwrócił błąd).

---

### `GET /mail-log`

Zwraca historię wszystkich wysyłek dla aplikacji powiązanej z tokenem. **Wymaga autoryzacji.**

**`200 OK`:**
```json
[
  {
    "id": "...",
    "appId": "moja-aplikacja",
    "appName": "MojaAplikacja",
    "to": "odbiorca@example.com",
    "subject": "[Q] Pytanie o status?",
    "body": "...",
    "status": "Success",
    "errorMessage": null,
    "createdAtUtc": "2026-06-30T10:00:00Z"
  }
]
```

### `GET /mail-log/{id}`

Zwraca pojedynczy log wysyłki po jego ID (musi należeć do aplikacji z tokenu).

**`404 Not Found`** jeśli log nie istnieje lub należy do innej aplikacji.

---

## Konfiguracja

`appsettings.json`:

```json
{
  "Students": [
    { "Surname": "Haberka", "IndexSuffix": "13" },
    { "Surname": "Haczyk", "IndexSuffix": "28" },
    { "Surname": "Kapusta", "IndexSuffix": "02" }
  ],
  "Jwt": {
    "SecretKey": "ZMIEŃ_MNIE_MIN_32_ZNAKI",
    "Issuer": "MailSender",
    "Audience": "MailSenderClients",
    "ExpirationDays": 90
  },
  "MailProvider": {
    "SelectedProvider": "Fake"
  },
  "Brevo": {
    "ApiKey": "...",
    "SenderEmail": "...",
    "SenderName": "..."
  },
  "Mailtrap": {
    "ApiKey": "...",
    "SenderEmail": "...",
    "SenderName": "...",
    "UseSandbox": true,
    "InboxId": 0
  }
}
```

| Pole | Opis |
|---|---|
| `Students` | Lista dozwolonych haseł rejestracyjnych. Każdy student ma hasło w formacie `dwa{IndexSuffix}` (np. `dwa13`), generowane przez `StudentSettings.Password`. Nazwiska są też używane do oznaczania treści maila. |
| `Jwt:SecretKey` | Klucz podpisujący JWT (min. 32 znaki) |
| `Jwt:ExpirationDays` | Czas życia tokenu (domyślnie 90 dni) |
| `MailProvider:SelectedProvider` | `Fake`, `Brevo` lub `Mailtrap` — wybiera aktywnego dostawcę maili |
| `Brevo:*` / `Mailtrap:*` | Dane uwierzytelniające dla odpowiedniego providera |

---

## Dostawcy maili

Wybór providera odbywa się dynamicznie w `Program.cs` na podstawie `MailProvider:SelectedProvider`:

| Provider | Klasa | Działanie |
|---|---|---|
| `Fake` | `FakeMailSenderProvider` | Nie wysyła nic — loguje treść maila do konsoli. Domyślny, bezpieczny do testów lokalnych. |
| `Brevo` | `BrevoMailSenderProvider` | Wysyła przez `https://api.brevo.com/v3/smtp/email` |
| `Mailtrap` | `MailtrapMailSenderProvider` | Wysyła przez Mailtrap. Wsparcie dla sandboxa (`UseSandbox: true` + `InboxId`) lub wysyłki produkcyjnej |

Podanie nieznanej wartości `SelectedProvider` powoduje wyjątek przy starcie aplikacji.

---

## Uruchomienie

### Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Opcjonalnie) konto Brevo lub Mailtrap do prawdziwej wysyłki maili

### Kroki

```bash
git clone <url-repozytorium>
cd MailSender

dotnet restore
dotnet run --project MailSender.Api
```

Aplikacja domyślnie wystartuje na `http://localhost:5235`. Swagger UI: `http://localhost:5235/swagger`.

### Baza danych

Projekt korzysta z **EF Core In-Memory Database** (`builder.Services.AddDbContext<MailSenderDbContext>(options => options.UseInMemoryDatabase("MailSenderDatabase"))`). Oznacza to, że:

- nie trzeba instalować ani konfigurować żadnej bazy danych,
- wszystkie dane (zarejestrowane aplikacje, logi wysyłek) **giną po każdym restarcie** aplikacji,

### User Secrets (zalecane)

```bash
dotnet user-secrets set "Jwt:SecretKey" "twój-sekretny-klucz" --project MailSender.Api
dotnet user-secrets set "Brevo:ApiKey" "twój-klucz-brevo" --project MailSender.Api
```

---

## Bezpieczeństwo

> Przed wdrożeniem produkcyjnym:

- **Zmień `Jwt:SecretKey`** — domyślna wartość jest jawna w repozytorium.
- **Hasła rejestracyjne** (`Students`) są obecnie jawnym tekstem w konfiguracji — rozważ ich ukrycie lub wymianę na inny mechanizm uwierzytelniania klientów.
- **Klucze API (Brevo/Mailtrap)** trzymaj w zmiennych środowiskowych lub menedżerze sekretów, nigdy w commitowanym `appsettings.json`.
- **Baza in-memory** nie nadaje się do produkcji — dane nie są trwałe.
- Token JWT niesie `client_application_id`, na podstawie którego serwer identyfikuje wywołującą aplikację przy każdym żądaniu do `/mail/send` i `/mail-log`.

---

## Testowanie przez Swagger

1. `POST /client-app/register` z poprawnym hasłem (np. `dwa13`) → skopiuj `key` z odpowiedzi.
2. Kliknij **Authorize** w Swagger UI, wpisz `Bearer <key>`.
3. Wywołaj `POST /mail/send` lub `GET /mail-log`.

---

## Klient demo (webclient)

Projekt zawiera dodatkowo prosty **frontend demonstracyjny** (`webclient/`) — formularz HTML/JS zbudowany w Vite, który pozwala wysłać maila przez `/mail/send` bez używania Swaggera.

### Struktura

```
webclient/
├── index.html              # Formularz (API URL, token JWT, odbiorca, temat, treść)
├── app.js                  # Logika formularza, wywołuje wygenerowanego klienta API
├── style.css               # Stylowanie
├── openapi.json             # Specyfikacja OpenAPI backendu (źródło generowania klienta)
├── package.json
└── generated-ts/            # Klient TS wygenerowany z openapi.json (openapi-typescript-codegen)
    └── generated-js/        # Skompilowana wersja JS (importowana przez app.js)
```

Klient API (`generated-ts`/`generated-js`) jest generowany automatycznie z `openapi.json` przy pomocy `openapi-typescript-codegen` — pliki te nie powinny być edytowane ręcznie (każdy zawiera nagłówek `/* generated using openapi-typescript-codegen -- do not edit */`).

### Uruchomienie klienta demo

```bash
cd webclient
npm install

# Wygeneruj klienta API na podstawie openapi.json (jeśli folder generated-ts/generated-js nie istnieje)
npm run generate:ts
npm run build:generated-js

# Uruchom serwer dev
npm run dev
```

Klient wystartuje na `http://127.0.0.1:5500`. **Backend (`MailSender.Api`) musi działać równolegle** na `http://localhost:5235` — w drugim terminalu:

```bash
dotnet run --project MailSender.Api
```

### Jak przetestować

1. Backend działa w tle.
2. Wejdź na Swagger (`http://localhost:5235/swagger`), wywołaj `POST /client-app/register`, skopiuj `key` z odpowiedzi.
3. Otwórz `http://127.0.0.1:5500` — wklej token do pola **JWT Token** (bez słowa "Bearer", skrypt dodaje je automatycznie).
4. Kliknij **„Wstaw przykład"**, aby wypełnić formularz danymi testującymi obie reguły biznesowe (temat z `?` → prefiks `[Q]`, nazwisko "Haberka" w treści → marker `[student.surname]`).
5. Kliknij **„Wyślij wiadomość"** — odpowiedź z API pojawi się w sekcji **„Odpowiedź API"** na dole strony.

### Skrypty npm (`webclient/package.json`)

| Skrypt | Działanie |
|---|---|
| `npm run generate:ts` | Generuje klienta TypeScript z `openapi.json` do folderu `generated-ts` |
| `npm run build:generated-js` | Kompiluje `generated-ts` do `generated-js` (importowane przez `app.js`) |
| `npm run dev` | Uruchamia serwer deweloperski Vite na `127.0.0.1:5500` |
| `npm run build` | Buduje wersję produkcyjną |
