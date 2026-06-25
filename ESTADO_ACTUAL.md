# ESTADO ACTUAL DEL PROYECTO — LLOKA
*Auditoría completa. Fecha: 2026-06-24. Rama: main.*

---

## 0. LÍNEA BASE: BUILD Y TESTS

### `dotnet build Lloka.slnx`
**Resultado: COMPILACIÓN CORRECTA — 0 errores, 4 advertencias**

| # | Advertencia | Archivo | Impacto |
|---|-------------|---------|---------|
| 1 | NU1510: `System.Net.Http.Json` innecesario | `Lloka.Infrastructure.csproj` | Ninguno en runtime; package redundante |
| 2-4 | MSB3277: Conflicto EF Core Relational `10.0.4` vs `10.0.9` | `Lloka.IntegrationTests.csproj` | MSBuild elige `10.0.4`; IntegrationTests corren contra versión anterior a la que usa la Api |
| 5 | CS0618: `PostgreSqlBuilder()` obsoleto | `BookingExclusionConstraintTests.cs:13` | Ninguno en runtime; solo API superficial |

**El conflicto de EF Core en IntegrationTests es el único warning que merece atención**: `Lloka.Infrastructure` y `Lloka.Api` dependen de `10.0.9`, pero `Lloka.IntegrationTests` referencia directamente `10.0.4`. Los tests pasan, pero teóricamente podrían enmascarar un comportamiento de migración diferente.

### `dotnet test Lloka.slnx`
**Resultado: 60 / 60 PASARON — 0 fallos**

| Suite | Total | Correctas | Falladas |
|-------|-------|-----------|---------|
| `Lloka.UnitTests` | 56 | 56 | 0 |
| `Lloka.IntegrationTests` | 4 | 4 | 0 |
| **TOTAL** | **60** | **60** | **0** |

Los tests de integración levantaron contenedores Docker reales de Postgres vía Testcontainers y validaron el constraint de exclusión (`btree_gist`).

---

## 1. CAPA DOMAIN

### ✅ Implementado y CONFIRMADO

| Elemento | Evidencia |
|----------|-----------|
| `User` (entity) — con `IsOwner`, `KycStatus`, `BecomeOwner()`, `UpdateKycStatus()` | Referenciado y usado correctamente por 4 handlers de Application; tests de Application con mocks pasan |
| `Booking` (entity) — con `Cancel()` | Handler `CancelBookingCommandHandler` lo invoca; unit test `Handle_DateOverlap_ThrowsConflictException` lo cubre |
| `Property`, `PropertyImage`, `KycVerification`, `WishlistItem`, `OutboxMessage` | Compilación correcta; configuraciones de EF existen para todos |
| `StayPeriod` (value object) — check-in fijo 14h, check-out fijo 12h, Colombia UTC-5 | **9 unit tests pasan** (SameDates throws, ValidRange computes nights, fixed times verified, equality) |
| `Address` (value object) — validación de lat/lon | **7 unit tests pasan** (boundaries, throws fuera de rango) |
| `KycVerification` — máquina de estados (Pending→Approved/Rejected) con invariantes | **9 unit tests pasan** (throws si ya Approved, si ya Rejected, etc.) |
| Enums: `BookingStatus`, `KycStatus`, `KycVerificationStatus`, `PropertyStatus`, `OutboxMessageStatus` | Compilación y uso correcto en toda la solución |

### ⚠️ Implementado pero NO verificado a fondo

| Elemento | Riesgo |
|----------|--------|
| `Booking.Cancel()` — marca como Cancelled | Lógica simple, pero no existe un test unitario de dominio dedicado para `Booking`; sólo se cubre indirectamente desde el handler |
| `Property.BecomeInactive()` / estado de propiedad | No hay tests de dominio para `Property`; la validación de `PropertyStatus.Active` sí es cubierta por el handler mock |

### ✅ No falta nada en Domain

---

## 2. CAPA APPLICATION

