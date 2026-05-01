using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BB.Persistence.Json;

/// <summary>
/// Trừu tượng hoá cột JSON: Postgres -> jsonb, Oracle -> CLOB IS JSON.
/// Domain entity chỉ thấy <c>string</c> JSON, không biết provider thật.
/// </summary>
public interface IJsonColumnConfigurator
{
    /// <summary>Cấu hình property kiểu string là cột JSON theo provider hiện tại.</summary>
    void Configure<TEntity, TProperty>(
        PropertyBuilder<TProperty> property,
        DbProvider provider) where TEntity : class;
}

public sealed class JsonColumnConfigurator : IJsonColumnConfigurator
{
    public void Configure<TEntity, TProperty>(PropertyBuilder<TProperty> property, DbProvider provider) where TEntity : class
    {
        switch (provider)
        {
            case DbProvider.Postgres:
                property.HasColumnType("jsonb");
                break;
            case DbProvider.Oracle:
                // Oracle 21c+ hỗ trợ kiểu JSON native; phiên bản cũ dùng CLOB + check IS JSON.
                property.HasColumnType("CLOB");
                break;
        }
    }
}

public static class JsonColumnExtensions
{
    public static PropertyBuilder<string> HasJsonColumn(
        this PropertyBuilder<string> property,
        DbProvider provider)
    {
        new JsonColumnConfigurator().Configure<object, string>(property, provider);
        return property;
    }

    /// <summary>Map property kiểu list/object qua JSON serialization.</summary>
    public static PropertyBuilder<TProperty> HasJsonConversion<TEntity, TProperty>(
        this PropertyBuilder<TProperty> property,
        DbProvider provider)
        where TProperty : class, new()
    {
        var converter = new ValueConverter<TProperty, string>(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null)!,
            v => System.Text.Json.JsonSerializer.Deserialize<TProperty>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new TProperty());

        property.HasConversion(converter);
        switch (provider)
        {
            case DbProvider.Postgres: property.HasColumnType("jsonb"); break;
            case DbProvider.Oracle: property.HasColumnType("CLOB"); break;
        }
        return property;
    }
}
