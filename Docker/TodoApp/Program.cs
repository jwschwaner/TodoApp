using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Components;
using TodoApp.Components.Account;
using TodoApp.Data;
using TodoApp.TodoData;
using TodoApp.TodoData.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with HTTPS certificate - let ASPNETCORE_URLS handle port binding
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Get certificate configuration from environment variables
    var certPath = builder.Configuration["Kestrel:Certificates:Default:Path"];
    var certPassword = builder.Configuration["Kestrel:Certificates:Default:Password"];

    if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
    {
        var resolvedPath = ResolveCertificatePath(certPath);

        Console.WriteLine($"Certificate path from config: {certPath}");
        Console.WriteLine($"Resolved certificate path: {resolvedPath}");
        Console.WriteLine($"Certificate exists: {File.Exists(resolvedPath)}");

        if (File.Exists(resolvedPath))
        {
            try
            {
                var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(resolvedPath, certPassword);

                serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.ServerCertificate = certificate;
                });

                Console.WriteLine("HTTPS configured successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load certificate: {ex.Message}");
            }
        }
    }
});

// Helper method for cross-platform certificate path resolution
static string ResolveCertificatePath(string certPath)
{
    Console.WriteLine($"Input path: '{certPath}'");

    // Handle tilde (~) expansion on Unix-like systems (macOS/Linux)
    if (certPath.StartsWith("~/"))
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var resolvedPath = Path.Combine(homeDirectory, certPath.Substring(2));
        Console.WriteLine($"Tilde expansion: {homeDirectory} + {certPath.Substring(2)} = {resolvedPath}");
        return resolvedPath;
    }

    // Handle Windows environment variables like %USERPROFILE%
    if (certPath.Contains('%'))
    {
        var expandedPath = Environment.ExpandEnvironmentVariables(certPath);
        Console.WriteLine($"Environment expansion: {certPath} -> {expandedPath}");
        return expandedPath;
    }

    // Handle relative paths - make them absolute relative to user profile
    if (!Path.IsPathRooted(certPath))
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var resolvedPath = Path.Combine(homeDirectory, certPath);
        Console.WriteLine($"Relative path resolution: {homeDirectory} + {certPath} = {resolvedPath}");
        return resolvedPath;
    }

    // Return absolute path as-is
    Console.WriteLine($"Absolute path returned as-is: {certPath}");
    return certPath;
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var connectionString = builder.Configuration.GetConnectionString("IdentityConnection") ??
                       throw new InvalidOperationException("Connection string 'IdentityConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure TodoDbContext
var todoConnectionString = builder.Configuration.GetConnectionString("TodoConnection") ??
                          throw new InvalidOperationException("Connection string 'TodoConnection' not found.");
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(todoConnectionString));

// Register CprService
builder.Services.AddScoped<CprService>();
// Register TodoService
builder.Services.AddScoped<TodoService>();

// Configure Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.Initialize(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnet/aspnetcore-hsts.
    app.UseHsts();
}

// Force HTTPS redirection
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();