# PLAN — Bloque 1: Queries, Tests y Fixes
*Estado: PENDIENTE DE APROBACIÓN*

---

## Contexto de las decisiones

Antes de cada punto describo lo que leí del código real para que la
decisión no sea una suposición.

---

## PUNTO 1 — `GetUserBookings` Query + Handler + `GET /api/bookings`

### Lo que existe hoy

- `IBookingRepository` tiene: `HasOverlapAsync`, `HasConfirmedBookingAsync`.
- `Repository<T>.GetByIdAsync` usa `FindAsync` — no hace eager load.
- `Booking` no tiene navigation property a `Property`, sólo `PropertyId` (FK).
- `BookingsController` tiene `POST` y `DELETE` pero **ningún `GET`**.

### Problema de diseño: obtener `PropertyTitle`

Para el DTO de respuesta necesitamos `PropertyTitle` que vive en `Property`.
`Booking` no tiene navigation property a `Property`, así que un simple
`GetByIdAsync(bookingId)` no sirve.

**Opción descartada**: N+1 (buscar cada propiedad por separado por `PropertyId`).  
**Opción descartada**: Agregar navigation property `Property` a la entidad `Booking`
en Domain — cambia el modelo de dominio sólo por conveniencia de una query,
que es exactamente el tipo de cambio que no debe hacerse.  
**Opción elegida**: Agregar un método especializado a `IBookingRepository` que retorna
un DTO de Application directamente, con un JOIN en la implementación de
Infrastructure. Este es el patrón correcto para queries de solo lectura
que cruzan agregados: la interfaz define el contrato y el DTO en Application,
Infrastructure hace el JOIN en SQL vía LINQ + EF.

### Archivos a crear

```
src/Core/Lloka.Application/Bookings/Queries/GetUserBookings/
    GetUserBookingsQuery.cs          → record IRequest<IReadOnlyList<BookingListItemResult>>
    GetUserBookingsQueryHandler.cs   → llama IBookingRepository.GetUserBookingsAsync()
    BookingListItemResult.cs         → DTO plano (ver campos abajo)
```

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `Application/Common/Interfaces/IBookingRepository.cs` | Agregar `Task<IReadOnlyList<BookingListItemResult>> GetUserBookingsAsync(Guid guestId, CancellationToken ct)` |
| `Infrastructure/Persistence/Repositories/BookingRepository.cs` | Implementar el método con EF JOIN |
| `Api/Controllers/BookingsController.cs` | Agregar `GET /api/bookings` que lee `UserId` del JWT claim `sub` |

### DTO de respuesta — `BookingListItemResult`

```csharp
public record BookingListItemResult(
    Guid          BookingId,
    string        PropertyTitle,
    DateTimeOffset CheckIn,
    DateTimeOffset CheckOut,
    decimal       TotalAmount,
    BookingStatus Status,
    int           Nights);
```

- `Nights` se calcula **en el repositorio** client-side (post-EF) como
  `(int)(CheckOut.Date - CheckIn.Date).TotalDays`. Funciona correctamente
  con UTC porque CheckIn UTC es la misma fecha calendario que CheckIn Colombia
  (14h Colombia = 19h UTC, mismo día). `StayPeriod.Nights` está marcado como
  `Ignored` en EF y no se puede proyectar directamente desde SQL.

### Implementación del JOIN en `BookingRepository`

```csharp
public async Task<IReadOnlyList<BookingListItemResult>> GetUserBookingsAsync(
    Guid guestId, CancellationToken ct)
{
    var raw = await Context.Bookings
        .Where(b => b.GuestId == guestId)
        .Join(Context.Properties,
              b => b.PropertyId,
              p => p.Id,
              (b, p) => new {
                  b.Id, PropertyTitle = p.Title,
                  b.StayPeriod.CheckIn, b.StayPeriod.CheckOut,
                  b.TotalAmount, b.Status
              })
        .OrderByDescending(x => x.CheckIn)
        .ToListAsync(ct);

    return raw.Select(x => new BookingListItemResult(
        x.Id, x.PropertyTitle, x.CheckIn, x.CheckOut, x.TotalAmount, x.Status,
        (int)(x.CheckOut.Date - x.CheckIn.Date).TotalDays))
        .ToList();
}
```

