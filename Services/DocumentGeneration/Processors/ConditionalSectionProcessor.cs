using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Processor voor conditionele secties in Word documenten.
    /// Verwerkt [[IF:VeldNaam]]...[[ENDIF:VeldNaam]] syntax.
    /// </summary>
    public class ConditionalSectionProcessor : IConditionalSectionProcessor
    {
        private readonly ILogger<ConditionalSectionProcessor> _logger;

        public ConditionalSectionProcessor(ILogger<ConditionalSectionProcessor> logger)
        {
            _logger = logger;
        }

        public void ProcessConditionalSections(
            WordprocessingDocument document,
            Dictionary<string, string> replacements,
            string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Starting conditional sections processing");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                _logger.LogWarning($"[{correlationId}] Document has no body");
                return;
            }

            // Process main body
            ProcessElementConditionals(mainPart.Document.Body, replacements, correlationId);

            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                if (headerPart.Header != null)
                {
                    ProcessElementConditionals(headerPart.Header, replacements, correlationId);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                if (footerPart.Footer != null)
                {
                    ProcessElementConditionals(footerPart.Footer, replacements, correlationId);
                }
            }

            _logger.LogInformation($"[{correlationId}] Conditional sections processing completed");
        }

        private void ProcessElementConditionals(
            OpenXmlElement element,
            Dictionary<string, string> replacements,
            string correlationId)
        {
            // BELANGRIJK: We moeten de volledige tekst van het element krijgen,
            // want [[IF:Veld]] kan over meerdere <w:t> elements verspreid zijn.

            // Strategie:
            // 1. Verzamel alle tekst uit paragraphs
            // 2. Zoek [[IF:VeldNaam]] en [[ENDIF:VeldNaam]] patronen
            // 3. Bepaal welke paragraphs binnen een conditioneel blok vallen
            // 4. Verwijder hele paragraphs als veld leeg is, of alleen de tags als veld gevuld is

            var paragraphs = element.Descendants<Paragraph>().ToList();

            // Bouw een map van paragraph index naar tekst
            var paragraphTexts = paragraphs
                .Select((p, index) => new { Index = index, Text = GetParagraphText(p), Paragraph = p })
                .ToList();

            // Zoek alle IF/ENDIF paren
            var conditionalBlocks = FindConditionalBlocks(paragraphTexts.Select(p => p.Text).ToList(), correlationId);

            // Verwerk van achteren naar voren (zodat indices kloppen bij verwijderen)
            foreach (var block in conditionalBlocks.OrderByDescending(b => b.StartIndex))
            {
                var fieldName = block.FieldName;
                var hasValue = HasFieldValue(fieldName, replacements);

                _logger.LogDebug($"[{correlationId}] Processing conditional block for '{fieldName}', hasValue: {hasValue}, paragraphs {block.StartIndex}-{block.EndIndex}");

                if (hasValue)
                {
                    // Veld heeft waarde: verwijder alleen de IF/ENDIF tags
                    RemoveConditionalTags(paragraphTexts[block.StartIndex].Paragraph, fieldName, isIfTag: true);
                    RemoveConditionalTags(paragraphTexts[block.EndIndex].Paragraph, fieldName, isIfTag: false);
                }
                else
                {
                    // Veld is leeg: verwijder alle paragraphs in het blok
                    for (int i = block.EndIndex; i >= block.StartIndex; i--)
                    {
                        var paragraph = paragraphTexts[i].Paragraph;
                        var paragraphText = GetParagraphText(paragraph);
                        _logger.LogDebug($"[{correlationId}] Removing paragraph {i}: '{paragraphText.Substring(0, Math.Min(50, paragraphText.Length))}'");
                        paragraph.Remove();
                    }
                }
            }
        }

        private bool HasFieldValue(string fieldName, Dictionary<string, string> replacements)
        {
            // Direct lookup
            if (replacements.TryGetValue(fieldName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            // Case-insensitive lookup
            var caseInsensitiveKey = replacements.Keys
                .FirstOrDefault(k => k.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (caseInsensitiveKey != null)
            {
                return !string.IsNullOrWhiteSpace(replacements[caseInsensitiveKey]);
            }

            return false;
        }

        private string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        }

        private List<ConditionalBlock> FindConditionalBlocks(List<string> paragraphTexts, string correlationId)
        {
            var blocks = new List<ConditionalBlock>();
            var ifPattern = new Regex(@"\[\[IF:(\w+)\]\]", RegexOptions.IgnoreCase);
            var endIfPattern = new Regex(@"\[\[ENDIF:(\w+)\]\]", RegexOptions.IgnoreCase);

            // Vind alle IF starts
            var ifStarts = new Stack<(int Index, string FieldName)>();

            for (int i = 0; i < paragraphTexts.Count; i++)
            {
                var text = paragraphTexts[i];

                // Check voor IF
                var ifMatch = ifPattern.Match(text);
                if (ifMatch.Success)
                {
                    ifStarts.Push((i, ifMatch.Groups[1].Value));
                    _logger.LogDebug($"[{correlationId}] Found IF:{ifMatch.Groups[1].Value} at paragraph {i}");
                }

                // Check voor ENDIF
                var endIfMatch = endIfPattern.Match(text);
                if (endIfMatch.Success && ifStarts.Count > 0)
                {
                    var endFieldName = endIfMatch.Groups[1].Value;

                    // Zoek matching IF (moet zelfde veldnaam zijn)
                    var matchingIf = ifStarts.FirstOrDefault(s =>
                        s.FieldName.Equals(endFieldName, StringComparison.OrdinalIgnoreCase));

                    if (matchingIf.FieldName != null)
                    {
                        // Verwijder uit stack
                        var tempStack = new Stack<(int Index, string FieldName)>();
                        while (ifStarts.Count > 0)
                        {
                            var item = ifStarts.Pop();
                            if (item.FieldName.Equals(endFieldName, StringComparison.OrdinalIgnoreCase))
                            {
                                blocks.Add(new ConditionalBlock
                                {
                                    FieldName = endFieldName,
                                    StartIndex = item.Index,
                                    EndIndex = i
                                });
                                break;
                            }
                            tempStack.Push(item);
                        }
                        // Zet overige terug
                        while (tempStack.Count > 0)
                        {
                            ifStarts.Push(tempStack.Pop());
                        }

                        _logger.LogDebug($"[{correlationId}] Found ENDIF:{endFieldName} at paragraph {i}");
                    }
                }
            }

            if (ifStarts.Count > 0)
            {
                _logger.LogWarning($"[{correlationId}] Unclosed IF blocks found: {string.Join(", ", ifStarts.Select(s => s.FieldName))}");
            }

            return blocks;
        }

        private void RemoveConditionalTags(Paragraph paragraph, string fieldName, bool isIfTag)
        {
            var tagPattern = isIfTag
                ? $@"\[\[IF:{Regex.Escape(fieldName)}\]\]"
                : $@"\[\[ENDIF:{Regex.Escape(fieldName)}\]\]";
            var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);

            foreach (var text in paragraph.Descendants<Text>())
            {
                if (regex.IsMatch(text.Text))
                {
                    text.Text = regex.Replace(text.Text, "");
                }
            }

            // Verwijder paragraph als deze nu helemaal leeg is
            if (string.IsNullOrWhiteSpace(GetParagraphText(paragraph)))
            {
                paragraph.Remove();
            }
        }

        private class ConditionalBlock
        {
            public string FieldName { get; set; } = string.Empty;
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
        }
    }
}
