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
builder.Services.AddDbContextFactory<VectisDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Vectis")
        ?? throw new InvalidOperationException("ConnectionStrings:Vectis est manquant.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddSingleton<IAppStore, EfAppStore>();
builder.Services.AddScoped<CurrentUser>();
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

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
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
