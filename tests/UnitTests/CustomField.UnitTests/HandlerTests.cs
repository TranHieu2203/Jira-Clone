using System.Text.Json;
using CustomField.Application.Handlers;
using CustomField.Domain;
using FluentAssertions;

namespace CustomField.UnitTests;

public class HandlerTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task Text_RejectsNonString()
    {
        var def = new Domain.CustomField("text_a", "T", CustomFieldType.Text);
        var h = new TextHandler();

        var ok = await h.ValidateAsync(def, Parse("\"hello\""));
        ok.IsSuccess.Should().BeTrue();

        var fail = await h.ValidateAsync(def, Parse("123"));
        fail.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Number_RejectsString()
    {
        var def = new Domain.CustomField("num_a", "N", CustomFieldType.Number);
        var h = new NumberHandler();

        (await h.ValidateAsync(def, Parse("42"))).IsSuccess.Should().BeTrue();
        (await h.ValidateAsync(def, Parse("\"42\""))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Url_ValidatesAbsoluteUri()
    {
        var def = new Domain.CustomField("url_a", "U", CustomFieldType.Url);
        var h = new UrlHandler();

        (await h.ValidateAsync(def, Parse("\"https://example.com\""))).IsSuccess.Should().BeTrue();
        (await h.ValidateAsync(def, Parse("\"not-a-url\""))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Select_AcceptsKnownOptionValue()
    {
        var def = new Domain.CustomField("priority", "Priority", CustomFieldType.Select);
        def.AddOption("HIGH", "High");
        def.AddOption("LOW", "Low");
        var h = new SelectHandler();

        (await h.ValidateAsync(def, Parse("\"HIGH\""))).IsSuccess.Should().BeTrue();
        (await h.ValidateAsync(def, Parse("\"UNKNOWN\""))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task MultiSelect_RejectsNonArray()
    {
        var def = new Domain.CustomField("labels_x", "L", CustomFieldType.MultiSelect);
        def.AddOption("a", "A");
        var h = new MultiSelectHandler();

        (await h.ValidateAsync(def, Parse("[\"a\"]"))).IsSuccess.Should().BeTrue();
        (await h.ValidateAsync(def, Parse("\"a\""))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task User_ValidatesGuid()
    {
        var def = new Domain.CustomField("owner_x", "O", CustomFieldType.User);
        var h = new UserHandler();

        (await h.ValidateAsync(def, Parse($"\"{Guid.NewGuid()}\""))).IsSuccess.Should().BeTrue();
        (await h.ValidateAsync(def, Parse("\"not-a-guid\""))).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Number_IndexExtractsNumeric()
    {
        var h = new NumberHandler();
        var (s, n, d) = h.Index(Parse("42"));
        n.Should().Be(42m);
        s.Should().BeNull();
        d.Should().BeNull();
    }

    [Fact]
    public void MultiSelect_IndexJoinsSorted()
    {
        var h = new MultiSelectHandler();
        var (s, _, _) = h.Index(Parse("[\"b\", \"a\", \"c\"]"));
        s.Should().Be("a,b,c");
    }

    [Fact]
    public void Registry_FindsHandlerByType()
    {
        var registry = new CustomFieldTypeHandlerRegistry(new ICustomFieldTypeHandler[]
        {
            new TextHandler(), new NumberHandler(), new SelectHandler()
        });

        registry.Find(CustomFieldType.Text).Should().NotBeNull();
        registry.Find(CustomFieldType.Number).Should().NotBeNull();
        registry.Find(CustomFieldType.Url).Should().BeNull();
    }
}
