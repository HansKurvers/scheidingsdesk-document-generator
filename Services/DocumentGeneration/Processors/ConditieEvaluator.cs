using System.Text.Json;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Interface for evaluating conditional placeholder logic
    /// </summary>
    public interface IConditieEvaluator
    {
        /// <summary>
        /// Evaluate a conditional configuration against a context dictionary
        /// </summary>
        /// <param name="conditieConfigJson">JSON string containing the ConditieConfig</param>
        /// <param name="context">Dictionary of placeholder keys and their values</param>
        /// <returns>The resolved value based on the conditional logic</returns>
        string Evaluate(string? conditieConfigJson, Dictionary<string, string> context);
    }

    /// <summary>
    /// Evaluates conditional placeholder logic
    /// </summary>
    public class ConditieEvaluator : IConditieEvaluator
    {
        private readonly ILogger<ConditieEvaluator> _logger;

        public ConditieEvaluator(ILogger<ConditieEvaluator> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string Evaluate(string? conditieConfigJson, Dictionary<string, string> context)
        {
            if (string.IsNullOrEmpty(conditieConfigJson))
            {
                return string.Empty;
            }

            try
            {
                var config = ParseConditieConfig(conditieConfigJson);
                if (config == null)
                {
                    _logger.LogWarning("Failed to parse conditie config JSON");
                    return string.Empty;
                }

                return EvaluateConfig(config, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating conditie config");
                return string.Empty;
            }
        }

        /// <summary>
        /// Parse JSON string to ConditieConfig
        /// </summary>
        private ConditieConfig? ParseConditieConfig(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // First parse as JsonDocument to handle polymorphic types
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var config = new ConditieConfig
                {
                    Default = root.TryGetProperty("default", out var defaultProp)
                        ? defaultProp.GetString() ?? string.Empty
                        : string.Empty,
                    Regels = new List<ConditieRegel>()
                };

                if (root.TryGetProperty("regels", out var regelsProp) && regelsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var regelElement in regelsProp.EnumerateArray())
                    {
                        var regel = ParseRegel(regelElement);
                        if (regel != null)
                        {
                            config.Regels.Add(regel);
                        }
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse conditie config: {Json}", json);
                return null;
            }
        }

        /// <summary>
        /// Parse a single regel from JsonElement
        /// </summary>
        private ConditieRegel? ParseRegel(JsonElement element)
        {
            var regel = new ConditieRegel
            {
                Resultaat = element.TryGetProperty("resultaat", out var resultaatProp)
                    ? resultaatProp.GetString() ?? string.Empty
                    : string.Empty
            };

            if (element.TryGetProperty("conditie", out var conditieProp))
            {
                regel.Conditie = ParseConditie(conditieProp);
            }

            return regel;
        }

        /// <summary>
        /// Parse a conditie (can be either ConditieVoorwaarde or ConditieGroep)
        /// </summary>
        private Conditie ParseConditie(JsonElement element)
        {
            // Check if it's a group (has "voorwaarden" property)
            if (element.TryGetProperty("voorwaarden", out var voorwaardenProp))
            {
                var groep = new ConditieGroep
                {
                    Operator = element.TryGetProperty("operator", out var opProp)
                        ? opProp.GetString() ?? "AND"
                        : "AND",
                    Voorwaarden = new List<Conditie>()
                };

                if (voorwaardenProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var vwElement in voorwaardenProp.EnumerateArray())
                    {
                        groep.Voorwaarden.Add(ParseConditie(vwElement));
                    }
                }

                return groep;
            }

            // It's a simple voorwaarde
            var voorwaarde = new ConditieVoorwaarde
            {
                Veld = element.TryGetProperty("veld", out var veldProp)
                    ? veldProp.GetString() ?? string.Empty
                    : string.Empty,
                Operator = element.TryGetProperty("operator", out var operatorProp)
                    ? operatorProp.GetString() ?? "="
                    : "="
            };

            if (element.TryGetProperty("waarde", out var waardeProp))
            {
                voorwaarde.Waarde = ParseWaarde(waardeProp);
            }

            return voorwaarde;
        }

        /// <summary>
        /// Parse waarde which can be string, number, boolean, or array
        /// </summary>
        private object? ParseWaarde(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : e.ToString())
                    .ToList(),
                _ => null
            };
        }

        /// <summary>
        /// Evaluate a ConditieConfig against context
        /// </summary>
        private string EvaluateConfig(ConditieConfig config, Dictionary<string, string> context)
        {
            foreach (var regel in config.Regels)
            {
                if (EvaluateConditie(regel.Conditie, context))
                {
                    return regel.Resultaat;
                }
            }

            return config.Default;
        }

        /// <summary>
        /// Evaluate a single conditie against context
        /// </summary>
        private bool EvaluateConditie(Conditie conditie, Dictionary<string, string> context)
        {
            if (conditie is ConditieGroep groep)
            {
                var results = groep.Voorwaarden.Select(v => EvaluateConditie(v, context)).ToList();

                return groep.Operator.ToUpperInvariant() == "AND"
                    ? results.All(r => r)
                    : results.Any(r => r);
            }

            if (conditie is ConditieVoorwaarde voorwaarde)
            {
                return EvaluateVoorwaarde(voorwaarde, context);
            }

            return false;
        }

        /// <summary>
        /// Evaluate a simple voorwaarde
        /// </summary>
        private bool EvaluateVoorwaarde(ConditieVoorwaarde voorwaarde, Dictionary<string, string> context)
        {
            var veldWaarde = context.TryGetValue(voorwaarde.Veld, out var val) ? val : string.Empty;
            var compareWaarde = voorwaarde.Waarde?.ToString() ?? string.Empty;

            return voorwaarde.Operator switch
            {
                "=" => string.Equals(veldWaarde, compareWaarde, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(veldWaarde, compareWaarde, StringComparison.OrdinalIgnoreCase),
                ">" => TryCompareNumeric(veldWaarde, compareWaarde) > 0,
                ">=" => TryCompareNumeric(veldWaarde, compareWaarde) >= 0,
                "<" => TryCompareNumeric(veldWaarde, compareWaarde) < 0,
                "<=" => TryCompareNumeric(veldWaarde, compareWaarde) <= 0,
                "bevat" => veldWaarde.Contains(compareWaarde, StringComparison.OrdinalIgnoreCase),
                "begint_met" => veldWaarde.StartsWith(compareWaarde, StringComparison.OrdinalIgnoreCase),
                "eindigt_met" => veldWaarde.EndsWith(compareWaarde, StringComparison.OrdinalIgnoreCase),
                "leeg" => string.IsNullOrEmpty(veldWaarde),
                "niet_leeg" => !string.IsNullOrEmpty(veldWaarde),
                "in" => EvaluateIn(veldWaarde, voorwaarde.Waarde),
                "niet_in" => !EvaluateIn(veldWaarde, voorwaarde.Waarde),
                _ => false
            };
        }

        /// <summary>
        /// Compare two values numerically
        /// </summary>
        private int TryCompareNumeric(string? a, string? b)
        {
            if (double.TryParse(a, out var numA) && double.TryParse(b, out var numB))
            {
                return numA.CompareTo(numB);
            }
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Evaluate "in" operator (value is in array)
        /// </summary>
        private bool EvaluateIn(string veldWaarde, object? waarde)
        {
            if (waarde is List<string?> stringList)
            {
                return stringList.Any(s => string.Equals(s, veldWaarde, StringComparison.OrdinalIgnoreCase));
            }

            if (waarde is IEnumerable<object> objList)
            {
                return objList.Any(o => string.Equals(o?.ToString(), veldWaarde, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }
    }
}