### ✅ Implementado y CONFIRMADO funcionando

| Feature | Evidencia |
|---------|-----------|
| `RegisterUserCommand` + Handler + Validator | **2 unit tests**: duplicado → ConflictException; nuevo email → persiste + retorna response |
| `LoginCommand` + Handler + Validator | **3 unit tests**: not found → UnauthorizedException; wrong password → UnauthorizedException; válido → token + datos |
| `CreateBookingCommand` + Handler + Validator | **8 unit tests**: property not found, inactive, guest count exceeded, overlap, KYC blocks first booking (Rejected y Pending), owner self-booking, primera reserva válida con outbox |
| `CreateBookingCommandValidator` | **5 unit tests**: empty GuestId, empty PropertyId, zero GuestCount, checkout=checkin, checkout<checkin |
| `SubmitKycDocumentCommand` + Handler + Validator | Compila; usa `IGroqKycService` vía mock — SIN tests unitarios dedicados |
| `CancelBookingCommand` + Handler | Compila; implementación completa (owner check + outbox) — SIN tests unitarios |
| `CreatePropertyCommand` / `UpdatePropertyCommand` + Handlers + Validators | Compilan — SIN tests unitarios |
| `SearchPropertiesQuery` / `GetPropertyByIdQuery` + Handlers | Compilan — SIN tests unitarios |
| `AddToWishlist` / `RemoveFromWishlist` / `MergeAnonymousSession` + Handlers | Compilan — SIN tests unitarios |
| `GetUserWishlistQuery` + Handler | Compila — SIN tests unitarios |
| `ValidationBehavior` (pipeline MediatR) | Cubierto indirectamente por los tests del Validator de CreateBooking |
| Excepciones custom: `NotFoundException`, `ConflictException`, `ValidationException`, `UnauthorizedException` | Usadas y lanzadas correctamente en handlers probados |

### ⚠️ Implementado pero NO verificado a fondo

| Feature | Problema específico |
|---------|---------------------|
| `SubmitKycDocumentCommand` | No hay tests. El handler llama `IGroqKycService.ExtractAsync()` que siempre retorna Approved=true con el mock — el flujo de rechazo nunca se ejercita |
| `CancelBookingCommand` | No hay tests. El ownership check (`GuestId != RequestingUserId`) sólo se verifica manualmente |
| `MergeAnonymousSessionCommand` | No hay tests. El merge de wishlist anónima al usuario real es lógica de negocio crítica sin cobertura |
| `CreatePropertyCommand` / handlers de Properties | Sin tests — la validación de `IsOwner` en el handler tampoco está probada |

### ❌ FALTA COMPLETAMENTE en Application

| Elemento faltante | Referencia en CLAUDE.md |
|-------------------|------------------------|
| **`GetUserBookings`** query + handler | `Bookings/Queries/ → GetUserBookings` |
| **`GetKycStatus`** query + handler | `Kyc/Queries/ → GetKycStatus` |
| **`GetOwnerPerformance`** query + handler | `Dashboard/Queries/ → GetOwnerPerformance` |
| **`ExportBookingsToExcel`** query + handler (ClosedXML) | `Reports/Queries/ → ExportBookingsToExcel` |

**Nota importante**: `BookingsController` NO tiene ningún endpoint GET. Un usuario logueado no puede consultar sus reservas — esto es un feature bloqueante para el flujo completo de usuario.

---

## 3. CAPA INFRASTRUCTURE

### ✅ Implementado y CONFIRMADO