EF proyecta propiedades de owned entities (`b.StayPeriod.CheckIn`) sin problema
porque están mapeadas como columnas normales en `BookingConfiguration`.

### Endpoint en `BookingsController`

```
[HttpGet]
[Authorize]
public async Task<IActionResult> GetUserBookings(CancellationToken ct)
```

Lee `UserId` del claim `sub` (igual que `CreateBooking` y `CancelBooking`).
Retorna `Ok(result)` — 200 con la lista (puede ser vacía, nunca 404).

### Supuesto explícito

El endpoint devuelve TODAS las reservas del usuario, sin paginación. Para un
MVP es suficiente. Si en el futuro se necesita paginación, se agrega
`page`/`pageSize` a la query y al repositorio.

### Qué NO se toca

- La entidad `Booking` en Domain (no se agrega navigation property).
- Los tests unitarios existentes (el nuevo método en la interfaz requiere que
  los mocks existentes lo implementen, pero Moq genera todos los métodos de
  la interfaz automáticamente — no hay que tocar los test files existentes).

---

## PUNTO 2 — `GetKycStatus` Query + Handler + `GET /api/kyc/status`

### Lo que existe hoy

- `UserRepository.GetByIdAsync` usa `FindAsync` → **no carga** la colección
  `KycVerifications` (navigation property).
- `User.KycVerifications` es `IReadOnlyCollection<KycVerification>` — se
  inicializa con `List<KycVerification> _kycVerifications = []` pero EF la
  llena sólo si se hace eager load o lazy load.
- `KycController` tiene sólo `POST /api/kyc/submit`.

### Diseño

Para leer `User.KycStatus` + la última verificación necesito cargar la
navigation. Opciones:

**Opción descartada**: Lazy loading — requiere proxies, añade complejidad,
no está habilitado en la solución.  
**Opción elegida**: Agregar `GetByIdWithKycAsync(Guid userId)` a `IUserRepository`.
Devuelve `User?` con `.Include(u => u.KycVerifications)`. Sigue el mismo
patrón que `GetByEmailAsync` (método especializado en la interfaz).

### Archivos a crear

```
src/Core/Lloka.Application/Kyc/Queries/GetKycStatus/
    GetKycStatusQuery.cs             → record(UserId) : IRequest<GetKycStatusResponse>
    GetKycStatusQueryHandler.cs      → busca user con KYC incluido
    GetKycStatusResponse.cs          → DTO (ver campos abajo)
```

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `Application/Common/Interfaces/IUserRepository.cs` | Agregar `Task<User?> GetByIdWithKycAsync(Guid userId, CancellationToken ct)` |
| `Infrastructure/Persistence/Repositories/UserRepository.cs` | Implementar con `.Include(u => u.KycVerifications)` |
| `Api/Controllers/KycController.cs` | Agregar `GET /api/kyc/status` con `[Authorize]` |

### DTO de respuesta

```csharp
public record GetKycStatusResponse(
    KycStatus              KycStatus,
    KycVerificationSummary? LatestVerification);

public record KycVerificationSummary(
    Guid                  VerificationId,
    KycVerificationStatus Status,
    DateTime              SubmittedAt,
    DateTime?             ReviewedAt);
```

**No se exponen** los datos extraídos de la cédula (FirstName, LastName,
DocumentNumber, DateOfBirth). Sólo metadata de auditoría.

### Lógica del handler

```
user = GetByIdWithKycAsync(userId) ?? throw NotFoundException
latest = user.KycVerifications.OrderByDescending(v => v.SubmittedAt).FirstOrDefault()
return GetKycStatusResponse(user.KycStatus, latest != null ? Summary(latest) : null)
```

### Qué NO se toca

- `SubmitKycDocumentCommandHandler` no cambia.
- El modelo de dominio `KycVerification` no cambia.

---

## PUNTO 3 — Tests unitarios faltantes

