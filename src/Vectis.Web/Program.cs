using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Vectis.Domain;
using Vectis.Web.Data;
using Vectis.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<VectisEngine>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddDbContextFactory<VectisDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Vectis")
        ?? throw new InvalidOperationException("ConnectionStrings:Vectis est manquant.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddSingleton<IAppStore, EfAppStore>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PumpingService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<BottleService>();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<FamilyService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<InvitationEmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "Vectis.Session";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app.Services);
await SeedDemoDataAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (IDbContextFactory<VectisDbContext> dbFactory) =>
{
    try
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var canConnect = await db.Database.CanConnectAsync();

        return canConnect
            ? Results.Ok(new { status = "ready", database = "ok" })
            : Results.Problem("La base de donnees n'est pas joignable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Problem("La verification de readiness a echoue.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task SeedDemoDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<IAppStore>();
    var engine = scope.ServiceProvider.GetRequiredService<VectisEngine>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    await store.MutateAsync(state => engine.SeedDemo(state, hasher.Hash("Demo123!")));
}

static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VectisDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
