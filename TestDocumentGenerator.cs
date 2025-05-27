using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Scheidingsdesk
{
    public class TestDocumentGenerator
    {
        public static void CreateTestDocument(string filePath)
        {
            using (var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Title
                AddParagraph(body, "DIVORCE AGREEMENT", true);

                // Article 1 with ^ marker - should be removed entirely
                AddParagraphWithContentControl(body, "1. KINDEREN EN GEZAG", "^");
                AddParagraph(body, "   1.1 Namen kinderen: Jan en Marie");
                AddParagraph(body, "   1.2 Gezagsregeling: Co-ouderschap");
                AddParagraph(body, "   1.3 Omgangsregeling: Om de week");

                // Article 2 - should become Article 1
                AddParagraph(body, "2. PARTNERALIMENTATIE");
                AddParagraph(body, "   2.1 Maandelijks bedrag: €1500");
                AddParagraphWithContentControl(body, "   2.2 Duur: 5 jaar", "#"); // Should be removed
                AddParagraph(body, "   2.3 Indexering: Jaarlijks");

                // Article 3 - should become Article 2
                AddParagraph(body, "3. VERMOGENSVERDELING");
                AddParagraph(body, "   3.1 Woning: Verkoop en 50/50 verdeling");
                AddParagraph(body, "   3.2 Auto: Naar partner A");

                mainPart.Document.Save();
            }
        }

        private static void AddParagraph(Body body, string text, bool isBold = false)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            
            if (isBold)
            {
                run.AppendChild(new RunProperties(new Bold()));
            }
            
            run.AppendChild(new Text(text));
        }

        private static void AddParagraphWithContentControl(Body body, string text, string contentControlText)
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text + " "));
            
            // Add content control
            var sdt = para.AppendChild(new SdtRun());
            var sdtContent = sdt.AppendChild(new SdtContentRun());
            var ccRun = sdtContent.AppendChild(new Run());
            ccRun.AppendChild(new Text(contentControlText));
        }
    }
}