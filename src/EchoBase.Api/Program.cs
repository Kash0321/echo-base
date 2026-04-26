using EchoBase.Api.Endpoints;
using EchoBase.Api.Services;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// ─── Autenticación ────────────────────────────────────────────────────────────
// En desarrollo se usa un handler que auto-autentica sin Azure AD.
// En producción se valida el Bearer JWT emitido por Azure AD.
if (builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Authentication:UseDevelopmentStub", false))
{
    builder.Services
        .AddAuthentication(DevApiAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevApiAuthHandler>(DevApiAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services
        .AddAuthentication()
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddAuthorization();

// ─── HttpContextAccessor (necesario para ApiCurrentUserService) ───────────────
builder.Services.AddHttpContextAccessor();

// ─── Servicios de usuario ─────────────────────────────────────────────────────
builder.Services.AddScoped<ApiCurrentUserService>();
builder.Services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<ApiCurrentUserService>());

// ─── Infrastructure ───────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("EchoBase")
    ?? throw new InvalidOperationException("Connection string 'EchoBase' not found.");
var useSqlite = builder.Configuration.GetValue("Database:UseSqlite", defaultValue: true);

builder.Services.AddEchoBaseDatabase(connectionString, useSqlite);
builder.Services.AddEchoBaseServices();
builder.Services.AddEchoBaseNotifications(builder.Configuration);

// ─── OpenAPI / Swagger ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "EchoBase API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce el token JWT Bearer. En desarrollo con stub no es necesario."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

var app = builder.Build();

// ─── Pipeline HTTP ────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EchoBase API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapReservationsEndpoints();
app.MapIncidencesEndpoints();
app.MapUsersEndpoints();
app.MapBlockedDocksEndpoints();

// ─── Inicialización de la base de datos ──────────────────────────────────────
await DatabaseInitializer.InitializeAsync(app.Services, app.Environment.IsDevelopment());

app.Run();