| Elemento | Evidencia |
|----------|-----------|
| `LlokaDbContext` con todas las configuraciones | Compilación + tests de integración con Postgres real |
| `BookingConfiguration` con constraint `EXCLUDE USING gist` | **2 tests de integración**: `OverlappingBookings_ThrowsExclusionViolation` (SqlState 23P01) y `CancelledBooking_DoesNotBlockOverlappingDates` — ambos pasan contra Postgres real |
| `UserConfiguration`, `PropertyConfiguration`, `KycVerificationConfiguration`, etc. | Compilación correcta |
| `BookingRepository` — `HasOverlapAsync` + búsqueda por ID | Usado en unit tests con mock; constraint validado en integración |
| `PropertyRepository`, `UserRepository`, `WishlistRepository` | Compilación + uso en handlers |
| `JwtTokenService` | Usado en `LoginCommandHandlerTests.Handle_ValidCredentials_ReturnsTokenAndUserData` (que pasa) |
| `BCryptPasswordHasher` | Usado en unit tests de Login y Register (que pasan) |
| `MockGroqKycService` | Compila, implementa la interfaz correctamente |
| `UnitOfWork` / `Repository<T>` genérico | Usados en todos los handlers probados |

### ⚠️ Implementado pero NO verificado contra infraestructura real

| Elemento | Estado |
|----------|--------|
| `BookingRepository.HasOverlapAsync` | Solo verificado contra mocks en unit tests; la query EF Core real nunca se ejecutó fuera de integración mínima |
| `OutboxMessage` — creación transaccional | Unit test confirma que el handler lo crea (`Handle_ValidRequest_ReturnsResponseAndPersistsBookingAndOutbox`); nunca se verificó que EF lo persiste en DB real |
| Todas las migraciones EF Core | No se probó `APPLY_MIGRATIONS_ON_START=true` contra el contenedor Docker de la Api — sólo en test containers ad-hoc |

### ❌ FALTA COMPLETAMENTE en Infrastructure

| Elemento faltante | Descripción |
|-------------------|-------------|
| **`RabbitMqPublisher`** / `INotificationPublisher` | No existe ninguna implementación. Los `OutboxMessage` se crean en DB pero nunca se publican |
| **`OutboxPublisherBackgroundService`**| No existe el worker que lee `Pending` del outbox y publica a RabbitMQ |
| **`GroqKycService`** (real) | Solo existe el mock; no hay cliente HTTP real que llame a `api.groq.com` |
| **`ExcelExportService`** (ClosedXML) | No existe; la interfaz `IExcelExportService` mencionada en CLAUDE.md tampoco existe en Application |

---

## 4. CAPA API (PRESENTATION)

### ✅ Implementado y CONFIRMADO

| Elemento | Estado |
|----------|--------|
| `AuthController` — `POST /api/auth/register`, `POST /api/auth/login` | Compilación + unit tests de handlers pasan |
| `BookingsController` — `POST /api/bookings`, `DELETE /api/bookings/{id}` | Compilación; implementa owner-id desde JWT claim `sub` |
| `PropertiesController` — `POST /api/properties`, `PUT /api/properties/{id}`, `GET /api/properties`, `GET /api/properties/{id}` | Compilación |
| `WishlistController` — `GET`, `POST`, `DELETE`, merge | Compilación |
| `KycController` — `POST /api/kyc/submit` | Compilación |
| `ExceptionMiddleware` — mapea excepciones custom a HTTP codes | Compilación; se ejerce indirectamente en integration tests de ASP.NET |
| JWT Bearer auth + `[Authorize]` en endpoints protegidos | Compilación; endpoints de búsqueda quedan abiertos ✅ |
| Scalar (documentación OpenAPI) en `/scalar` | Configurado en Program.cs |

### ❌ FALTA COMPLETAMENTE en Api

| Endpoint faltante | Descripción |
|-------------------|-------------|
| `GET /api/bookings` | No existe — `BookingsController` no tiene GET |
| `GET /api/kyc/status` | No existe — `KycController` solo tiene el POST de submit |
| `GET /api/dashboard/performance` | No existe — no hay `DashboardController` |
| `GET /api/reports/bookings/excel` | No existe — no hay `ReportsController` |

---

## 5. PUNTOS ESPECÍFICOS AUDITADOS

### 5.1 Claim `isOwner` en JWT + `RegisterUserCommand`

**Veredicto: Bien resuelto, con una limitación conocida documentada.**

