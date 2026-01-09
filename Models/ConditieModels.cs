using System.Text.Json.Serialization;

namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Complete conditional configuration for a placeholder
    /// </summary>
    public class ConditieConfig
    {
        [JsonPropertyName("regels")]
        public List<ConditieRegel> Regels { get; set; } = new();

        [JsonPropertyName("default")]
        public string Default { get; set; } = string.Empty;
    }

    /// <summary>
    /// A single conditional rule
    /// </summary>
    public class ConditieRegel
    {
        [JsonPropertyName("conditie")]
        public Conditie Conditie { get; set; } = new ConditieVoorwaarde();

        [JsonPropertyName("resultaat")]
        public string Resultaat { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base class for conditions - can be either a simple comparison or a group
    /// </summary>
    [JsonDerivedType(typeof(ConditieVoorwaarde))]
    [JsonDerivedType(typeof(ConditieGroep))]
    public abstract class Conditie
    {
    }

    /// <summary>
    /// Simple condition (single field comparison)
    /// </summary>
    public class ConditieVoorwaarde : Conditie
    {
        [JsonPropertyName("veld")]
        public string Veld { get; set; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "=";

        [JsonPropertyName("waarde")]
        public object? Waarde { get; set; }
    }

    /// <summary>
    /// Group condition (AND/OR)
    /// </summary>
    public class ConditieGroep : Conditie
    {
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "AND"; // "AND" or "OR"

        [JsonPropertyName("voorwaarden")]
        public List<Conditie> Voorwaarden { get; set; } = new();
    }

    /// <summary>
    /// Custom placeholder from the catalogus with optional conditional logic
    /// </summary>
    public class CustomPlaceholderData
    {
        public int Id { get; set; }
        public string PlaceholderKey { get; set; } = string.Empty;
        public string? Waarde { get; set; }
        public string? StandaardWaarde { get; set; }
        public bool HeeftConditie { get; set; }
        public string? ConditieConfigJson { get; set; }
    }
}
