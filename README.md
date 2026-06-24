# Lloka — Plataforma de Rentas Cortas

Lloka es un backend para una plataforma de rentas cortas estilo Airbnb. Permite a propietarios publicar inmuebles, a huéspedes buscar y reservar propiedades, y gestiona todo el ciclo de vida de la reserva (registro con KYC, anti double-booking, wishlist anónima con merge al loguearse). Desarrollado como prueba de desempeño técnico con énfasis en decisiones arquitectónicas defensibles.

---

## Requisitos previos

| Herramienta       | Versión mínima | Para qué                                     |
|-------------------|---------------|----------------------------------------------|
| Docker Desktop    | 4.x           | Levantar la API + PostgreSQL en contenedores |
| .NET 10 SDK       | 10.0.x        | Solo si quieres correr sin Docker            |
| psql (opcional)   | cualquiera    | Inspeccionar la base de datos directamente   |

---

## Opción A: Docker Compose (recomendado)

```bash
# 1. Clonar el repositorio
git clone <url-del-repo> && cd Lloka

# 2. (Opcional) Copiar las variables de entorno
cp .env.example .env   # editar si quieres cambiar contraseñas o puertos

# 3. Levantar todo con un solo comando
docker-compose up --build
```

Al arrancar, el contenedor de la API aplica automáticamente las migraciones de EF Core (`APPLY_MIGRATIONS_ON_START=true`). No necesitas correr `dotnet ef` manualmente.

### Puertos por defecto

| Servicio | Puerto host | URL                              |
|----------|-------------|----------------------------------|
| API      | **8082**    | `http://localhost:8082/scalar`   |
| DB       | **5435**    | `localhost:5435` (user/pass: `lloka`) |

> **Nota:** Los puertos 8082 y 5435 se eligieron para evitar conflictos con otros
> contenedores Docker que puedan estar corriendo (8080–8081 y 5432–5434 son comunes).
> Si alguno ya está ocupado en tu máquina, cambia el mapeo en `docker-compose.yml`
> (`"8082:8080"` → el puerto externo que prefieras).

### Flujo de prueba rápida en Scalar

1. Abre `http://localhost:8082/scalar`
2. `POST /api/auth/register` — crea un usuario (añade `"isOwner": true` para publicar inmuebles)
3. `POST /api/auth/login` — obtén el JWT
4. En Scalar: clic en el candado → pega el token → endpoints protegidos autorizados
5. `POST /api/properties` — publica un inmueble
6. `GET /api/properties` — busca (filtra por `city`, `checkIn`, `checkOut`)
7. `POST /api/bookings` — crea una reserva (requiere KYC aprobado; usa `POST /api/kyc` primero)

---

## Opción B: Sin Docker Compose (alternativa validada)

Útil si Docker Desktop está saturado o los contenedores del Compose no levantan correctamente.

**Prerrequisito:** Tener una instancia de PostgreSQL 16 corriendo (puede ser un contenedor suelto o una instalación local).

### 1. Levantar PostgreSQL suelto

```bash
docker run -d \
  --name lloka-postgres \
  -e POSTGRES_DB=lloka_dev \
  -e POSTGRES_USER=lloka \
  -e POSTGRES_PASSWORD=lloka \
  -p 5434:5432 \
  -v ./docker/postgres/init-btree-gist.sql:/docker-entrypoint-initdb.d/init-btree-gist.sql:ro \
  postgres:16-alpine
```

> Cambia el puerto externo (`5434`) si ya está ocupado.

### 2. Aplicar migraciones

```bash
# Desde la raíz del repositorio:
dotnet ef database update \
  --project src/Infrastructure/Lloka.Infrastructure \
  --startup-project src/Presentation/Lloka.Api \
  -- --connectionstrings:defaultconnection "Host=localhost;Port=5434;Database=lloka_dev;Username=lloka;Password=lloka"
```

Alternativa: configurar la connection string en `appsettings.Development.json` y dejar que la app aplique las migraciones al iniciar (`APPLY_MIGRATIONS_ON_START=true` ya está activo en `Development`).