Todos siguen exactamente el mismo patrón que `CreateBookingCommandHandlerTests`:
campos de mock como `readonly`, método helper `CreateHandler()`, builders de
entidades como métodos `static`, `Act` con lambda + `await`.

### 3a — `CancelBookingCommandHandlerTests`

**Handler**: usa `IBookingRepository`, `IRepository<OutboxMessage>`, `IUnitOfWork`.  
**Excepción para acceso no autorizado**: el código actual lanza `ConflictException`
(confirmado en `CancelBookingCommandHandler.cs:22`: `throw new ConflictException(...)`).
No es `UnauthorizedException` — se usa `ConflictException` por consistencia
con el resto de los handlers de este proyecto.

**Casos**:
1. `Handle_HappyPath_CancelsBookingAndPersistsOutbox` — GuestId == RequestingUserId,
   verifica `bookingRepo.Update`, `outboxRepo.AddAsync`, `unitOfWork.SaveChangesAsync` x1.
2. `Handle_BookingNotFound_ThrowsNotFoundException`
3. `Handle_RequesterIsNotGuest_ThrowsConflictException` — GuestId ≠ RequestingUserId

**Nota sobre builder**: `Booking.Create(...)` retorna estado `Pending`. El handler
llama `booking.Cancel()` que funciona desde `Pending` → `Cancelled` sin problema.

### 3b — `SubmitKycDocumentCommandHandlerTests`

**Handler**: usa `IUserRepository`, `IGroqKycService`, `IUnitOfWork`.  
**Mock de IGroqKycService**: se configura con `Setup(...).ReturnsAsync(result)` —
`MockGroqKycService` es la implementación en Infrastructure, pero en los tests se
usa `Mock<IGroqKycService>` directamente (igual que los otros handlers usan
`Mock<IRepository<T>>`). El mock puede configurarse para ambos resultados.

**Casos**:
1. `Handle_ApprovedExtraction_SetsKycApprovedAndSaves`
   - Mock: `groqKyc.ExtractAsync(...)` → `KycExtractionResult(..., Approved: true)`
   - Verifica: `user.KycStatus == Approved`, `unitOfWork.SaveChangesAsync` x1.
2. `Handle_RejectedExtraction_SetsKycRejectedAndSaves`
   - Mock: `groqKyc.ExtractAsync(...)` → `KycExtractionResult(..., Approved: false)`
   - Verifica: `user.KycStatus == Rejected`, `unitOfWork.SaveChangesAsync` x1.
3. `Handle_UserNotFound_ThrowsNotFoundException`
4. `Handle_AlreadyApproved_ThrowsConflictException`
   - El handler lanza `ConflictException("Tu identidad ya fue verificada.")` si
     `user.KycStatus == Approved`.

**Supuesto**: No se puede inspeccionar `user.KycStatus` directamente en el mock
porque `IUserRepository.GetByIdAsync` retorna la entidad real `User.Create(...)`,
que se crea con `KycStatus.NotSubmitted`. Para el test de `AlreadyApproved` se
llama `user.UpdateKycStatus(KycStatus.Approved)` antes de configurar el mock.

### 3c — `MergeAnonymousSessionCommandHandlerTests`

**Handler**: usa `IWishlistRepository`, `IUnitOfWork`.

**Casos**:
1. `Handle_WithItems_AssignsToUserAndSaves`
   - Mock: `wishlistRepo.GetBySessionIdAsync(...)` → lista con 2 `WishlistItem`
   - Verifica: cada item tiene `UserId` seteado (llamar `AssignToUser`),
     `unitOfWork.SaveChangesAsync` exactamente x1.
2. `Handle_WithNoItems_ReturnsWithoutSaving`
   - Mock: `wishlistRepo.GetBySessionIdAsync(...)` → lista vacía
   - Verifica: `unitOfWork.SaveChangesAsync` NUNCA se llama (el handler hace
     early return en `items.Count == 0`).

