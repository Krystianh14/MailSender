# MailSender API

REST API do wysyłania wiadomości e-mail, zbudowane w ASP.NET Core. Aplikacje klienckie rejestrują się, otrzymują token JWT, a następnie używają go do autoryzowanego wysyłania maili przez dostawcę Brevo (dawniej Sendinblue).

---

## Spis treści

- [Jak działa](#jak-działa)
- [Architektura](#architektura)
- [Endpointy API](#endpointy-api)
- [Konfiguracja](#konfiguracja)
- [Uruchomienie](#uruchomienie)
- [Bezpieczeństwo](#bezpieczeństwo)

---

## Jak działa

Przepływ działania aplikacji składa się z dwóch kroków:

```
1. Rejestracja                          2. Wysyłanie maila
──────────────────                      ────────────────────────────────
POST /client-app/register               POST /mail/send
  + hasło rejestracyjne                   + Bearer token (JWT)
        │                                       │
        ▼                                       ▼
  Walidacja hasła                    Weryfikacja tokenu JWT
        │                             Odczyt app_id z claimu
        ▼                                       │
  Zapis aplikacji                               ▼
  w repozytorium                   Pobranie aplikacji z repozytorium
        │                                       │
        ▼                                       ▼
  Generowanie JWT                     Wysłanie maila przez Brevo
        │                             (lub FakeMailSenderProvider)
        ▼
  Zwrot tokenu do klienta
```

---

## Architektura

Projekt zbudowany jest warstwowo, zgodnie z Clean Architecture:

```
MailSender/
├── Api/                        # Warstwa prezentacji
│   └── Controllers/
│       ├── ClientAppController.cs   # Rejestracja klientów
│       └── MailController.cs        # Wysyłanie maili
│
├── Application/                # Logika aplikacji
│   ├── Interfaces/
│   │   ├── IClientApplicationRepository.cs
│   │   ├── IJwtTokenService.cs
│   │   ├── IMailSenderProvider.cs
│   │   └── IRegistrationPasswordValidator.cs
│   ├── Services/
│   │   ├── ClientApplicationService.cs
│   │   └── MailService.cs
│   ├── DTOs/
│   └── Settings/
│
├── Infrastructure/             # Implementacje zewnętrzne
│   ├── Auth/                   # JwtTokenService
│   ├── MailProviders/
│   │   ├── BrevoMailSenderProvider.cs   # Produkcyjny provider (HTTP)
│   │   └── FakeMailSenderProvider.cs    # Mock do developmentu
│   ├── Repositories/
│   │   └── InMemoryClientApplicationRepository.cs
│   └── Registration/
│
└── Domain/                     # Encje domenowe
    └── Entities/
        ├── ClientApplication.cs
        └── MailMessage.cs
```

### Kluczowe komponenty

| Komponent | Typ | Opis |
|---|---|---|
| `ClientApplicationService` | Scoped | Obsługuje rejestrację — waliduje hasło, tworzy aplikację, generuje JWT |
| `MailService` | Scoped | Buduje wiadomość i deleguje wysyłkę do providera |
| `JwtTokenService` | Scoped | Generuje tokeny JWT z claimem `app_id` |
| `InMemoryClientApplicationRepository` | Singleton | Przechowuje zarejestrowane aplikacje w pamięci (dane giną po restarcie) |
| `BrevoMailSenderProvider` | Scoped (HttpClient) | Wysyła maile przez Brevo REST API |
| `FakeMailSenderProvider` | Scoped | Symuluje wysyłkę bez zewnętrznego API — do testów lokalnych |
| `RegistrationPasswordValidator` | Scoped | Sprawdza hasło rejestracyjne z konfiguracji |

---

## Endpointy API

### `POST /client-app/register`

Rejestruje nową aplikację kliencką i zwraca token JWT.

**Nie wymaga autoryzacji.**

**Request body:**
```json
{
  "appName": "MojaAplikacja",
  "password": "dwa13"
}
```

**Odpowiedź sukcesu `200 OK`:**
```json
{
  "appId": "550e8400-e29b-41d4-a716-446655440000",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Odpowiedź błędu `403 Forbidden`** (nieprawidłowe hasło):
```json
{
  "error": "Invalid registration password."
}
```

---

### `POST /mail/send`

Wysyła wiadomość e-mail. Wymaga ważnego tokenu JWT w nagłówku.

**Wymaga autoryzacji:** `Authorization: Bearer <token>`

**Request body:**
```json
{
  "to": "odbiorca@example.com",
  "subject": "Temat wiadomości",
  "body": "Treść wiadomości e-mail"
}
```

**Odpowiedź sukcesu `200 OK`:**
```json
{
  "message": "Email sent successfully."
}
```

**Odpowiedź błędu `401 Unauthorized`** (brak/błędny token):
```json
{
  "error": "Invalid token. Missing app_id claim."
}
```

**Odpowiedź błędu `400 Bad Request`:**
```json
{
  "error": "Opis błędu"
}
```

---

## Konfiguracja

Plik `appsettings.json` — **przed wdrożeniem produkcyjnym zmień wszystkie domyślne wartości**.

```json
{
  "Registration": {
    "Password": "ZMIEŃ_MNIE",
    "IndexSuffix": "13"
  },
  "Jwt": {
    "SecretKey": "ZMIEŃ_MNIE_MINIMUM_32_ZNAKI",
    "Issuer": "MailSender",
    "Audience": "MailSenderClients",
    "ExpirationDays": 90
  },
  "Student": {
    "Surname": "Haberka"
  },
  "Brevo": {
    "ApiKey": "TWÓJ_KLUCZ_BREVO",
    "SenderEmail": "twój-email@example.com",
    "SenderName": "MailSender App"
  }
}
```

| Pole | Opis |
|---|---|
| `Registration:Password` | Hasło wymagane do rejestracji nowego klienta |
| `Registration:IndexSuffix` | Dodatkowy sufiks walidacji hasła |
| `Jwt:SecretKey` | Klucz do podpisywania tokenów JWT (min. 32 znaki) |
| `Jwt:ExpirationDays` | Czas ważności tokenu (domyślnie 90 dni) |
| `Brevo:ApiKey` | Klucz API z panelu Brevo |
| `Brevo:SenderEmail` | Adres nadawcy maili (musi być zweryfikowany w Brevo) |

### Zmiana providera maili

W `Program.cs` domyślnie zarejestrowany jest `FakeMailSenderProvider` (mock). Aby przełączyć na prawdziwy Brevo, zmień:

```csharp
// Przed (mock):
builder.Services.AddScoped<IMailSenderProvider, FakeMailSenderProvider>();

// Po (produkcja):
// Wystarczy mieć wpis poniżej — HttpClient jest już skonfigurowany:
builder.Services.AddHttpClient<IMailSenderProvider, BrevoMailSenderProvider>();
```

---

## Uruchomienie

### Wymagania

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Konto w [Brevo](https://www.brevo.com/) (opcjonalnie — do produkcyjnego wysyłania maili)

### Kroki

```bash
# 1. Sklonuj repozytorium
git clone <url-repozytorium>
cd MailSender

# 2. Uzupełnij konfigurację
# Edytuj appsettings.json lub użyj user secrets

# 3. Uruchom aplikację
dotnet run --project MailSender.Api

# 4. Otwórz Swagger UI
# https://localhost:5001/swagger
```

### User Secrets (zalecane zamiast edycji appsettings.json)

```bash
dotnet user-secrets set "Jwt:SecretKey" "twój-sekretny-klucz"
dotnet user-secrets set "Brevo:ApiKey" "twój-klucz-brevo"
dotnet user-secrets set "Registration:Password" "twoje-hasło"
```

---

## Bezpieczeństwo

> ⚠️ **Uwagi dla środowiska produkcyjnego:**

- **Zmień `Jwt:SecretKey`** — domyślna wartość jest publiczna i niebezpieczna.
- **Zmień `Registration:Password`** — domyślne hasło `dwa13` jest widoczne w repozytorium.
- **Klucz Brevo** trzymaj w zmiennych środowiskowych lub Azure Key Vault, nigdy w `appsettings.json` commitowanym do gita.
- **Repozytorium in-memory** — dane aplikacji klienckich giną po każdym restarcie serwera. Do produkcji należy zastąpić je bazą danych (np. Entity Framework + SQL Server/PostgreSQL).
- Token JWT zawiera claim `app_id`, który identyfikuje aplikację kliencką przy każdym żądaniu wysyłki maila.

---

## Swagger / Dokumentacja API

Swagger UI dostępny jest pod adresem `/swagger` (włączony w każdym środowisku).

Aby przetestować chroniony endpoint `/mail/send`:
1. Wywołaj `POST /client-app/register` i skopiuj `token` z odpowiedzi.
2. Kliknij **Authorize** w Swagger UI.
3. Wpisz `Bearer <skopiowany-token>` i zatwierdź.
4. Wywołaj `POST /mail/send`.
