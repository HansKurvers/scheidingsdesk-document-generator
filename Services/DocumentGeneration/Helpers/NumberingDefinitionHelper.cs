using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor het aanmaken van Word numbering definitions.
    /// Dit maakt dynamische nummering mogelijk die zich aanpast bij verwijderen/toevoegen.
    /// </summary>
    public static class NumberingDefinitionHelper
    {
        // Vaste IDs voor onze numbering definitions
        public const int ArtikelNumberingId = 1001;
        public const int SubArtikelNumberingId = 1002;

        /// <summary>
        /// Zorgt dat het document een NumberingDefinitionsPart heeft met onze artikel-nummering.
        /// Moet worden aangeroepen VOOR artikelen worden verwerkt.
        /// </summary>
        public static void EnsureNumberingDefinitions(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return;

            // Haal bestaande NumberingDefinitionsPart op of maak nieuwe
            var numberingPart = mainPart.NumberingDefinitionsPart;
            if (numberingPart == null)
            {
                numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
                numberingPart.Numbering = new Numbering();
            }

            var numbering = numberingPart.Numbering;

            // Check of onze definitions al bestaan
            var existingNum = numbering.Elements<NumberingInstance>()
                .FirstOrDefault(n => n.NumberID?.Value == ArtikelNumberingId);

            if (existingNum != null)
            {
                // Al aanwezig, niets te doen
                return;
            }

            // Maak AbstractNum voor artikel nummering (Artikel 1, Artikel 2, etc.)
            var abstractNumArtikel = CreateArtikelAbstractNum(1001);
            numbering.Append(abstractNumArtikel);

            // Maak AbstractNum voor subartikel nummering (1.1, 1.2, etc.)
            var abstractNumSubArtikel = CreateSubArtikelAbstractNum(1002);
            numbering.Append(abstractNumSubArtikel);

            // Maak NumberingInstance die naar AbstractNum verwijst
            var numInstanceArtikel = new NumberingInstance(
                new AbstractNumId { Val = 1001 }
            )
            { NumberID = ArtikelNumberingId };
            numbering.Append(numInstanceArtikel);

            var numInstanceSubArtikel = new NumberingInstance(
                new AbstractNumId { Val = 1002 }
            )
            { NumberID = SubArtikelNumberingId };
            numbering.Append(numInstanceSubArtikel);

            numbering.Save();
        }

        /// <summary>
        /// Maakt AbstractNum voor hoofdartikelen: "Artikel 1", "Artikel 2", etc.
        /// </summary>
        private static AbstractNum CreateArtikelAbstractNum(int abstractNumId)
        {
            var abstractNum = new AbstractNum { AbstractNumberId = abstractNumId };

            // Multi-level list settings
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel });

            // Level 0: Artikel 1, Artikel 2, etc.
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level0.Append(new LevelText { Val = "Artikel %1" });  // "Artikel 1", "Artikel 2"
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            // Formatting voor het nummer
            var runProps0 = new NumberingSymbolRunProperties();
            runProps0.Append(new Bold());
            level0.Append(runProps0);

            // Paragraph properties (indentation)
            var pPr0 = new PreviousParagraphProperties();
            pPr0.Append(new Indentation { Left = "0", Hanging = "0" });
            level0.Append(pPr0);

            abstractNum.Append(level0);

            // Level 1: Voor subartikelen binnen dit systeem (1.1, 1.2) - backup
            var level1 = new Level { LevelIndex = 1 };
            level1.Append(new StartNumberingValue { Val = 1 });
            level1.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level1.Append(new LevelText { Val = "%1.%2" });  // "1.1", "1.2"
            level1.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr1 = new PreviousParagraphProperties();
            pPr1.Append(new Indentation { Left = "720", Hanging = "360" });
            level1.Append(pPr1);

            abstractNum.Append(level1);

            return abstractNum;
        }

        /// <summary>
        /// Maakt AbstractNum voor subartikelen: "1.1", "1.2", etc.
        /// Deze nummering is gekoppeld aan het hoofdartikel nummer.
        /// </summary>
        private static AbstractNum CreateSubArtikelAbstractNum(int abstractNumId)
        {
            var abstractNum = new AbstractNum { AbstractNumberId = abstractNumId };
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel });

            // Level 0: X.1, X.2 (X moet handmatig worden gezet per artikel)
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level0.Append(new LevelText { Val = "%1" });  // Alleen het subnummer, prefix komt apart
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr0 = new PreviousParagraphProperties();
            pPr0.Append(new Indentation { Left = "720", Hanging = "360" });
            level0.Append(pPr0);

            abstractNum.Append(level0);

            return abstractNum;
        }

        /// <summary>
        /// Maakt NumberingProperties voor een hoofdartikel paragraph.
        /// </summary>
        public static NumberingProperties CreateArtikelNumberingProperties()
        {
            return new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = ArtikelNumberingId }
            );
        }

        /// <summary>
        /// Maakt NumberingProperties voor een subartikel paragraph.
        /// </summary>
        public static NumberingProperties CreateSubArtikelNumberingProperties()
        {
            return new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = SubArtikelNumberingId }
            );
        }

        /// <summary>
        /// Reset de subartikel nummering (bij start van nieuw hoofdartikel).
        /// Dit wordt gedaan door een nieuwe NumberingInstance aan te maken.
        /// </summary>
        public static int CreateRestartedSubArtikelNumbering(
            WordprocessingDocument document,
            int artikelNumber,
            int nextNumId)
        {
            var numberingPart = document.MainDocumentPart?.NumberingDefinitionsPart;
            if (numberingPart?.Numbering == null) return SubArtikelNumberingId;

            // Maak nieuwe AbstractNum specifiek voor dit artikel
            var abstractNum = new AbstractNum { AbstractNumberId = nextNumId };
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.HybridMultilevel });

            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level0.Append(new LevelText { Val = $"{artikelNumber}.%1" });  // "3.1", "3.2" voor artikel 3
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr0 = new PreviousParagraphProperties();
            pPr0.Append(new Indentation { Left = "720", Hanging = "360" });
            level0.Append(pPr0);

            abstractNum.Append(level0);
            numberingPart.Numbering.Append(abstractNum);

            // Maak NumberingInstance
            var numInstance = new NumberingInstance(
                new AbstractNumId { Val = nextNumId }
            )
            { NumberID = nextNumId };
            numberingPart.Numbering.Append(numInstance);

            numberingPart.Numbering.Save();

            return nextNumId;
        }
    }
}
