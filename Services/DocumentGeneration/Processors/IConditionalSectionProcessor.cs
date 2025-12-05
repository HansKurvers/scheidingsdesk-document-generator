using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Processor voor conditionele secties in Word documenten.
    /// Verwerkt [[IF:VeldNaam]]...[[ENDIF:VeldNaam]] syntax.
    /// </summary>
    public interface IConditionalSectionProcessor
    {
        /// <summary>
        /// Verwerkt alle conditionele secties in het document.
        /// - Als het veld leeg/null is: verwijdert het hele blok inclusief IF/ENDIF tags
        /// - Als het veld een waarde heeft: verwijdert alleen de IF/ENDIF tags, behoudt content
        /// </summary>
        /// <param name="document">Het Word document om te verwerken</param>
        /// <param name="replacements">Dictionary met veldnamen en hun waarden</param>
        /// <param name="correlationId">Correlation ID voor logging</param>
        void ProcessConditionalSections(
            WordprocessingDocument document,
            Dictionary<string, string> replacements,
            string correlationId);
    }
}