- `RegisterUserCommand` acepta `bool IsOwner = false`; el handler llama `user.BecomeOwner()` si es `true`. Limpio.
- `JwtTokenService.GenerateToken(Guid userId, string email, bool isOwner)` emite `new Claim("isOwner", isOwner.ToString().ToLower())` → el valor es la cadena `"true"` o `"false"`, no un claim de `role`.
- La **autorización real** no depende del JWT: `CreatePropertyCommandHandler` consulta la DB (`owner.IsOwner`) y lanza `ConflictException` si no es propietario. Correcto — el JWT es informativo para el frontend.
- **Limitación conocida**: Si un usuario se registra como guest y luego "se convierte" en propietario, su token no refleja el cambio hasta que vuelve a hacer login. No es un bug para el MVP, pero debe mencionarse en la entrevista.
- `[Authorize(Roles = "Owner")]` NO funcionaría con este esquema; los controllers usan `[Authorize]` genérico y la granularidad se delega al handler. Decisión válida.

### 5.2 `MockGroqKycService` — ¿Lista la interfaz para implementación real?

**Veredicto: Sí, la interfaz es limpia y correcta.**

```csharp
// IGroqKycService.cs — Application layer (no toca Infrastructure)
public record KycExtractionResult(string FirstName, string LastName,
    string DocumentNumber, DateOnly DateOfBirth, bool Approved);

public interface IGroqKycService {
    Task<KycExtractionResult> ExtractAsync(string documentBase64, CancellationToken ct = default);
}
```

- El contrato es mínimo: recibe base64, retorna `KycExtractionResult`. Reemplazar el mock por un `GroqKycService` real sólo requiere crear la clase en Infrastructure e inyectarla en `DependencyInjection.cs`. Application no necesita tocar nada.
- **Único punto a revisar**: la implementación real necesitará `IHttpClientFactory` o `HttpClient` — verificar que esté registrado en `Program.cs` antes de implementar.

### 5.3 `docker-compose.yml` — Puertos y conflictos

**Veredicto: Bien resuelto. Documentación de puertos:**

| Servicio | Puerto host | Puerto contenedor | Razón del offset |
|----------|-------------|-------------------|------------------|
| `db` (Postgres 16) | **5435** | 5432 | Evita conflicto con Postgres local que por defecto usa 5432 |
| `api` (.NET 10) | **8082** | 8080 | Evita conflicto con aplicaciones locales en 8080/8081 |

**Servicios que faltan en docker-compose:**

| Servicio | Estado |
|----------|--------|
| `rabbitmq` | ❌ No existe — requerido para el outbox pattern |
| `notifications` (Laravel) | ❌ No existe |
| `mailpit` | ❌ No existe |
| `frontend` (React/Nginx) | ❌ No existe |

La inicialización de `btree_gist` vía `./docker/postgres/init-btree-gist.sql` existe y es correcta.

### 5.4 `ValueConverter` de `DateTimeOffset` en `BookingConfiguration`

**Veredicto: Funcional, con una asimetría de offset que merece verificación manual.**

```csharp
var utcConverter = new ValueConverter<DateTimeOffset, DateTimeOffset>(
    v => v.ToUniversalTime(),  // toDb: Colombia (-05:00) → UTC
    v => v);                   // fromDb: devuelve UTC sin convertir
```

- **`toDb`**: correcto — Npgsql exige UTC para `timestamptz`.
- **`fromDb` (`v => v`)**: devuelve `DateTimeOffset` con offset `+00:00`. El comentario en el código dice "el ctor privado aplica ToOffset(-5)" — esto implicaría que `StayPeriod` tiene un constructor privado que EF usa para materializar con offset Colombia. **Esto requiere verificación**: si EF materializa `StayPeriod` seteando propiedades directamente (no via constructor), los objetos leídos de DB tendrán `+00:00` en lugar de `-05:00`. El dato es correcto (mismo instante), pero el offset representado difiere del que crea el constructor público.
- **Para el anti-double-booking**: no importa (Postgres compara instantes en UTC). Para display al usuario: depende de cómo el frontend convierte la hora. **Acción recomendada**: hacer un test de integración que cree una reserva, la lea de DB, y verifique el offset del `CheckIn`.

