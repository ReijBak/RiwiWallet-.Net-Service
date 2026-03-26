# Riwi Wallet - MS Core API

**[English](#english) | [Espanol](#espanol)**

---

## English

### What is Riwi Wallet?

Riwi Wallet is a financial management microservice core built with .NET 8. It serves as the central backend for a personal finance ecosystem, providing spending validation, transaction management, budget rules, and user authentication. The system integrates with Telegram bots, Vue.js dashboards, and n8n automation workflows.

### Purpose

This backend was created to:

- **Validate spending requests** in real-time against user-defined budget rules
- **Track all financial transactions** (income and expenses) from multiple sources
- **Manage financial rules** such as spending limits, savings goals, and category budgets
- **Provide authentication** for users via JWT tokens and API Keys for internal services
- **Enable Telegram integration** for quick expense logging via chat

### Architecture

The project follows **Domain-Driven Design (DDD)** with Clean Architecture principles, organized into 4 main layers:

```
src/
├── API/                    # Presentation Layer (Controllers, Authentication)
├── Core.Application/       # Application Layer (Services, DTOs, Interfaces)
├── Core.Domain/            # Domain Layer (Entities, Enums, Repository Interfaces)
└── Infrastructure/         # Infrastructure Layer (Database, Repositories)
```

#### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **API** | REST endpoints, authentication handlers, CORS configuration, Swagger documentation |
| **Core.Application** | Business logic services, data transfer objects, service interfaces |
| **Core.Domain** | Domain entities with encapsulated behavior, enumerations, repository contracts |
| **Infrastructure** | PostgreSQL database access via Entity Framework Core, repository implementations |

### Domain Model

#### Entities

- **User**: Represents a system user with balance, Telegram integration, and OAuth2 support
- **Transaction**: Records income or expense entries with category, source, and description
- **FinancialRule**: Defines budget limits per category and time period
- **RefreshToken**: Manages JWT refresh token lifecycle
- **TelegramLinkCode**: Handles temporary codes for linking Telegram accounts

#### Enumerations

| Enum | Values |
|------|--------|
| TransactionType | Income, Expense |
| TransactionSource | Manual, Telegram, WhatsApp, Automatic |
| RuleType | SpendingLimit, SavingsGoal, CategoryBudget |
| RulePeriod | Daily, Weekly, Biweekly, Monthly, Yearly |

### Technology Stack

| Technology | Purpose |
|------------|---------|
| .NET 8 | Runtime and framework |
| ASP.NET Core | Web API framework |
| Entity Framework Core | ORM for database access |
| PostgreSQL | Relational database |
| Serilog | Structured logging |
| JWT Bearer | Token-based authentication |
| Swagger/OpenAPI | API documentation |
| Docker | Containerization |

### API Endpoints

#### Authentication (`/api/Auth`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/register` | Register a new user |
| POST | `/login` | Authenticate and get tokens |
| POST | `/refresh-token` | Refresh access token |
| POST | `/revoke-token` | Revoke a refresh token |

#### Users (`/api/User`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/` | Create a new user |
| GET | `/{id}` | Get user by ID |
| GET | `/email/{email}` | Get user by email |
| GET | `/phone/{phone}` | Get user by phone |
| GET | `/telegram/{telegramId}` | Get user by Telegram ID |
| GET | `/{userId}/balance` | Get user balance summary |
| GET | `/{userId}/telegram-id` | Get user Telegram ID |
| POST | `/{userId}/telegram/generate-link` | Generate Telegram link code |
| POST | `/telegram/link` | Link Telegram account |
| GET | `/{userId}/telegram/status` | Get Telegram link status |
| DELETE | `/{userId}/telegram` | Unlink Telegram account |

#### Transactions (`/api/Transaction`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/` | Create a transaction |
| GET | `/user/{userId}` | Get user transactions (paginated) |
| GET | `/{id}` | Get transaction by ID |
| DELETE | `/{id}` | Delete a transaction |
| GET | `/user/{userId}/range` | Get transactions by date range |
| GET | `/user/{userId}/date/{date}` | Get transactions by specific date |
| GET | `/user/{userId}/search` | Search transactions |
| GET | `/user/{userId}/category-summary` | Get category spending summary |

#### Financial Rules (`/api/FinancialRule`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/` | Create a financial rule |
| GET | `/user/{userId}` | Get user rules |
| GET | `/{id}` | Get rule by ID |
| PUT | `/{id}/deactivate` | Deactivate a rule |
| DELETE | `/{id}` | Delete a rule |
| GET | `/{id}/progress` | Get rule progress (spent vs limit) |
| GET | `/user/{userId}/progress` | Get all rules progress |

#### Spending Validation (`/api/SpendingValidation`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/validate` | Validate a spending against user rules |

### Installation

#### Prerequisites

- .NET 8 SDK
- PostgreSQL 14+
- Docker (optional)

#### 1. Clone the repository

```bash
git clone <repository-url>
cd Micro-Back-Brahiam
```

#### 2. Configure environment variables

Copy the example file and update with your values:

```bash
cp .env.example .env
```

Required variables:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Database=financial_assistant;Username=your_user;Password=your_password
Jwt__Key=your-super-secret-key-minimum-32-characters-long
Jwt__Issuer=FinancialAssistantAPI
Jwt__Audience=FinancialAssistantClients
Telegram__BotUsername=your_telegram_bot_username
```

#### 3. Run database migrations

```bash
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj
```

> Note: `ConnectionStrings__DefaultConnection` must be set in the environment variables before running migrations.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=financial_assistant;Username=your_user;Password=your_password;Port=5432"
```

Git Bash:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=financial_assistant;Username=your_user;Password=your_password;Port=5432"
```

#### 4. Run the application

```bash
dotnet run --project src/API/API.csproj
```

The API will be available at `https://localhost:5001` (or the configured port).

### Docker Deployment

#### Build and run with Docker Compose

```bash
docker-compose -f deploy-compose.yml up -d
```

The container exposes port 5001 and connects to a PostgreSQL database.

### Testing with Swagger

Access the Swagger UI at: `https://localhost:<port>/swagger`

### Integration with Other Services

This microservice is designed to work within a larger ecosystem:

| Service | Integration Point |
|---------|-------------------|
| **Vue.js Dashboard** | Consumes all endpoints via REST API with JWT authentication |
| **Spring Boot AI Worker** | Calls `/api/SpendingValidation/validate` to check budget compliance |
| **n8n Automation** | Posts to `/api/Transaction` for automatic expense logging |
| **Telegram Bot** | Uses `/api/User/telegram/*` endpoints for account linking and transaction creation |

### CORS Configuration

The API allows requests from:

- `http://localhost:5173` (Vue.js development)
- `http://localhost:8080` (Spring Boot AI Worker)
- `http://localhost:8081` (Spring Boot Gateway)
- `https://avaricia.crudzaso.com` (Production frontend)
- `https://sb.avaricia.crudzaso.com` (Production Spring Boot)

### Project Structure

```
Micro-Back-Brahiam/
├── .env.example                    # Environment variables template
├── .github/                        # GitHub workflows
├── deploy-compose.yml             # Docker Compose for deployment
├── FinancialAssistant.sln         # Solution file
├── README.md                       # This file
└── src/
    ├── API/
    │   ├── Controllers/           # REST API controllers
    │   ├── Authentication/        # API Key authentication handler
    │   ├── Program.cs            # Application entry point and DI configuration
    │   ├── appsettings.json      # Configuration file
    │   └── Dockerfile            # Container definition
    ├── Core.Application/
    │   ├── DTOs/                 # Request/Response data transfer objects
    │   ├── Interfaces/           # Service interfaces
    │   └── Services/             # Business logic implementation
    ├── Core.Domain/
    │   ├── Common/               # Base entity class
    │   ├── Entities/             # Domain entities
    │   ├── Enums/                # Domain enumerations
    │   └── Interfaces/           # Repository interfaces
    └── Infrastructure/
        ├── Migrations/           # EF Core migrations
        ├── Persistence/          # Database context
        └── Repositories/         # Repository implementations
```

---

## Espanol

### Que es Riwi Wallet?

Riwi Wallet es un microservicio central de gestion financiera construido con .NET 8. Funciona como el backend principal para un ecosistema de finanzas personales, proporcionando validacion de gastos, gestion de transacciones, reglas de presupuesto y autenticacion de usuarios. El sistema se integra con bots de Telegram, dashboards de Vue.js y flujos de automatizacion n8n.

### Proposito

Este backend fue creado para:

- **Validar solicitudes de gasto** en tiempo real contra reglas de presupuesto definidas por el usuario
- **Rastrear todas las transacciones financieras** (ingresos y gastos) de multiples fuentes
- **Gestionar reglas financieras** como limites de gasto, metas de ahorro y presupuestos por categoria
- **Proporcionar autenticacion** para usuarios via tokens JWT y API Keys para servicios internos
- **Habilitar integracion con Telegram** para registro rapido de gastos via chat

### Arquitectura

El proyecto sigue **Domain-Driven Design (DDD)** con principios de Arquitectura Limpia, organizado en 4 capas principales:

```
src/
├── API/                    # Capa de Presentacion (Controllers, Autenticacion)
├── Core.Application/       # Capa de Aplicacion (Servicios, DTOs, Interfaces)
├── Core.Domain/            # Capa de Dominio (Entidades, Enums, Interfaces de Repositorios)
└── Infrastructure/         # Capa de Infraestructura (Base de datos, Repositorios)
```

#### Responsabilidades de las Capas

| Capa | Responsabilidad |
|------|-----------------|
| **API** | Endpoints REST, manejadores de autenticacion, configuracion CORS, documentacion Swagger |
| **Core.Application** | Servicios de logica de negocio, objetos de transferencia de datos, interfaces de servicios |
| **Core.Domain** | Entidades de dominio con comportamiento encapsulado, enumeraciones, contratos de repositorios |
| **Infrastructure** | Acceso a base de datos PostgreSQL via Entity Framework Core, implementaciones de repositorios |

### Modelo de Dominio

#### Entidades

- **User**: Representa un usuario del sistema con saldo, integracion Telegram y soporte OAuth2
- **Transaction**: Registra entradas de ingresos o gastos con categoria, fuente y descripcion
- **FinancialRule**: Define limites de presupuesto por categoria y periodo de tiempo
- **RefreshToken**: Gestiona el ciclo de vida de tokens de actualizacion JWT
- **TelegramLinkCode**: Maneja codigos temporales para vincular cuentas de Telegram

#### Enumeraciones

| Enum | Valores |
|------|---------|
| TransactionType | Income (Ingreso), Expense (Gasto) |
| TransactionSource | Manual, Telegram, WhatsApp, Automatic (Automatico) |
| RuleType | SpendingLimit (Limite de gasto), SavingsGoal (Meta de ahorro), CategoryBudget (Presupuesto por categoria) |
| RulePeriod | Daily (Diario), Weekly (Semanal), Biweekly (Quincenal), Monthly (Mensual), Yearly (Anual) |

### Stack Tecnologico

| Tecnologia | Proposito |
|------------|-----------|
| .NET 8 | Runtime y framework |
| ASP.NET Core | Framework de API Web |
| Entity Framework Core | ORM para acceso a base de datos |
| PostgreSQL | Base de datos relacional |
| Serilog | Logging estructurado |
| JWT Bearer | Autenticacion basada en tokens |
| Swagger/OpenAPI | Documentacion de API |
| Docker | Contenedorizacion |

### Endpoints de la API

#### Autenticacion (`/api/Auth`)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/register` | Registrar un nuevo usuario |
| POST | `/login` | Autenticar y obtener tokens |
| POST | `/refresh-token` | Refrescar token de acceso |
| POST | `/revoke-token` | Revocar un token de actualizacion |

#### Usuarios (`/api/User`)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/` | Crear un nuevo usuario |
| GET | `/{id}` | Obtener usuario por ID |
| GET | `/email/{email}` | Obtener usuario por email |
| GET | `/phone/{phone}` | Obtener usuario por telefono |
| GET | `/telegram/{telegramId}` | Obtener usuario por ID de Telegram |
| GET | `/{userId}/balance` | Obtener resumen de saldo del usuario |
| GET | `/{userId}/telegram-id` | Obtener ID de Telegram del usuario |
| POST | `/{userId}/telegram/generate-link` | Generar codigo de vinculacion Telegram |
| POST | `/telegram/link` | Vincular cuenta de Telegram |
| GET | `/{userId}/telegram/status` | Obtener estado de vinculacion Telegram |
| DELETE | `/{userId}/telegram` | Desvincular cuenta de Telegram |

#### Transacciones (`/api/Transaction`)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/` | Crear una transaccion |
| GET | `/user/{userId}` | Obtener transacciones del usuario (paginadas) |
| GET | `/{id}` | Obtener transaccion por ID |
| DELETE | `/{id}` | Eliminar una transaccion |
| GET | `/user/{userId}/range` | Obtener transacciones por rango de fechas |
| GET | `/user/{userId}/date/{date}` | Obtener transacciones por fecha especifica |
| GET | `/user/{userId}/search` | Buscar transacciones |
| GET | `/user/{userId}/category-summary` | Obtener resumen de gastos por categoria |

#### Reglas Financieras (`/api/FinancialRule`)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/` | Crear una regla financiera |
| GET | `/user/{userId}` | Obtener reglas del usuario |
| GET | `/{id}` | Obtener regla por ID |
| PUT | `/{id}/deactivate` | Desactivar una regla |
| DELETE | `/{id}` | Eliminar una regla |
| GET | `/{id}/progress` | Obtener progreso de la regla (gastado vs limite) |
| GET | `/user/{userId}/progress` | Obtener progreso de todas las reglas |

#### Validacion de Gastos (`/api/SpendingValidation`)

| Metodo | Endpoint | Descripcion |
|--------|----------|-------------|
| POST | `/validate` | Validar un gasto contra las reglas del usuario |

### Instalacion

#### Requisitos Previos

- .NET 8 SDK
- PostgreSQL 14+
- Docker (opcional)

#### 1. Clonar el repositorio

```bash
git clone <repository-url>
cd Micro-Back-Brahiam
```

#### 2. Configurar variables de entorno

Copiar el archivo de ejemplo y actualizar con tus valores:

```bash
cp .env.example .env
```

Variables requeridas:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Database=financial_assistant;Username=tu_usuario;Password=tu_password
Jwt__Key=tu-clave-secreta-super-segura-minimo-32-caracteres
Jwt__Issuer=FinancialAssistantAPI
Jwt__Audience=FinancialAssistantClients
Telegram__BotUsername=nombre_de_tu_bot_telegram
```

#### 3. Ejecutar migraciones de base de datos

```bash
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj
```

> Nota: `ConnectionStrings__DefaultConnection` debe estar definida en variables de entorno antes de correr migraciones.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=financial_assistant;Username=tu_usuario;Password=tu_password;Port=5432"
```

Git Bash:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=financial_assistant;Username=tu_usuario;Password=tu_password;Port=5432"
```

#### 4. Ejecutar la aplicacion

```bash
dotnet run --project src/API/API.csproj
```

La API estara disponible en `https://localhost:5001` (o el puerto configurado).

### Despliegue con Docker

#### Construir y ejecutar con Docker Compose

```bash
docker-compose -f deploy-compose.yml up -d
```

El contenedor expone el puerto 5001 y se conecta a una base de datos PostgreSQL.

### Pruebas con Swagger

Accede a la interfaz de Swagger en: `https://localhost:<puerto>/swagger`

### Integracion con Otros Servicios

Este microservicio esta disenado para funcionar dentro de un ecosistema mayor:

| Servicio | Punto de Integracion |
|----------|---------------------|
| **Dashboard Vue.js** | Consume todos los endpoints via API REST con autenticacion JWT |
| **AI Worker Spring Boot** | Llama a `/api/SpendingValidation/validate` para verificar cumplimiento de presupuesto |
| **Automatizacion n8n** | Hace POST a `/api/Transaction` para registro automatico de gastos |
| **Bot de Telegram** | Usa endpoints `/api/User/telegram/*` para vinculacion de cuentas y creacion de transacciones |

### Configuracion CORS

La API permite solicitudes desde:

- `http://localhost:5173` (Desarrollo Vue.js)
- `http://localhost:8080` (AI Worker Spring Boot)
- `http://localhost:8081` (Gateway Spring Boot)
- `https://avaricia.crudzaso.com` (Frontend de produccion)
- `https://sb.avaricia.crudzaso.com` (Spring Boot de produccion)

### Estructura del Proyecto

```
Micro-Back-Brahiam/
├── .env.example                    # Plantilla de variables de entorno
├── .github/                        # Workflows de GitHub
├── deploy-compose.yml             # Docker Compose para despliegue
├── FinancialAssistant.sln         # Archivo de solucion
├── README.md                       # Este archivo
└── src/
    ├── API/
    │   ├── Controllers/           # Controladores de API REST
    │   ├── Authentication/        # Manejador de autenticacion por API Key
    │   ├── Program.cs            # Punto de entrada y configuracion de DI
    │   ├── appsettings.json      # Archivo de configuracion
    │   └── Dockerfile            # Definicion del contenedor
    ├── Core.Application/
    │   ├── DTOs/                 # Objetos de transferencia de datos
    │   ├── Interfaces/           # Interfaces de servicios
    │   └── Services/             # Implementacion de logica de negocio
    ├── Core.Domain/
    │   ├── Common/               # Clase base de entidades
    │   ├── Entities/             # Entidades de dominio
    │   ├── Enums/                # Enumeraciones de dominio
    │   └── Interfaces/           # Interfaces de repositorios
    └── Infrastructure/
        ├── Migrations/           # Migraciones de EF Core
        ├── Persistence/          # Contexto de base de datos
        └── Repositories/         # Implementaciones de repositorios
```

---

**License**: This project is part of the Riwi educational program.

