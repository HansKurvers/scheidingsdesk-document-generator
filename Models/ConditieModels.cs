using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Complete configuration for conditional placeholder evaluation
    /// Stored as JSON in the database (conditie_config column)
    /// </summary>
    public class ConditieConfig
    {
        [JsonPropertyName("regels")]
        public List<ConditieRegel> Regels { get; set; } = new List<ConditieRegel>();

        [JsonPropertyName("default")]
        public string Default { get; set; } = string.Empty;
    }

    /// <summary>
    /// A rule that evaluates a condition and produces a result
    /// </summary>
    public class ConditieRegel
    {
        [JsonPropertyName("conditie")]
        public Conditie Conditie { get; set; } = new Conditie();

        [JsonPropertyName("resultaat")]
        public string Resultaat { get; set; } = string.Empty;
    }

    /// <summary>
    /// A condition can be either a single comparison (voorwaarde) or a group of conditions
    /// Uses discriminated union pattern with nullable properties
    /// </summary>
    public class Conditie
    {
        // For group conditions (AND/OR)
        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("voorwaarden")]
        public List<Conditie>? Voorwaarden { get; set; }

        // For single comparison (voorwaarde)
        [JsonPropertyName("veld")]
        public string? Veld { get; set; }

        // Note: This is the comparison operator (=, !=, etc.), not the logical operator
        // In the JSON, it's stored in the "operator" field for single conditions too
        // But for groups, "operator" is "AND" or "OR"

        [JsonPropertyName("waarde")]
        public JsonElement? Waarde { get; set; }

        /// <summary>
        /// Check if this is a group condition (AND/OR)
        /// </summary>
        public bool IsGroep => Voorwaarden != null && Voorwaarden.Count > 0;

        /// <summary>
        /// Check if this is a single comparison
        /// </summary>
        public bool IsVoorwaarde => !string.IsNullOrEmpty(Veld);

        /// <summary>
        /// Get the comparison operator for single conditions
        /// (reuses the Operator property)
        /// </summary>
        public string? VergelijkingsOperator => IsVoorwaarde ? Operator : null;

        /// <summary>
        /// Get the logical operator for group conditions
        /// </summary>
        public string? LogicalOperator => IsGroep ? Operator : null;
    }

    /// <summary>
    /// Placeholder with conditional logic from the database
    /// </summary>
    public class ConditionalPlaceholder
    {
        public int Id { get; set; }
        public string PlaceholderKey { get; set; } = string.Empty;
        public bool HeeftConditie { get; set; }
        public string? ConditieConfigJson { get; set; }

        /// <summary>
        /// Parsed condition configuration (lazy loaded)
        /// </summary>
        private ConditieConfig? _conditieConfig;
        public ConditieConfig? ConditieConfig
        {
            get
            {
                if (_conditieConfig == null && !string.IsNullOrEmpty(ConditieConfigJson))
                {
                    try
                    {
                        _conditieConfig = JsonSerializer.Deserialize<ConditieConfig>(ConditieConfigJson);
                    }
                    catch
                    {
                        _conditieConfig = null;
                    }
                }
                return _conditieConfig;
            }
        }
    }

    /// <summary>
    /// Result of evaluating a conditional placeholder
    /// </summary>
    public class ConditieEvaluatieResult
    {
        public bool Success { get; set; }
        public int? MatchedRule { get; set; }
        public string RawResult { get; set; } = string.Empty;
        public string ResolvedResult { get; set; } = string.Empty;
        public List<ConditieEvaluatieStap> EvaluationSteps { get; set; } = new List<ConditieEvaluatieStap>();
    }

    /// <summary>
    /// A single step in the evaluation process (for debugging)
    /// </summary>
    public class ConditieEvaluatieStap
    {
        public int Regel { get; set; }
        public string Conditie { get; set; } = string.Empty;
        public bool Result { get; set; }
    }

    /// <summary>
    /// Available operators for conditions
    /// </summary>
    public static class ConditieOperators
    {
        public new const string Equals = "=";
        public const string NotEquals = "!=";
        public const string GreaterThan = ">";
        public const string GreaterThanOrEqual = ">=";
        public const string LessThan = "<";
        public const string LessThanOrEqual = "<=";
        public const string Contains = "bevat";
        public const string StartsWith = "begint_met";
        public const string EndsWith = "eindigt_met";
        public const string Empty = "leeg";
        public const string NotEmpty = "niet_leeg";
        public const string In = "in";
        public const string NotIn = "niet_in";

        // Logical operators
        public const string And = "AND";
        public const string Or = "OR";
    }
}
