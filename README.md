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

## Cómo levantarlo

```bash
# 1. Clonar el repositorio
git clone <url-del-repo> && cd Lloka

# 2. (Opcional) Copiar las variables de entorno
cp .env.example .env   # y editar si quieres cambiar contraseñas

# 3. Levantar todo con un solo comando
docker-compose up --build
```

Al arrancar, el contenedor de la API aplica automáticamente las migraciones de EF Core contra la base de datos del Compose (controlado por `APPLY_MIGRATIONS_ON_START=true`). No necesitas correr `dotnet ef database update` manualmente.

### Endpoints disponibles

| URL                          | Qué es                                      |
|------------------------------|---------------------------------------------|
| `http://localhost:8080/scalar` | Documentación interactiva (Scalar UI)     |
| `http://localhost:8080/openapi/v1.json` | Documento OpenAPI generado por .NET  |
| `http://localhost:5432`       | PostgreSQL (usuario: `lloka`, pass: `lloka`)|

### Flujo de prueba rápida en Scalar

1. `POST /api/auth/register` — crea un usuario (incluye `"isOwner": true` para publicar inmuebles)
2. `POST /api/auth/login` — obtén el JWT
3. En Scalar: clic en el candado, pega el token → los endpoints protegidos quedan autorizados
4. `POST /api/properties` — publica un inmueble
5. `GET /api/properties` — busca (filtra por `city`, `checkIn`, `checkOut`)
6. `POST /api/bookings` — crea una reserva

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

`CreateBookingCommandHandler` verifica si es la primera reserva del usuario. Si lo es, exige `KycStatus == Approved`. El modelo de KYC incluye la entidad `KycVerification`, el método `User.SubmitKycVerification()`, y la interfaz `IGroqKycService` para extracción de datos de cédula vía Llama Vision de Groq. **El cliente real de Groq no está implementado en este MVP** (ver "Estado del proyecto").

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
- **Bookings:** `POST /api/bookings` (crea y confirma en un paso, instant booking, verifica KYC en primera reserva, anti-double-booking doble capa, escribe outbox), `DELETE /api/bookings/{id}` (cancela, escribe outbox).
- **Wishlist:** `GET/POST/DELETE /api/wishlist` — funciona para usuarios autenticados y anónimos (`X-Session-Id`). `MergeAnonymousSessionCommand` disponible para invocar tras login.
- **Anti-double-booking:** Validado end-to-end con tests de integración reales (Testcontainers + PostgreSQL).
- **Middleware de excepciones:** Mapeo centralizado `NotFoundException→404`, `ConflictException→409`, `ValidationException→400`, `UnauthorizedException→401`, `DomainException→422`.
- **Docker Compose:** `docker-compose up --build` levanta API + PostgreSQL con migraciones automáticas.
- **56 tests unitarios** (xUnit + Moq + FluentAssertions) + **3 tests de integración** (Testcontainers).

### Diseñado pero no implementado por límite de tiempo

| Feature                     | Estado                                                                 |
|-----------------------------|------------------------------------------------------------------------|
| **KYC con Groq Llama Vision** | Interfaz `IGroqKycService` definida, entidades y flujo documentados. Falta el cliente HTTP real a la API de Groq y el command handler final. |
| **RabbitMQ + notificaciones** | Outbox pattern implementado (los mensajes se escriben). Falta el `BackgroundService` que los publica y el microservicio Laravel consumidor. |
| **Frontend React**           | Diseñado en `CLAUDE.md`. No implementado.                             |
| **Dashboard de rendimiento** | Query diseñada. Handler no implementado.                              |
| **Exportación Excel**        | Interfaz `IExcelExportService` diseñada. Handler no implementado.     |

El diseño completo de todas estas piezas, incluyendo decisiones técnicas y justificaciones, está documentado en `CLAUDE.md`.

---

## Cómo correr los tests

```bash
# Tests unitarios (sin Docker, rápidos)
dotnet test tests/Lloka.UnitTests/Lloka.UnitTests.csproj

# Tests de integración (requieren Docker Desktop corriendo)
dotnet test tests/Lloka.IntegrationTests/Lloka.IntegrationTests.csproj
```

Los tests de integración usan **Testcontainers** para levantar una instancia real de PostgreSQL 16 en Docker por cada clase de test, ejecutan la migración, y la destruyen al terminar. No requieren ninguna base de datos local configurada.
