using EchoBase.Infrastructure;
using EchoBase.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("EchoBase")
    ?? throw new InvalidOperationException("Connection string 'EchoBase' not found.");

var useSqlite = builder.Configuration.GetValue("Database:UseSqlite", defaultValue: true);

builder.Services.AddEchoBaseDatabase(connectionString, useSqlite);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Aplicar migraciones pendientes e inicializar datos maestros al arrancar
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();
