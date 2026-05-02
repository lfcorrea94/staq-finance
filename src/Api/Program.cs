using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using StaqFinance.Api.Authorization;
using StaqFinance.Api.Middleware;
using StaqFinance.Api.Persistence;
using StaqFinance.Modules.Accounts.Infrastructure.Extensions;
using StaqFinance.Modules.Categories.Infrastructure.Extensions;
using StaqFinance.Modules.Identity.Domain.Entities;
using StaqFinance.Modules.Identity.Infrastructure.Extensions;
using StaqFinance.Modules.Tenancy.Infrastructure.Extensions;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting StaqFinance API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "StaqFinance.Api")
            .WriteTo.Console());

    // EF Core — InMemory for Testing, PostgreSQL otherwise
    if (builder.Environment.IsEnvironment("Testing"))
    {
        var testDbName = builder.Configuration["TestDbName"] ?? "TestDb";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(testDbName)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
    }
    else
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
    }

    // DbContext abstraction for repositories
    builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

    // ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    // Identity module (JWT)
    builder.Services.AddIdentityModule(builder.Configuration);

    // Tenancy module
    builder.Services.AddTenancyModule();

    // Accounts module
    builder.Services.AddAccountsModule();

    // Categories module
    builder.Services.AddCategoriesModule();

    // Authorization
    builder.Services.AddScoped<IAuthorizationHandler, MustBelongToTenantHandler>();
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("MustBelongToTenant", policy =>
            policy
                .RequireAuthenticatedUser()
                .AddRequirements(new MustBelongToTenantRequirement()));

    // Controllers
    builder.Services.AddControllers();

    // Swagger / OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "StaqFinance API",
            Version = "v1",
            Description = "API de controle de gastos pessoais com suporte a múltiplos workspaces."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Informe: Bearer {seu token JWT}"
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "StaqFinance API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseAuthentication();

    app.UseMiddleware<TenantResolutionMiddleware>();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/api/health/db");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
