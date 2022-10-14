using Catalog.Host.Configurations;
using Catalog.Host.Data;
using Catalog.Host.Repositories;
using Catalog.Host.Repositories.Interfaces;
using Catalog.Host.Services;
using Catalog.Host.Services.Interfaces;

using Infrastructure.Configuration;

using Microsoft.AspNetCore.HttpOverrides;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

var webApplicationOptions = new WebApplicationOptions()
{
    ContentRootPath = baseDirectory,
};

var builder = WebApplication.CreateBuilder(webApplicationOptions);

builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.Sources.Clear();

    var env = hostingContext.HostingEnvironment;

    config.SetBasePath(baseDirectory);

    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    config.AddEnvironmentVariables();

    if (args != null)
    {
        config.AddCommandLine(args);
    }
});

builder.AddConfiguration();

// 1st variant how to get desired configuration for WebApplicationBuilder in Program.cs
//var appConfig = new AppConfig();
//builder.Configuration.GetSection(AppConfig.App).Bind(appConfig);

// 2nd variant how to get desired configuration for WebApplicationBuilder in Program.cs
var appConfig = builder.Configuration.GetSection(AppConfig.App).Get<AppConfig>();
var authConfig = builder.Configuration.GetSection(AuthorizationConfig.Authorization).Get<AuthorizationConfig>();

builder.AddHttpLoggingConfiguration();
builder.AddNginxConfiguration();

builder.Services.Configure<CatalogConfig>(builder.Configuration.GetSection(CatalogConfig.Catalog));
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(DatabaseConfig.Database));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;
    options.RequireHeaderSymmetry = false;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCertificateForwarding(options => { });

builder.Services.AddHsts(options =>
{
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
    options.Preload = true;
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;

    var isPortParsed = int.TryParse(builder.Configuration["HTTPS_PORT"], out var httpsPort);

    if (isPortParsed)
    {
        options.HttpsPort = httpsPort;
    }
});

builder.Services.AddCookiePolicy(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = false;
    options.Cookie.Expiration = TimeSpan.FromDays(30);
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = false;
    options.Cookie.Expiration = TimeSpan.FromDays(30);
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.AddCors(
    options => options
        .AddPolicy(
            "CorsPolicy",
            corsBuilder => corsBuilder
                //.SetIsOriginAllowed((host) => true)
                .WithOrigins(
                    authConfig.Authority,
                    appConfig.BaseUrl,
                    appConfig.GlobalUrl,
                    appConfig.SpaUrl)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()));

builder.Services
    .AddControllers(options => options.Filters.Add(typeof(HttpGlobalExceptionFilter)))
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "eShop - Catalog HTTP API",
        Version = "v1",
        Description = "The Catalog Service HTTP API",
    });

    var authority = authConfig.Authority;

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows()
        {
            Implicit = new OpenApiOAuthFlow()
            {
                AuthorizationUrl = new Uri($"{authority}/connect/authorize"),
                TokenUrl = new Uri($"{authority}/connect/token"),
                Scopes = new Dictionary<string, string>()
                {
                    { "catalog", "catalog" },
                    { "catalog.bff", "catalog.bff" },
                },
            },
        },
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

builder.AddAuthorization();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddTransient<ICatalogRepository, CatalogRepository>();
builder.Services.AddTransient<ICatalogBrandRepository, CatalogBrandRepository>();
builder.Services.AddTransient<ICatalogItemRepository, CatalogItemRepository>();
builder.Services.AddTransient<ICatalogTypeRepository, CatalogTypeRepository>();

builder.Services.AddTransient<ICatalogService, CatalogService>();
builder.Services.AddTransient<ICatalogBrandService, CatalogBrandService>();
builder.Services.AddTransient<ICatalogItemService, CatalogItemService>();
builder.Services.AddTransient<ICatalogTypeService, CatalogTypeService>();

builder.Services.AddDbContextFactory<ApplicationDbContext>();

builder.Services.AddScoped<IDbContextWrapper<ApplicationDbContext>, DbContextWrapper<ApplicationDbContext>>();

var app = builder.Build();

// a variant how to get desired configuration for WebApplication in Program.cs
var webAppConfig = app.Services.GetRequiredService<IOptionsMonitor<AppConfig>>().CurrentValue;

var basePath = webAppConfig.BasePath;

if (!string.IsNullOrEmpty(basePath))
{
    app.UsePathBase(basePath);
}

if (webAppConfig.HttpLogging == "true")
{
    app.UseHttpLogging();

    app.Use(async (ctx, next) =>
    {
        var remoteAddress = ctx.Connection.RemoteIpAddress;
        var remotePort = ctx.Connection.RemotePort;

        app.Logger.LogInformation($"Request Remote: {remoteAddress}:{remotePort}");

        await next(ctx);
    });
}

var forwardedHeadersOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedProto,
    ForwardLimit = 2,
    RequireHeaderSymmetry = false,
};

forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCertificateForwarding();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days.
    // see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

//app.UseDefaultFiles();
//app.UseStaticFiles();

var cookiePolicyOptions = new CookiePolicyOptions()
{
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None,
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.SameAsRequest,
};

app.UseCookiePolicy(cookiePolicyOptions);

app.UseSwagger()
    .UseSwaggerUI(setup =>
    {
        setup.SwaggerEndpoint("v1/swagger.json", "Catalog.API V1");
        setup.OAuthClientId("catalogswaggerui");
        setup.OAuthAppName("Catalog Swagger UI");
    });

app.UseRouting();

//app.UseRequestLocalization();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

//app.UseSession();
//app.UseResponseCompression();
//app.UseResponseCaching();

//app.MapControllers();
//app.MapDefaultControllerRoute();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapDefaultControllerRoute();
});

InitializeDB(app);

app.Run();

void InitializeDB(IHost host)
{
    using (var scope = host.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            DbInitializer.Initialize(context, logger).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error occurred while creating the Database!\n" +
                $"[Ex: {ex.Message}]\n[InnerEx: {ex.InnerException?.Message}]");
        }
    }
}
