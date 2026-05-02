using System.Threading.RateLimiting;
using Asp.Versioning;
using BB.Localization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using BB.Logging;
using BB.Persistence;
using BB.Security;
using BB.Storage;
using BB.Web;
using Identity.Api;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi.Models;
using ActivityLog.Api;
using Attachment.Api;
using Comment.Api;
using CustomField.Api;
using Issue.Api;
using Project.Api;
using Serilog;
using Workflow.Api;
using Api.Host.Infrastructure.Outbox;
using BB.EventBus;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseBbSerilog();

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services.AddBbWeb();
builder.Services.AddBbSecurity(builder.Configuration);

var resourcesDir = Path.Combine(AppContext.BaseDirectory, "Resources");
if (!Directory.Exists(resourcesDir))
{
    resourcesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BuildingBlocks", "BB.Localization", "Resources");
}
builder.Services.AddBbLocalization(resourcesDir);

builder.Services.AddRequestLocalization(opts =>
{
    var supported = new[] { "vi", "en" };
    opts.SetDefaultCulture("vi");
    opts.AddSupportedCultures(supported);
    opts.AddSupportedUICultures(supported);
    opts.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());
});

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddProjectModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddCustomFieldModule(builder.Configuration);
builder.Services.AddIssueModule(builder.Configuration);
builder.Services.AddCommentModule(builder.Configuration);
builder.Services.AddActivityLogModule(builder.Configuration);
builder.Services.AddBbStorage(builder.Configuration);
builder.Services.AddAttachmentModule(builder.Configuration);

// Cross-cutting cho domain events + clock (đã đăng ký 1 lần dùng cho mọi DbContext)
builder.Services.AddSingleton<BB.Common.IClock, BB.Common.SystemClock>();
builder.Services.AddSingleton<BB.Common.IGuidGenerator, BB.Common.UuidV7Generator>();
builder.Services.AddScoped<BB.Common.IDomainEventDispatcher, BB.EventBus.DomainEventDispatcher>();

builder.Services.AddControllers();

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jira-Clone API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Bearer JWT",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            }] = Array.Empty<string>()
    });
});

builder.Services.AddCors(o => o.AddPolicy("Default", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:4200" })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders(TraceIdMiddleware.HeaderName)));

builder.Services.AddRateLimiter(opt =>
{
    opt.AddPolicy("default", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseBbTraceId();
app.UseRequestLocalization();
app.UseCors("Default");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("default");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

if (args.Contains("--migrate") || builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var bootstrapLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");
    var identityDb = scope.ServiceProvider.GetRequiredService<Identity.Infrastructure.IdentityDbContext>();
    var workflowDb = scope.ServiceProvider.GetRequiredService<Workflow.Infrastructure.WorkflowDbContext>();
    var projectDb = scope.ServiceProvider.GetRequiredService<Project.Infrastructure.ProjectDbContext>();
    var customFieldDb = scope.ServiceProvider.GetRequiredService<CustomField.Infrastructure.CustomFieldDbContext>();
    var issueDb = scope.ServiceProvider.GetRequiredService<Issue.Infrastructure.IssueDbContext>();
    var commentDb = scope.ServiceProvider.GetRequiredService<Comment.Infrastructure.CommentDbContext>();
    var activityLogDb = scope.ServiceProvider.GetRequiredService<ActivityLog.Infrastructure.ActivityLogDbContext>();
    var attachmentDb = scope.ServiceProvider.GetRequiredService<Attachment.Infrastructure.AttachmentDbContext>();
    var outboxDb = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
    await EnsureSchemaAsync(identityDb, bootstrapLogger);
    await EnsureSchemaAsync(workflowDb, bootstrapLogger);
    await EnsureSchemaAsync(projectDb, bootstrapLogger);
    await EnsureSchemaAsync(customFieldDb, bootstrapLogger);
    await EnsureSchemaAsync(issueDb, bootstrapLogger);
    await EnsureSchemaAsync(commentDb, bootstrapLogger);
    await EnsureSchemaAsync(activityLogDb, bootstrapLogger);
    await EnsureSchemaAsync(attachmentDb, bootstrapLogger);
    await EnsureSchemaAsync(outboxDb, bootstrapLogger);
}

await app.Services.SeedIdentityAsync();
await Workflow.Infrastructure.Seed.WorkflowSeeder.SeedDefaultsAsync(app.Services);
await CustomField.Infrastructure.Seed.CustomFieldSeeder.SeedDefaultsAsync(app.Services);
await Api.Host.Bootstrap.CustomFieldDemoProjectBinderBackfill.RunAsync(app.Services);

app.Run();

static async Task EnsureSchemaAsync(DbContext ctx, Microsoft.Extensions.Logging.ILogger logger)
{
    var migrationsAssembly = ctx.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly>();
    if (migrationsAssembly.Migrations.Count > 0)
    {
        await ctx.Database.MigrateAsync();
        return;
    }

    // Fresh scaffold without migrations: create this context's tables.
    // EnsureCreatedAsync short-circuits when the database exists, so for multiple
    // DbContexts on a shared DB we drop down to IRelationalDatabaseCreator.
    var creator = (IRelationalDatabaseCreator)ctx.GetService<IDatabaseCreator>();
    if (!await creator.ExistsAsync())
    {
        await creator.CreateAsync();
    }
    try
    {
        await creator.CreateTablesAsync();
        logger.LogInformation("Created tables for {Context}", ctx.GetType().Name);
    }
    catch (Exception ex)
    {
        // On repeat runs the tables already exist — that's fine.
        logger.LogDebug(ex, "CreateTables skipped for {Context} (likely already exist)", ctx.GetType().Name);
    }
}

public partial class Program { }