### 3. Correr la API

```bash
cd src/Presentation/Lloka.Api

# Apuntar a tu Postgres local:
dotnet run \
  --ConnectionStrings:DefaultConnection "Host=localhost;Port=5434;Database=lloka_dev;Username=lloka;Password=lloka"
```

La API queda disponible en `http://localhost:5000` (HTTP) o `https://localhost:5001` (HTTPS).
Scalar estará en `http://localhost:5000/scalar`.

---

## Arquitectura

### Clean Architecture (Onion) + CQRS

```
Lloka.Domain        → Entidades, Value Objects, excepciones de dominio. CERO dependencias externas.
Lloka.Application   → Casos de uso via MediatR (Commands + Queries), interfaces de repositorios e
                       infraestructura. Solo depende de Domain.
Lloka.Infrastructure→ EF Core, repositorios, JwtTokenService, BCryptPasswordHasher.
                       Implementa los contratos definidos en Application.
Lloka.Api           → Controllers delgados, middleware de excepciones, Program.cs (composition root).
                       El único proyecto que conoce todo.
```

**Por qué esta elección:** permite testear la lógica de negocio (Application) en total aislamiento con mocks (sin base de datos), y los detalles de infraestructura (qué BD, qué hasher) pueden cambiarse sin tocar ningún caso de uso. El CQRS con MediatR fuerza la separación de lecturas y escrituras: cada Command/Query vive en su propia carpeta con su Handler, Validator y DTO de salida, lo que hace el código navegable y cada caso de uso independientemente auditable.

---

## Decisiones técnicas clave

### Anti double-booking: doble capa de protección

Garantizar que dos reservas no se solapen en el mismo inmueble requiere más que una validación de aplicación, porque dos requests concurrentes pueden pasar la validación al mismo tiempo antes de que cualquiera haga commit.

**Capa 1 — Application:** `HasOverlapAsync` en `BookingRepository` consulta si ya existe una reserva activa que se solape con el período propuesto. Da buen UX con un mensaje de error claro.

**Capa 2 — PostgreSQL:** La tabla `Bookings` tiene una columna `StayPeriod tstzrange` generada automáticamente (STORED) a partir de `CheckIn`/`CheckOut`, con un constraint de exclusión:

```sql
ALTER TABLE "Bookings"
ADD CONSTRAINT "no_overlapping_bookings"
EXCLUDE USING gist ("PropertyId" WITH =, "StayPeriod" WITH &&)
WHERE ("Status" <> 'Cancelled');
```

Esto hace que PostgreSQL rechace cualquier inserción conflictuante a nivel de base de datos, incluso ante condiciones de carrera. Esta capa está **validada con tests de integración reales** usando Testcontainers (PostgreSQL real en Docker, sin mocks): `BookingExclusionConstraintTests` verifica que el solapamiento lanza `PostgresException` con `SqlState = "23P01"` (exclusion_violation).

### Autenticación diferida con sesión anónima

Cualquier visitante puede buscar propiedades y agregar favoritos sin registrarse, usando un `AnonymousSessionId` (GUID en cabecera `X-Session-Id`). Al registrarse o loguearse, `MergeAnonymousSessionCommand` migra automáticamente los favoritos anónimos al usuario real en la misma transacción.

### KYC obligatorio antes de la primera reserva

`CreateBookingCommandHandler` verifica si es la primera reserva del usuario. Si lo es, exige `KycStatus == Approved`. El flujo: `POST /api/kyc` llama a `IGroqKycService` (mock en este MVP que simula aprobación automática; diseñado para ser reemplazado por el cliente real de Groq Llama Vision sin cambiar el handler).

### JWT + BCrypt