**Supuesto**: `WishlistItem` tiene un método `AssignToUser(Guid userId)`. Necesito
confirmar que existe y que setea el `UserId`. Si no existe, el test verificará que
al menos el SaveChanges no se llama en el caso vacío. Leeré el entity antes de
escribir el test para no hacer suposiciones.

### 3d — `CreatePropertyCommandHandlerTests`

**Handler**: usa `IUserRepository`, `IPropertyRepository`, `IUnitOfWork`.

**Casos**:
1. `Handle_HappyPath_CreatesPropertyAndSaves`
   - Mock: user es Owner (`user.BecomeOwner()`), repo retorna el user.
   - Verifica: `propertyRepo.AddAsync` x1, `unitOfWork.SaveChangesAsync` x1.
   - Verifica response: `PropertyId != Guid.Empty`, `Title` correcto.
2. `Handle_UserNotFound_ThrowsNotFoundException`
3. `Handle_UserIsNotOwner_ThrowsConflictException`
   - User creado con `User.Create(...)` sin llamar `BecomeOwner()`.
4. `Handle_InvalidCoordinates_ThrowsDomainException`
   - Latitud `999m` → `Address` constructor lanza `DomainException`.
   - **Importante**: esto sólo es testeable a nivel handler porque el
     `ValidationBehavior` NO corre en unit tests directos del handler.
     Si el `CreatePropertyCommandValidator` también valida coordenadas,
     en producción la validación ocurre antes; en el unit test del handler
     la `DomainException` llega hasta el caller. El test es válido y documenta
     que hay dos capas de validación para coordenadas.

---

## PUNTO 4 — Test de integración: `StayPeriod` offset post-roundtrip

### Análisis del código antes de diseñar el test

`StayPeriod` es `sealed record` con dos constructores:

```csharp
// Constructor público — lo usa el dominio
public StayPeriod(DateOnly checkInDate, DateOnly checkOutDate)
    → CheckIn = new DateTimeOffset(..., ColombiaTzOffset)  // offset = -05:00

// Constructor privado — coincide en nombre y tipo con lo que EF materializa desde DB
private StayPeriod(DateTimeOffset checkIn, DateTimeOffset checkOut)
    → CheckIn = checkIn.ToOffset(ColombiaTzOffset)          // offset = -05:00
```

El `fromDb` converter en `BookingConfiguration` retorna `v => v` (UTC sin convertir).
EF Core puede usar constructores privados en owned entities vía reflexión si los
parámetros coinciden por nombre (case-insensitive) y tipo con las propiedades
mapeadas. Los parámetros `checkIn`/`checkOut` coinciden con las propiedades
`CheckIn`/`CheckOut` del tipo `DateTimeOffset`. **La expectativa es que el
roundtrip preserve el offset `-05:00`.**

Si el test FALLA, el diagnóstico antes de cualquier arreglo:

| Síntoma | Causa probable |
|---------|---------------|
| `CheckIn.Offset == +00:00` | EF usa setters directos (no el ctor privado) porque el `record` tiene propiedades `{ get; }` — EF puede setear via reflection sin llamar al ctor |
| `CheckIn.Hour == 19` (no 14) | Confirmaría lo anterior: se guarda UTC 19h, se lee UTC 19h sin conversión |

Si el test falla, **reportar exactamente qué valores tienen `CheckIn.Offset` y `CheckIn.Hour`** antes de arreglar, para entender si es un problema de EF, del converter, o del constructor.

### Archivo a crear

```
tests/Lloka.IntegrationTests/Persistence/StayPeriodRoundtripTests.cs
```

**Clase nueva** (no agregar en `BookingExclusionConstraintTests.cs`): mismo patrón
`IAsyncLifetime` con `PostgreSqlContainer`.

### Estructura del test

```csharp
[Fact]
public async Task Booking_AfterDbRoundtrip_StayPeriodHasColombiaOffset()
{
    // Arrange: crear booking, persistir
    await using var freshContext = new LlokaDbContext(FreshOptions());
    
    // Assert (si pasa):
    retrieved.StayPeriod.CheckIn.Offset.Should().Be(TimeSpan.FromHours(-5));
    retrieved.StayPeriod.CheckOut.Offset.Should().Be(TimeSpan.FromHours(-5));
    retrieved.StayPeriod.CheckIn.Hour.Should().Be(14);
    retrieved.StayPeriod.CheckOut.Hour.Should().Be(12);
    retrieved.StayPeriod.Nights.Should().Be(4); // Sep 1 → Sep 5
}
```

