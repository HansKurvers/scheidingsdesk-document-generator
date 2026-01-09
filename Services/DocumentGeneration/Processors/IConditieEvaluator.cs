using System.Collections.Generic;
using scheidingsdesk_document_generator.Models;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Service interface for evaluating conditional placeholder logic
    /// </summary>
    public interface IConditieEvaluator
    {
        /// <summary>
        /// Evaluates a conditional placeholder configuration against the provided context
        /// </summary>
        /// <param name="config">The conditional configuration to evaluate</param>
        /// <param name="context">Dictionary of field values to evaluate against</param>
        /// <returns>The evaluation result including matched rule and resolved value</returns>
        ConditieEvaluatieResult Evaluate(ConditieConfig config, Dictionary<string, object> context);

        /// <summary>
        /// Resolves nested placeholders in a result string
        /// e.g., "[[Partij1_Roepnaam]]" becomes the actual value
        /// </summary>
        /// <param name="text">Text potentially containing nested placeholders</param>
        /// <param name="replacements">Dictionary of available replacements</param>
        /// <param name="maxDepth">Maximum recursion depth (default 5)</param>
        /// <returns>Text with all placeholders resolved</returns>
        string ResolveNestedPlaceholders(string text, Dictionary<string, string> replacements, int maxDepth = 5);

        /// <summary>
        /// Builds a context dictionary from dossier data for condition evaluation
        /// Includes computed fields like AantalKinderen, HeeftAlimentatie, etc.
        /// </summary>
        /// <param name="data">The dossier data</param>
        /// <param name="replacements">Current placeholder replacements</param>
        /// <returns>Context dictionary for condition evaluation</returns>
        Dictionary<string, object> BuildEvaluationContext(DossierData data, Dictionary<string, string> replacements);
    }
}
