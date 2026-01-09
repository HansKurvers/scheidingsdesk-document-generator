using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Evaluates conditional placeholder logic based on dossier context
    /// </summary>
    public class ConditieEvaluator : IConditieEvaluator
    {
        private readonly ILogger<ConditieEvaluator> _logger;
        private static readonly Regex PlaceholderRegex = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

        public ConditieEvaluator(ILogger<ConditieEvaluator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Evaluates a conditional configuration against the provided context
        /// </summary>
        public ConditieEvaluatieResult Evaluate(ConditieConfig config, Dictionary<string, object> context)
        {
            var result = new ConditieEvaluatieResult
            {
                Success = true,
                EvaluationSteps = new List<ConditieEvaluatieStap>()
            };

            // Evaluate each rule in order
            for (int i = 0; i < config.Regels.Count; i++)
            {
                var regel = config.Regels[i];
                var ruleNumber = i + 1;

                var conditionDescription = DescribeCondition(regel.Conditie);
                var conditionResult = EvaluateCondition(regel.Conditie, context);

                result.EvaluationSteps.Add(new ConditieEvaluatieStap
                {
                    Regel = ruleNumber,
                    Conditie = conditionDescription,
                    Result = conditionResult
                });

                if (conditionResult)
                {
                    result.MatchedRule = ruleNumber;
                    result.RawResult = regel.Resultaat;
                    _logger.LogDebug("Condition rule {RuleNumber} matched: {Description}", ruleNumber, conditionDescription);
                    return result;
                }
            }

            // No rule matched, use default
            result.RawResult = config.Default;
            _logger.LogDebug("No condition rules matched, using default value");
            return result;
        }

        /// <summary>
        /// Evaluates a single condition (either group or comparison)
        /// </summary>
        private bool EvaluateCondition(Conditie conditie, Dictionary<string, object> context)
        {
            if (conditie.IsGroep)
            {
                return EvaluateGroup(conditie, context);
            }
            else if (conditie.IsVoorwaarde)
            {
                return EvaluateComparison(conditie, context);
            }

            _logger.LogWarning("Invalid condition: neither group nor comparison");
            return false;
        }

        /// <summary>
        /// Evaluates a group condition (AND/OR)
        /// </summary>
        private bool EvaluateGroup(Conditie groep, Dictionary<string, object> context)
        {
            if (groep.Voorwaarden == null || groep.Voorwaarden.Count == 0)
            {
                return true; // Empty group evaluates to true
            }

            var isAnd = groep.LogicalOperator?.ToUpperInvariant() == ConditieOperators.And;

            foreach (var voorwaarde in groep.Voorwaarden)
            {
                var result = EvaluateCondition(voorwaarde, context);

                if (isAnd && !result)
                {
                    return false; // AND: short-circuit on first false
                }
                if (!isAnd && result)
                {
                    return true; // OR: short-circuit on first true
                }
            }

            return isAnd; // AND returns true if all passed, OR returns false if none passed
        }

        /// <summary>
        /// Evaluates a single comparison condition
        /// </summary>
        private bool EvaluateComparison(Conditie conditie, Dictionary<string, object> context)
        {
            var veld = conditie.Veld;
            var op = conditie.VergelijkingsOperator;

            if (string.IsNullOrEmpty(veld) || string.IsNullOrEmpty(op))
            {
                _logger.LogWarning("Invalid comparison: missing field or operator");
                return false;
            }

            // Get the field value from context (case-insensitive)
            var contextKey = context.Keys.FirstOrDefault(k => k.Equals(veld, StringComparison.OrdinalIgnoreCase));
            var fieldValue = contextKey != null ? context[contextKey] : null;

            // Get the comparison value
            var compareValue = GetCompareValue(conditie.Waarde);

            return EvaluateOperator(op, fieldValue, compareValue);
        }

        /// <summary>
        /// Extracts the comparison value from the JsonElement
        /// </summary>
        private object? GetCompareValue(JsonElement? waarde)
        {
            if (!waarde.HasValue) return null;

            var element = waarde.Value;

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intVal))
                        return intVal;
                    if (element.TryGetDouble(out var doubleVal))
                        return doubleVal;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Array:
                    return element.EnumerateArray()
                        .Select(e => GetCompareValue(e))
                        .ToList();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Evaluates the comparison operator
        /// </summary>
        private bool EvaluateOperator(string op, object? fieldValue, object? compareValue)
        {
            switch (op)
            {
                case ConditieOperators.Empty:
                    return IsEmpty(fieldValue);

                case ConditieOperators.NotEmpty:
                    return !IsEmpty(fieldValue);

                case ConditieOperators.Equals:
                    return AreEqual(fieldValue, compareValue);

                case ConditieOperators.NotEquals:
                    return !AreEqual(fieldValue, compareValue);

                case ConditieOperators.GreaterThan:
                    return Compare(fieldValue, compareValue) > 0;

                case ConditieOperators.GreaterThanOrEqual:
                    return Compare(fieldValue, compareValue) >= 0;

                case ConditieOperators.LessThan:
                    return Compare(fieldValue, compareValue) < 0;

                case ConditieOperators.LessThanOrEqual:
                    return Compare(fieldValue, compareValue) <= 0;

                case ConditieOperators.Contains:
                    return ContainsString(fieldValue, compareValue);

                case ConditieOperators.StartsWith:
                    return StartsWithString(fieldValue, compareValue);

                case ConditieOperators.EndsWith:
                    return EndsWithString(fieldValue, compareValue);

                case ConditieOperators.In:
                    return IsInList(fieldValue, compareValue);

                case ConditieOperators.NotIn:
                    return !IsInList(fieldValue, compareValue);

                default:
                    _logger.LogWarning("Unknown operator: {Operator}", op);
                    return false;
            }
        }

        private bool IsEmpty(object? value)
        {
            if (value == null) return true;
            if (value is string str) return string.IsNullOrEmpty(str);
            return false;
        }

        private bool AreEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // Handle boolean comparisons
            if (TryConvertToBool(a, out var aBool) && TryConvertToBool(b, out var bBool))
            {
                return aBool == bBool;
            }

            // Handle numeric comparisons
            if (TryConvertToDouble(a, out var aNum) && TryConvertToDouble(b, out var bNum))
            {
                return Math.Abs(aNum - bNum) < 0.0001;
            }

            // String comparison (case-insensitive)
            return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private int Compare(object? a, object? b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            // Numeric comparison
            if (TryConvertToDouble(a, out var aNum) && TryConvertToDouble(b, out var bNum))
            {
                return aNum.CompareTo(bNum);
            }

            // Date comparison
            if (DateTime.TryParse(a.ToString(), out var aDate) && DateTime.TryParse(b.ToString(), out var bDate))
            {
                return aDate.CompareTo(bDate);
            }

            // String comparison
            return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private bool ContainsString(object? fieldValue, object? compareValue)
        {
            if (fieldValue == null || compareValue == null) return false;
            return fieldValue.ToString()!.Contains(compareValue.ToString()!, StringComparison.OrdinalIgnoreCase);
        }

        private bool StartsWithString(object? fieldValue, object? compareValue)
        {
            if (fieldValue == null || compareValue == null) return false;
            return fieldValue.ToString()!.StartsWith(compareValue.ToString()!, StringComparison.OrdinalIgnoreCase);
        }

        private bool EndsWithString(object? fieldValue, object? compareValue)
        {
            if (fieldValue == null || compareValue == null) return false;
            return fieldValue.ToString()!.EndsWith(compareValue.ToString()!, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsInList(object? fieldValue, object? listValue)
        {
            if (fieldValue == null) return false;

            if (listValue is List<object?> list)
            {
                return list.Any(item => AreEqual(fieldValue, item));
            }

            // Single value comparison
            return AreEqual(fieldValue, listValue);
        }

        private bool TryConvertToDouble(object? value, out double result)
        {
            result = 0;
            if (value == null) return false;

            if (value is double d) { result = d; return true; }
            if (value is int i) { result = i; return true; }
            if (value is decimal dec) { result = (double)dec; return true; }
            if (value is float f) { result = f; return true; }
            if (value is long l) { result = l; return true; }

            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private bool TryConvertToBool(object? value, out bool result)
        {
            result = false;
            if (value == null) return false;

            if (value is bool b) { result = b; return true; }

            var str = value.ToString()?.ToLowerInvariant();
            if (str == "true" || str == "ja" || str == "1" || str == "yes")
            {
                result = true;
                return true;
            }
            if (str == "false" || str == "nee" || str == "0" || str == "no")
            {
                result = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a human-readable description of a condition
        /// </summary>
        private string DescribeCondition(Conditie conditie)
        {
            if (conditie.IsGroep)
            {
                var descriptions = conditie.Voorwaarden?
                    .Select(v => DescribeCondition(v))
                    .ToList() ?? new List<string>();

                var op = conditie.LogicalOperator ?? "AND";
                return $"({string.Join($" {op} ", descriptions)})";
            }
            else if (conditie.IsVoorwaarde)
            {
                var valueStr = conditie.Waarde.HasValue
                    ? JsonSerializer.Serialize(conditie.Waarde.Value)
                    : "null";
                return $"{conditie.Veld} {conditie.VergelijkingsOperator} {valueStr}";
            }

            return "?";
        }

        /// <summary>
        /// Resolves nested placeholders in a result string
        /// </summary>
        public string ResolveNestedPlaceholders(string text, Dictionary<string, string> replacements, int maxDepth = 5)
        {
            if (string.IsNullOrEmpty(text) || maxDepth <= 0)
            {
                return text;
            }

            var result = text;
            var hasReplacements = true;
            var depth = 0;

            while (hasReplacements && depth < maxDepth)
            {
                hasReplacements = false;
                depth++;

                var matches = PlaceholderRegex.Matches(result);
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value;
                    var replacementKey = replacements.Keys.FirstOrDefault(k =>
                        k.Equals(key, StringComparison.OrdinalIgnoreCase));

                    if (replacementKey != null)
                    {
                        result = result.Replace(match.Value, replacements[replacementKey]);
                        hasReplacements = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a context dictionary from dossier data for condition evaluation
        /// </summary>
        public Dictionary<string, object> BuildEvaluationContext(DossierData data, Dictionary<string, string> replacements)
        {
            var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Add all existing replacements as string values
            foreach (var kvp in replacements)
            {
                context[kvp.Key] = kvp.Value;
            }

            // Add computed fields

            // Children-related computed fields
            var minderjarigCount = data.Kinderen.Count(k =>
            {
                if (k.GeboorteDatum.HasValue)
                {
                    var age = DateTime.Today.Year - k.GeboorteDatum.Value.Year;
                    if (k.GeboorteDatum.Value.Date > DateTime.Today.AddYears(-age)) age--;
                    return age < 18;
                }
                return true; // Assume minor if no birthdate
            });

            context["AantalKinderen"] = data.Kinderen.Count;
            context["AantalMinderjarigeKinderen"] = minderjarigCount;
            context["HeeftKinderen"] = data.Kinderen.Count > 0;

            // Alimentatie-related computed fields
            context["HeeftAlimentatie"] = data.Alimentatie != null;
            context["HeeftKinderrekening"] = data.Alimentatie?.IsKinderrekeningBetaalwijze ?? false;

            // Relationship-related computed fields
            var isCoOuderschap = false;
            if (data.OuderschapsplanInfo != null)
            {
                // Check if gezag situation indicates co-parenting
                // GezagPartij: 1 = Gezamenlijk gezag
                var gezagPartij = data.OuderschapsplanInfo.GezagPartij;
                isCoOuderschap = gezagPartij == 1; // Gezamenlijk gezag
            }
            context["IsCoOuderschap"] = isCoOuderschap;
            context["HeeftGezamenlijkGezag"] = isCoOuderschap;

            // Dossier-related computed fields
            context["IsAnoniem"] = data.IsAnoniem ?? false;
            context["DossierStatus"] = data.Status ?? "";

            // Party gender fields (useful for conditional text like "de vader/de moeder")
            if (data.Partij1 != null)
            {
                context["Partij1_Geslacht"] = data.Partij1.Geslacht ?? "";
                context["Partij1_IsMan"] = data.Partij1.Geslacht?.ToLowerInvariant() == "man";
                context["Partij1_IsVrouw"] = data.Partij1.Geslacht?.ToLowerInvariant() == "vrouw";
            }

            if (data.Partij2 != null)
            {
                context["Partij2_Geslacht"] = data.Partij2.Geslacht ?? "";
                context["Partij2_IsMan"] = data.Partij2.Geslacht?.ToLowerInvariant() == "man";
                context["Partij2_IsVrouw"] = data.Partij2.Geslacht?.ToLowerInvariant() == "vrouw";
            }

            _logger.LogDebug("Built evaluation context with {Count} entries", context.Count);

            return context;
        }
    }
}