**Requisito clave**: el `DbContext` con el que se lee DEBE ser diferente del que
guardó. `FindAsync` / `FirstOrDefaultAsync` con el mismo contexto podría retornar
el objeto del identity cache (no materializado desde SQL). Un contexto nuevo fuerza
ir a DB.

---

## PUNTO 5 — Fix: conflicto de versión EF Core en `Lloka.IntegrationTests`

### Diagnóstico

`Lloka.IntegrationTests.csproj` no tiene referencia directa a
`Microsoft.EntityFrameworkCore.Relational`. El conflicto viene de que:
- `Testcontainers.PostgreSql 4.12.0` → `Npgsql.EntityFrameworkCore.PostgreSQL` →
  `Microsoft.EntityFrameworkCore.Relational 10.0.4` (versión más antigua)
- `Lloka.Api` (referenciado vía `<ProjectReference>`) → `Lloka.Infrastructure` →
  `Microsoft.EntityFrameworkCore.Relational 10.0.9`

MSBuild elige 10.0.4 como "principal" (referencia directa a través de una cadena
de assemblies en caché), ignorando la versión más nueva transitiva. El comportamiento
runtime es correcto (los tests pasan), pero el warning indica que los tests corren
contra 10.0.4 mientras la Api y la Infrastructure usan 10.0.9.

### Fix

Un solo cambio en `Lloka.IntegrationTests.csproj`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.9" />
```

Esto fuerza a 10.0.9 explícitamente como referencia directa, eliminando la
ambigüedad. El warning MSB3277 desaparece.

### Qué NO se toca

No hay cambio de comportamiento en los tests existentes. El cambio es sólo de
versión de dependencia — los tests seguirán pasando.

---

## ORDEN DE EJECUCIÓN Y CHECKPOINTS

| Paso | Tarea | Checkpoint post-tarea |
|------|-------|----------------------|
| 1 | Fix versión EF (Punto 5) | `dotnet build` → 0 warnings MSB3277 |
| 2 | `GetUserBookings` (Punto 1) | `dotnet build` + `dotnet test` → 60/60 verdes |
| 3 | `GetKycStatus` (Punto 2) | `dotnet build` + `dotnet test` → 60/60 verdes |
| 4 | Tests unitarios (Punto 3) | `dotnet test` → verde con nuevos tests (target: 60 + ~12 = ~72) |
| 5 | Test integración StayPeriod (Punto 4) | `dotnet test` → REPORTAR resultado exacto antes de arreglar si falla |

El Punto 5 (fix EF) va primero porque es trivial y elimina ruido del output del
build desde el inicio. Los Puntos 1 y 2 van antes de los tests unitarios porque
los tests del Punto 3 que cubren los nuevos handlers necesitan que los handlers
existan primero.

---

## SUPUESTOS EXPLÍCITOS

1. `WishlistItem.AssignToUser(Guid userId)` existe en el entity. Lo confirmaré
   leyendo el archivo antes de escribir `MergeAnonymousSessionCommandHandlerTests`.
2. `CreatePropertyCommandValidator` puede o no validar coordenadas — el test de
   `DomainException` para coordenadas inválidas es válido en el unit test del handler
   independientemente.
3. El `AnonymousSessionId` en `CreateBookingCommand` tiene default `null` — confirmado
   en el archivo existente.

## QUÉ NO SE TOCA EN ESTE BLOQUE

- RabbitMQ / OutboxPublisher
- Laravel notifications-service
- Frontend React
- `GetOwnerPerformance` / `ExportBookingsToExcel`
- Entidades de Domain (no se agregan navigations)
- `Program.cs` / DI (salvo los nuevos handlers que se registran automáticamente por
  convención vía `MediatR.Extensions`)
