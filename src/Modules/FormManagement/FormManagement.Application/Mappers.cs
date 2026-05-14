using System.Text.Json;
using FormManagement.Domain;

namespace FormManagement.Application;

internal static class Mappers
{
    public static MetadataDto ToDto(MetadataDef m) =>
        new(m.Id, m.Value, m.Label, m.Type, m.FieldGroup, m.Description, m.ValidationJson, m.CreatedAt);

    public static TemplateSummaryDto ToSummaryDto(DocumentTemplate t) =>
        new(t.Id, t.Code, t.Name, t.Category, t.Version, t.Status,
            ParseUsedFields(t.UsedFieldsJson).Count,
            t.CreatedAt, t.UpdatedAt);

    public static TemplateDetailDto ToDetailDto(DocumentTemplate t) =>
        new(t.Id, t.Code, t.Name, t.Category,
            ParseUsedFields(t.UsedFieldsJson),
            t.Version, t.Status,
            HasOriginalDocx: t.DocxBytes is { Length: > 0 },
            t.CreatedAt, t.UpdatedAt);

    public static SubmissionDto ToDto(Submission s) =>
        new(s.Id, s.TemplateId, s.TemplateVersion, s.ExportFormat, s.OutputPath, s.CreatedAt, s.CreatedBy);

    public static IReadOnlyList<string> ParseUsedFields(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            // Defensive: corrupt persisted JSON shouldn't crash listing. Returning empty signals "unknown fields".
            return Array.Empty<string>();
        }
    }

    public static string SerializeUsedFields(IEnumerable<string> values) =>
        JsonSerializer.Serialize(values.ToArray());
}
