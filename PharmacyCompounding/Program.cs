using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PharmacyCompounding.Data;
using PharmacyCompounding.Models;

// DateTime without a time zone is stored in PostgreSQL as timestamp without time zone
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Load variables from the .env file in the repository root (database name, user, password).
// Environment variables that are already set take precedence over the file.
DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Output Cyrillic in HTML as is, without encoding it as &#x...;
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

// Register the data context (PostgreSQL): host and port come from appsettings.json, credentials from .env.
// Table and column names are snake_case: Orders.CreatedAt → orders.created_at
// The interceptors record the executed SQL and transaction boundaries to show them on the report pages.
var connection = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("PharmacyDb"))
{
    Database = Required("POSTGRES_DB"),
    Username = Required("POSTGRES_USER"),
    Password = Required("POSTGRES_PASSWORD")
}.ConnectionString;

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SqlCaptureInterceptor>();
builder.Services.AddSingleton<TransactionCaptureInterceptor>();
builder.Services.AddDbContext<PharmacyContext>((services, options) =>
    options.UseNpgsql(connection)
           .UseSnakeCaseNamingConvention()
           .AddInterceptors(services.GetRequiredService<SqlCaptureInterceptor>(),
                            services.GetRequiredService<TransactionCaptureInterceptor>()));

// Cookie authentication: anonymous users are redirected to the sign-in page
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
    });

// By default every page is available to pharmacy staff only;
// exceptions are marked with [AllowAnonymous] or [Authorize(Roles = ...)]
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireRole(Roles.Pharmacist, Roles.Technologist, Roles.Manager)
        .Build());

var app = builder.Build();

// Ukrainian locale (dates, hryvnia) with a dot as the decimal separator, as in <input type="number"> fields
var culture = (CultureInfo)CultureInfo.GetCultureInfo("uk-UA").Clone();
culture.NumberFormat.NumberDecimalSeparator = ".";
culture.NumberFormat.CurrencyDecimalSeparator = ".";
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Code First: apply migrations (create the database and tables) and seed test data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PharmacyContext>();
    db.Database.Migrate();
    DbInitializer.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Default route: /{controller}/{action}/{id?}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

string Required(string name) => builder.Configuration[name]
    ?? throw new InvalidOperationException($"{name} is not set: copy .env.example to .env");
