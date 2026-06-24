using System.Text;
using FluentValidation;
using Lloka.Api.Middleware;
using Lloka.Application.Common.Behaviors;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Infrastructure.Persistence;
using Lloka.Infrastructure.Persistence.Repositories;
using Lloka.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- OpenAPI ---
builder.Services.AddOpenApi();

// --- Controllers ---
builder.Services.AddControllers();

// --- Database ---
builder.Services.AddDbContext<LlokaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- MediatR + FluentValidation pipeline ---
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(IUnitOfWork).Assembly);

// --- Repositories ---
builder.Services.AddScoped<IBookingRepository,  BookingRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IUserRepository,     UserRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IRepository<OutboxMessage>, Repository<OutboxMessage>>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// --- Auth + KYC services ---
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IGroqKycService, MockGroqKycService>();
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// --- JWT authentication middleware ---
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
        };
    });
builder.Services.AddAuthorization();

// -----------------------------------------------------------------------
var app = builder.Build();

// Auto-migrate: útil en Development o cuando la variable de entorno lo indica.
// Permite que el contenedor Docker quede listo sin correr dotnet ef manualmente.
var applyMigrations = app.Environment.IsDevelopment()
    || string.Equals(app.Configuration["APPLY_MIGRATIONS_ON_START"], "true",
                     StringComparison.OrdinalIgnoreCase);

if (applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LlokaDbContext>();
    try
    {
        db.Database.Migrate();
        app.Logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error applying migrations on startup.");
    }
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.WithTitle("Lloka API");
        opts.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        opts.AddPreferredSecuritySchemes("Bearer");
    });
}

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