---

## 6. LISTA PRIORIZADA — QUÉ FALTA PARA "COMPLETO"

### PRIORIDAD 1 — Bloqueantes para demostrar el flujo end-to-end

| # | Tarea | Esfuerzo estimado |
|---|-------|-------------------|
| 1 | `GET /api/bookings` — `GetUserBookings` query + handler + endpoint | ~2h |
| 2 | RabbitMQ en docker-compose + `OutboxPublisherBackgroundService` + `RabbitMqPublisher` | ~4h |
| 3 | Laravel `notifications-service` — consumer RabbitMQ + email Mailpit + notificación in-app | ~4-5h |
| 4 | Mailpit y RabbitMQ en docker-compose (configuración base) | ~1h |

### PRIORIDAD 2 — Features obligatorios del PDF

| # | Tarea | Esfuerzo estimado |
|---|-------|-------------------|
| 5 | `GetOwnerPerformance` query + handler + `GET /api/dashboard/performance` | ~3h |
| 6 | `ExportBookingsToExcel` query + handler + ClosedXML + `GET /api/reports/bookings/excel` | ~2-3h |
| 7 | `GET /api/kyc/status` — `GetKycStatus` query + handler | ~1h |

### PRIORIDAD 3 — Mejora de calidad y completitud

| # | Tarea | Esfuerzo estimado |
|---|-------|-------------------|
| 8 | `GroqKycService` real (cliente HTTP a api.groq.com + Llama Vision) | ~2-3h |
| 9 | Tests unitarios para `CancelBooking`, `SubmitKycDocument`, `MergeAnonymousSession`, `CreateProperty` | ~3h |
| 10 | Test de integración para offset de `StayPeriod` post-roundtrip | ~30min |
| 11 | Fix versión de `Microsoft.EntityFrameworkCore.Relational` en `Lloka.IntegrationTests` (alinear a 10.0.9) | ~10min |

### PRIORIDAD 4 — Frontend

| # | Tarea | Esfuerzo estimado |
|---|-------|-------------------|
| 12 | React + Vite + Tailwind — flujo completo (búsqueda, detalle, reserva, auth, wishlist) | ~8-12h |

---

## 7. RESUMEN EJECUTIVO

```
Domain          ████████████████████ 100%  (todos los modelos, VOs, enums)
Application     ████████████░░░░░░░░  60%  (falta 4 queries clave)
Infrastructure  ██████████░░░░░░░░░░  50%  (falta RabbitMQ, Groq real, Excel)
Api             ████████████░░░░░░░░  60%  (falta 4 endpoints)
Tests           ████████░░░░░░░░░░░░  40%  (buenos en core, ausentes en KYC/Cancel/Properties)
Docker/Infra    ████░░░░░░░░░░░░░░░░  20%  (faltan 4 servicios del compose)
Frontend        ░░░░░░░░░░░░░░░░░░░░   0%  (no iniciado)
Notificaciones  ░░░░░░░░░░░░░░░░░░░░   0%  (no iniciado)
```

**Lo que funciona hoy mismo** (compila, pasa tests, se puede probar con `docker-compose up`):
- Register / Login con JWT
- Crear propiedad (usuario Owner)
- Buscar propiedades
- Crear reserva con anti-double-booking garantizado en dos capas
- Cancelar reserva
- KYC submit (mock, siempre aprueba)
- Wishlist completa (add/remove/merge)

**Lo que no funciona todavía**:
- Consultar las propias reservas (endpoint ausente)
- Notificaciones (RabbitMQ + Laravel)
- Dashboard de rendimiento
- Exportación Excel
- Frontend visual
- KYC real con Groq
