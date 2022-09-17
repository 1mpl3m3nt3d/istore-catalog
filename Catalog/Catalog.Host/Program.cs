using Catalog.Host.Configurations;
using Catalog.Host.Data;
using Catalog.Host.Repositories;
using Catalog.Host.Repositories.Interfaces;
using Catalog.Host.Services;
using Catalog.Host.Services.Interfaces;

var configuration = GetConfiguration();

var builder = WebApplication.CreateBuilder(args);

var connectionString = GetConnectionString(builder);

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

    var authority = configuration["Authorization:Authority"];

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

builder.AddConfiguration();

builder.Services.Configure<CatalogConfig>(builder.Configuration.GetSection(CatalogConfig.Catalog));
// builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(DatabaseConfig.Database));

builder.Services.AddAuthorization(configuration);

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddTransient<ICatalogBrandRepository, CatalogBrandRepository>();
builder.Services.AddTransient<ICatalogItemRepository, CatalogItemRepository>();
builder.Services.AddTransient<ICatalogTypeRepository, CatalogTypeRepository>();

builder.Services.AddTransient<ICatalogService, CatalogService>();

builder.Services.AddTransient<ICatalogBrandService, CatalogBrandService>();
builder.Services.AddTransient<ICatalogItemService, CatalogItemService>();
builder.Services.AddTransient<ICatalogTypeService, CatalogTypeService>();

builder.Services.AddDbContextFactory<ApplicationDbContext>(
    opts => opts.UseNpgsql(connectionString));

builder.Services.AddScoped<IDbContextWrapper<ApplicationDbContext>, DbContextWrapper<ApplicationDbContext>>();

builder.Services.AddCors(
    options => options
    .AddPolicy(
        "CorsPolicy",
        builder => builder
            .SetIsOriginAllowed((host) => true)
            .WithOrigins(configuration["SpaUrl"], configuration["PathBase"], configuration["GlobalUrl"], configuration["Authorization:Authority"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

if (configuration["Nginx:UseNginx"] == "true" || Environment.GetEnvironmentVariable("Nginx__UseNginx") == "true")
{
    try
    {
        if (configuration["Nginx:UseInitFile"] == "true" || Environment.GetEnvironmentVariable("Nginx__UseInitFile") == "true")
        {
            var initFile = configuration["Nginx:InitFilePath"] ?? Environment.GetEnvironmentVariable("Nginx__InitFilePath") ?? "/tmp/app-initialized";

            if (!File.Exists(initFile))
            {
                File.Create(initFile).Close();
            }

            File.SetLastWriteTimeUtc(initFile, DateTime.UtcNow);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Environment variable <Nginx__UseNginx> is set to 'true', but there was an exception while configuring Initialize File:\n{ex.Message}");
    }

    try
    {
        if (configuration["Nginx:UseUnixSocket"] == "true" || Environment.GetEnvironmentVariable("Nginx__UseUnixSocket") == "true")
        {
            var unixSocket = configuration["Nginx:UnixSocketPath"] ?? Environment.GetEnvironmentVariable("Nginx__UnixSocketPath") ?? "/tmp/nginx.socket";

            builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenUnixSocket(unixSocket));
        }
        else
        {
            var portParsed = int.TryParse(configuration["PORT"] ?? Environment.GetEnvironmentVariable("PORT"), out var port);

            if (portParsed)
            {
                builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenAnyIP(port));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Environment variable <Nginx__UseNginx> is set to 'true', but there was an exception while configuring Kestrel:\n{ex.Message}");
    }
}
else
{
    var portEnv = configuration["PORT"] ?? Environment.GetEnvironmentVariable("PORT");

    try
    {
        if (portEnv != null)
        {
            var portParsed = int.TryParse(portEnv, out var port);

            if (portParsed)
            {
                builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenAnyIP(port));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Environment variable <PORT> is set to '{portEnv}', but there was an exception while configuring Kestrel:\n{ex.Message}");
    }
}

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI(setup =>
    {
        setup.SwaggerEndpoint($"{configuration["PathBase"]}/swagger/v1/swagger.json", "Catalog.API V1");
        setup.OAuthClientId("catalogswaggerui");
        setup.OAuthAppName("Catalog Swagger UI");
    });

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapControllers();
});

InitializeDB(app);

app.Run();

IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);

    return builder.Build();
}

string GetConnectionString(WebApplicationBuilder builder)
{
    var envVarDb = builder.Configuration["Database:EnvVar"];

    var connectionString = envVarDb != null
        ? Environment.GetEnvironmentVariable(envVarDb) ?? builder.Configuration["Database:ConnectionString"]
        : builder.Configuration["Database:ConnectionString"];

    if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
    {
        var uri = new Uri(connectionString);

        var host = uri.Host;
        var port = uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':', 2);
        var uid = userInfo[0];
        var password = userInfo[1];

        var keyValueConnectionString = $"server={host};port={port};database={database};uid={uid};password={password};sslmode=require;Trust Server Certificate=true;";

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
        {
            keyValueConnectionString += "Include Error Detail=true;";
        }

        return keyValueConnectionString;
    }
    else
    {
        return connectionString;
    }
}

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
            logger.LogError(ex, $"An error occurred while creating the Database!\n[Ex: {ex.Message}]\n[InnerEx: {ex.InnerException?.Message}]");
        }
    }
}
