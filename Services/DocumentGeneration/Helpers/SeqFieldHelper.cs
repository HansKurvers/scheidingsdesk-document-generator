using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor het aanmaken van Word SEQ (Sequence) velden.
    /// SEQ velden zijn onafhankelijke tellers die niet interfereren met bullets/lijsten.
    /// </summary>
    public static class SeqFieldHelper
    {
        /// <summary>
        /// Maakt een SEQ veld voor artikel nummering.
        /// Output: "Artikel 1", "Artikel 2", etc.
        /// </summary>
        /// <param name="seqName">Naam van de sequence (bijv. "Artikel")</param>
        /// <param name="prefix">Tekst voor het nummer (bijv. "Artikel ")</param>
        /// <param name="resetTo">Als niet null, reset de teller naar deze waarde</param>
        /// <returns>Lijst van OpenXML elementen die het veld vormen</returns>
        public static List<OpenXmlElement> CreateSeqField(
            string seqName,
            string prefix = "",
            int? resetTo = null)
        {
            var elements = new List<OpenXmlElement>();

            // Prefix tekst (bijv. "Artikel ")
            if (!string.IsNullOrEmpty(prefix))
            {
                var prefixRun = new Run(new Text(prefix) { Space = SpaceProcessingModeValues.Preserve });
                elements.Add(prefixRun);
            }

            // SEQ veld instructie
            // \r n = reset naar n
            // \* ARABIC = Arabische cijfers (1, 2, 3)
            string instruction = resetTo.HasValue
                ? $" SEQ {seqName} \\r {resetTo.Value} \\* ARABIC "
                : $" SEQ {seqName} \\* ARABIC ";

            // Begin veld
            var beginRun = new Run(new FieldChar { FieldCharType = FieldCharValues.Begin });
            elements.Add(beginRun);

            // Instructie
            var instrRun = new Run(new FieldCode(instruction) { Space = SpaceProcessingModeValues.Preserve });
            elements.Add(instrRun);

            // Separator
            var separatorRun = new Run(new FieldChar { FieldCharType = FieldCharValues.Separate });
            elements.Add(separatorRun);

            // Placeholder waarde (Word vervangt dit bij update)
            var valueRun = new Run(new Text("0"));
            elements.Add(valueRun);

            // Einde veld
            var endRun = new Run(new FieldChar { FieldCharType = FieldCharValues.End });
            elements.Add(endRun);

            return elements;
        }

        /// <summary>
        /// Maakt een samengesteld subartikel veld: "X.Y" waar X het artikelnummer is.
        /// </summary>
        /// <param name="artikelNumber">Het huidige artikelnummer (voor de prefix)</param>
        /// <param name="resetSubNumber">Als true, reset het subnummer naar 1</param>
        /// <returns>Lijst van OpenXML elementen</returns>
        public static List<OpenXmlElement> CreateSubArtikelSeqField(
            int artikelNumber,
            bool resetSubNumber = false)
        {
            var elements = new List<OpenXmlElement>();

            // Prefix: artikelnummer + punt (bijv. "1.")
            var prefixRun = new Run(new Text($"{artikelNumber}.") { Space = SpaceProcessingModeValues.Preserve });
            elements.Add(prefixRun);

            // SEQ veld voor subartikel - unieke naam per artikel zodat elke artikel zijn eigen teller heeft
            string seqName = $"SubArt{artikelNumber}";
            string instruction = resetSubNumber
                ? $" SEQ {seqName} \\r 1 \\* ARABIC "
                : $" SEQ {seqName} \\* ARABIC ";

            // Begin veld
            var beginRun = new Run(new FieldChar { FieldCharType = FieldCharValues.Begin });
            elements.Add(beginRun);

            // Instructie
            var instrRun = new Run(new FieldCode(instruction) { Space = SpaceProcessingModeValues.Preserve });
            elements.Add(instrRun);

            // Separator
            var separatorRun = new Run(new FieldChar { FieldCharType = FieldCharValues.Separate });
            elements.Add(separatorRun);

            // Placeholder waarde
            var valueRun = new Run(new Text("1"));
            elements.Add(valueRun);

            // Einde veld
            var endRun = new Run(new FieldChar { FieldCharType = FieldCharValues.End });
            elements.Add(endRun);

            return elements;
        }

        /// <summary>
        /// Zorgt dat velden automatisch worden bijgewerkt bij openen van het document.
        /// Dit moet worden aangeroepen na document generatie.
        /// </summary>
        public static void EnableAutoUpdateFields(WordprocessingDocument document)
        {
            var settingsPart = document.MainDocumentPart?.DocumentSettingsPart;
            if (settingsPart == null)
            {
                settingsPart = document.MainDocumentPart!.AddNewPart<DocumentSettingsPart>();
                settingsPart.Settings = new Settings();
            }

            var settings = settingsPart.Settings;

            // Verwijder bestaande updateFields als die er is
            var existingUpdateFields = settings.GetFirstChild<UpdateFieldsOnOpen>();
            existingUpdateFields?.Remove();

            // Voeg updateFields toe - dit zorgt dat velden automatisch updaten bij openen
            settings.PrependChild(new UpdateFieldsOnOpen { Val = true });

            settings.Save();
        }
    }
}
