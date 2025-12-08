using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor multi-level juridische nummering.
    /// Gebruikt Word's native list numbering met meerdere levels.
    /// Level 0 = Artikelen (Artikel 1, Artikel 2)
    /// Level 1 = Subartikelen (1.1, 1.2, 2.1)
    /// </summary>
    public static class LegalNumberingHelper
    {
        // Gebruik hoge numIds om conflicten met template te voorkomen
        public const int AbstractNumId = 9001;
        public const int NumberingInstanceId = 9001;

        // Counter voor restart numbering instances (ThreadStatic voor thread safety)
        [ThreadStatic]
        private static int _nextRestartNumId;

        /// <summary>
        /// Zorgt dat het document onze juridische nummering definitie heeft.
        /// Moet worden aangeroepen VOOR artikelen worden verwerkt.
        /// </summary>
        public static void EnsureLegalNumberingDefinition(WordprocessingDocument document)
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

            // Check of onze definitie al bestaat
            var existingAbstractNum = numbering.Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == AbstractNumId);

            if (existingAbstractNum != null)
            {
                return; // Al aanwezig
            }

            // Maak AbstractNum voor juridische nummering
            var abstractNum = CreateLegalAbstractNum();

            // Voeg toe NA bestaande AbstractNums maar VOOR NumberingInstances
            var lastAbstractNum = numbering.Elements<AbstractNum>().LastOrDefault();
            if (lastAbstractNum != null)
            {
                lastAbstractNum.InsertAfterSelf(abstractNum);
            }
            else
            {
                numbering.PrependChild(abstractNum);
            }

            // Maak NumberingInstance
            var numInstance = new NumberingInstance(
                new AbstractNumId { Val = AbstractNumId }
            )
            {
                NumberID = NumberingInstanceId
            };

            numbering.Append(numInstance);
            numbering.Save();
        }

        /// <summary>
        /// Maakt de AbstractNum definitie voor juridische nummering.
        /// </summary>
        private static AbstractNum CreateLegalAbstractNum()
        {
            var abstractNum = new AbstractNum { AbstractNumberId = AbstractNumId };

            // Unieke identifier
            abstractNum.Append(new Nsid { Val = "9001ABCD" });
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.Multilevel });

            // Level 0: Artikel 1, Artikel 2, etc.
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level0.Append(new LevelText { Val = "Artikel %1" });
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr0 = new PreviousParagraphProperties();
            pPr0.Append(new Indentation { Left = "0", Hanging = "0" });
            level0.Append(pPr0);

            // Formatting voor artikel nummer (bold)
            var rPr0 = new NumberingSymbolRunProperties();
            rPr0.Append(new Bold());
            level0.Append(rPr0);

            abstractNum.Append(level0);

            // Level 1: 1.1, 1.2, 2.1, etc. (erft %1 van level 0!)
            var level1 = new Level { LevelIndex = 1 };
            level1.Append(new StartNumberingValue { Val = 1 });
            level1.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level1.Append(new LevelText { Val = "%1.%2" });  // %1 = artikel nummer, %2 = subartikel nummer
            level1.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr1 = new PreviousParagraphProperties();
            pPr1.Append(new Indentation { Left = "0", Hanging = "0" });
            level1.Append(pPr1);

            abstractNum.Append(level1);

            // Voeg lege levels 2-8 toe (Word verwacht 9 levels)
            for (int i = 2; i <= 8; i++)
            {
                var level = new Level { LevelIndex = i };
                level.Append(new StartNumberingValue { Val = 1 });
                level.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
                level.Append(new LevelText { Val = $"%{i + 1}." });
                level.Append(new LevelJustification { Val = LevelJustificationValues.Left });
                abstractNum.Append(level);
            }

            return abstractNum;
        }

        /// <summary>
        /// Maakt NumberingProperties voor een artikel (level 0).
        /// </summary>
        public static NumberingProperties CreateArtikelNumberingProperties(int numId = NumberingInstanceId)
        {
            return new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = numId }
            );
        }

        /// <summary>
        /// Maakt NumberingProperties voor een subartikel (level 1).
        /// </summary>
        public static NumberingProperties CreateSubArtikelNumberingProperties(int numId = NumberingInstanceId)
        {
            return new NumberingProperties(
                new NumberingLevelReference { Val = 1 },
                new NumberingId { Val = numId }
            );
        }

        /// <summary>
        /// Reset de artikel nummering naar 1.
        /// Dit maakt een nieuwe NumberingInstance met een startOverride.
        /// </summary>
        public static int CreateRestartedNumberingInstance(WordprocessingDocument document)
        {
            var numberingPart = document.MainDocumentPart?.NumberingDefinitionsPart;
            if (numberingPart?.Numbering == null) return NumberingInstanceId;

            int newNumId = _nextRestartNumId;
            _nextRestartNumId++;

            // Maak nieuwe NumberingInstance met restart
            var numInstance = new NumberingInstance { NumberID = newNumId };
            numInstance.Append(new AbstractNumId { Val = AbstractNumId });

            // Override level 0 om te starten bij 1
            var lvlOverride0 = new LevelOverride { LevelIndex = 0 };
            lvlOverride0.Append(new StartOverrideNumberingValue { Val = 1 });
            numInstance.Append(lvlOverride0);

            // Override level 1 om te starten bij 1
            var lvlOverride1 = new LevelOverride { LevelIndex = 1 };
            lvlOverride1.Append(new StartOverrideNumberingValue { Val = 1 });
            numInstance.Append(lvlOverride1);

            numberingPart.Numbering.Append(numInstance);
            numberingPart.Numbering.Save();

            return newNumId;
        }

        /// <summary>
        /// Reset de static counter (nodig voor unit tests en nieuwe documenten).
        /// </summary>
        public static void ResetCounters()
        {
            _nextRestartNumId = 9100;
        }
    }
}
