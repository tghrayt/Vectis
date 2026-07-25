using Microsoft.AspNetCore.Authentication.Cookies;
using Vectis.Domain;
using Vectis.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<VectisEngine>();
builder.Services.AddSingleton<JsonAppStore>();
builder.Services.AddSingleton<PasswordHasher>();
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
    var store = scope.ServiceProvider.GetRequiredService<JsonAppStore>();
    var engine = scope.ServiceProvider.GetRequiredService<VectisEngine>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    await store.MutateAsync(state => engine.SeedDemo(state, hasher.Hash("Demo123!")));
}
