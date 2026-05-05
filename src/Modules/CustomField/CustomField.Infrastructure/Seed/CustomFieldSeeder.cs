using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CfDomain = CustomField.Domain;

namespace CustomField.Infrastructure.Seed;

/// <summary>
/// Seed định nghĩa field demo (không gắn context global). Context theo project do
/// DemoCustomFieldProjectBinder + domain event ProjectCreated, và backfill startup.
/// </summary>
public static class CustomFieldSeeder
{
    public const string AcceptanceCriteriaKey = "acceptance_criteria";
    public const string RiskLevelKey = "risk_level";
    public const string StoryPointsKey = "cf_story_points";
    public const string TargetDateKey = "cf_target_date";
    public const string ComponentsKey = "cf_components";
    public const string MandaysKey = "mandays";

    public static async Task SeedDefaultsAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CustomFieldDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CustomFieldSeeder");

        if (!await ctx.Fields.AnyAsync(f => f.Key == AcceptanceCriteriaKey, ct))
        {
            var text = new CfDomain.CustomField(AcceptanceCriteriaKey, "Acceptance criteria", CfDomain.CustomFieldType.Text,
                "Conditions that must be met to close this issue", isSearchable: true);
            ctx.Fields.Add(text);
            logger.LogInformation("Seeding custom field {Key}", AcceptanceCriteriaKey);
        }

        if (!await ctx.Fields.AnyAsync(f => f.Key == RiskLevelKey, ct))
        {
            var risk = new CfDomain.CustomField(RiskLevelKey, "Risk level", CfDomain.CustomFieldType.Select,
                description: null, isSearchable: true);
            risk.AddOption("low", "Low");
            risk.AddOption("med", "Medium");
            risk.AddOption("high", "High");
            ctx.Fields.Add(risk);
            logger.LogInformation("Seeding custom field {Key}", RiskLevelKey);
        }

        if (!await ctx.Fields.AnyAsync(f => f.Key == StoryPointsKey, ct))
        {
            var pts = new CfDomain.CustomField(StoryPointsKey, "Story points (CF)", CfDomain.CustomFieldType.Number,
                "Numeric estimate stored as custom field", isSearchable: true);
            ctx.Fields.Add(pts);
            logger.LogInformation("Seeding custom field {Key}", StoryPointsKey);
        }

        if (!await ctx.Fields.AnyAsync(f => f.Key == TargetDateKey, ct))
        {
            var dt = new CfDomain.CustomField(TargetDateKey, "Target date", CfDomain.CustomFieldType.Date,
                description: null, isSearchable: true);
            ctx.Fields.Add(dt);
            logger.LogInformation("Seeding custom field {Key}", TargetDateKey);
        }

        if (!await ctx.Fields.AnyAsync(f => f.Key == ComponentsKey, ct))
        {
            var comp = new CfDomain.CustomField(ComponentsKey, "Components", CfDomain.CustomFieldType.MultiSelect,
                description: null, isSearchable: true);
            comp.AddOption("fe", "Frontend");
            comp.AddOption("api", "API");
            comp.AddOption("db", "Database");
            ctx.Fields.Add(comp);
            logger.LogInformation("Seeding custom field {Key}", ComponentsKey);
        }

        if (!await ctx.Fields.AnyAsync(f => f.Key == MandaysKey, ct))
        {
            var md = new CfDomain.CustomField(MandaysKey, "Mandays", CfDomain.CustomFieldType.Decimal,
                description: "Effort in mandays (decimal)", isSearchable: true);
            ctx.Fields.Add(md);
            logger.LogInformation("Seeding custom field {Key}", MandaysKey);
        }

        await ctx.SaveChangesAsync(ct);
    }
}