- Passwords hasheados con **BCrypt.Net-Next** (cost factor 11 por defecto). BCrypt embebe el salt en el hash y su cost factor escala con el hardware, sin depender de ASP.NET Identity.
- Tokens JWT firmados con HMAC-SHA256, claims: `sub` (UserId), `email`, `isOwner`, `jti`. `MapInboundClaims = false` garantiza que `sub` no se renombra a `NameIdentifier` por el middleware de ASP.NET.
- Mensajes de error del login son idénticos si el email no existe o si la contraseña es incorrecta — evita email enumeration (verificado por dos tests unitarios que aseguran el mismo string).

### Outbox Pattern para notificaciones

Eventos de negocio (`booking.confirmed`, `booking.cancelled`) se escriben en la tabla `OutboxMessages` dentro de la **misma transacción** que la operación de negocio. Esto garantiza atomicidad: si la transacción falla, no queda ningún mensaje huérfano. Un `BackgroundService` publicaría los mensajes pendientes a RabbitMQ (diseñado, no implementado en este MVP).

---

## Estado del proyecto

### Completado y funcional

- **Auth:** `POST /api/auth/register`, `POST /api/auth/login` con JWT + BCrypt. Registro soporta `isOwner: true`.
- **Properties:** `GET /api/properties` (búsqueda paginada por ciudad y fechas, filtra disponibilidad), `GET /api/properties/{id}`, `POST /api/properties` (requiere `isOwner`), `PUT /api/properties/{id}`.
- **Bookings:** `POST /api/bookings` (crea y confirma en un paso, verifica KYC en primera reserva, anti-double-booking doble capa, escribe outbox), `DELETE /api/bookings/{id}` (cancela, escribe outbox).
- **Wishlist:** `GET/POST/DELETE /api/wishlist` — funciona para usuarios autenticados y anónimos (`X-Session-Id`). `MergeAnonymousSessionCommand` disponible para invocar tras login.
- **KYC:** `POST /api/kyc` — flujo completo implementado con `MockGroqKycService` (aprobación automática en dev). Listo para conectar cliente real de Groq.
- **Anti-double-booking:** Validado end-to-end con tests de integración reales (Testcontainers + PostgreSQL).
- **Middleware de excepciones:** Mapeo centralizado `NotFoundException→404`, `ConflictException→409`, `ValidationException→400`, `UnauthorizedException→401`, `DomainException→422`.
- **Docker Compose:** `docker-compose up --build` levanta API + PostgreSQL con migraciones automáticas.
- **56 tests unitarios** (xUnit + Moq + FluentAssertions) + **3 tests de integración** (Testcontainers).

### Diseñado pero no implementado por límite de tiempo

| Feature                     | Estado                                                                 |
|-----------------------------|------------------------------------------------------------------------|
| **KYC con Groq Llama Vision** | Handler y flujo completos. Falta solo el cliente HTTP real a la API de Groq (`MockGroqKycService` activo en su lugar). |
| **RabbitMQ + notificaciones** | Outbox pattern implementado (los mensajes se escriben). Falta el `BackgroundService` que los publica y el microservicio Laravel consumidor. |
| **Frontend React**           | Diseñado en `CLAUDE.md`. No implementado.                             |
| **Dashboard de rendimiento** | Query diseñada. Handler no implementado.                              |
| **Exportación Excel**        | Interfaz `IExcelExportService` diseñada. Handler no implementado.     |

El diseño completo de todas estas piezas, incluyendo decisiones técnicas y justificaciones, está documentado en `CLAUDE.md`.

---

## Cómo correr los tests

```bash
# Tests unitarios (sin Docker, rápidos ~200ms)
dotnet test tests/Lloka.UnitTests/Lloka.UnitTests.csproj

# Tests de integración (requieren Docker Desktop corriendo)
dotnet test tests/Lloka.IntegrationTests/Lloka.IntegrationTests.csproj
```

Los tests de integración usan **Testcontainers** para levantar una instancia real de PostgreSQL 16 en Docker por cada clase de test, ejecutan la migración completa (incluido el constraint `EXCLUDE USING gist`), y la destruyen al terminar. No requieren ninguna base de datos local configurada.
