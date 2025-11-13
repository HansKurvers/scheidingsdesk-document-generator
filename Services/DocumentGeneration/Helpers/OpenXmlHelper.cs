using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper class for creating and styling OpenXML Word document elements
    /// Provides reusable building blocks for document construction
    /// </summary>
    public static class OpenXmlHelper
    {
        /// <summary>
        /// Creates a styled table cell with optional formatting
        /// </summary>
        /// <param name="text">Text content for the cell</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <param name="bgColor">Background color (hex without #, e.g., "2E74B5")</param>
        /// <param name="textColor">Text color (hex without #, e.g., "FFFFFF")</param>
        /// <param name="alignment">Text alignment (default: Center)</param>
        /// <param name="fontSize">Font size in half-points (e.g., "20" = 10pt, "18" = 9pt)</param>
        /// <returns>Configured TableCell</returns>
        public static TableCell CreateStyledCell(
            string text,
            bool isBold = false,
            string? bgColor = null,
            string? textColor = null,
            JustificationValues? alignment = null,
            string? fontSize = null)
        {
            var cell = new TableCell();

            // Add cell properties if background color specified
            if (!string.IsNullOrEmpty(bgColor))
            {
                var cellProps = new TableCellProperties();
                var shading = new Shading() { Val = ShadingPatternValues.Clear, Fill = bgColor };
                cellProps.Append(shading);
                cell.Append(cellProps);
            }

            // Create paragraph with alignment
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new Justification() { Val = alignment ?? JustificationValues.Center });
            paragraph.Append(paragraphProps);

            // Create run with text styling
            var run = new Run();
            var runProps = new RunProperties();

            if (isBold)
            {
                runProps.Append(new Bold());
            }

            if (!string.IsNullOrEmpty(textColor))
            {
                runProps.Append(new Color() { Val = textColor });
            }

            if (!string.IsNullOrEmpty(fontSize))
            {
                runProps.Append(new FontSize() { Val = fontSize });
            }

            if (runProps.HasChildren)
            {
                run.Append(runProps);
            }

            run.Append(new Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);

            return cell;
        }

        /// <summary>
        /// Creates a styled heading paragraph
        /// </summary>
        /// <param name="text">Heading text</param>
        /// <param name="fontSize">Font size in half-points (default: 24 = 12pt)</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateStyledHeading(string text, string fontSize = "24")
        {
            var heading = new Paragraph();
            var headingRun = new Run();
            var headingRunProps = new RunProperties();
            headingRunProps.Append(new Bold());
            headingRunProps.Append(new FontSize() { Val = fontSize });
            headingRun.Append(headingRunProps);
            headingRun.Append(new Text(text));
            heading.Append(headingRun);
            return heading;
        }

        /// <summary>
        /// Creates a styled sub-heading (smaller than main heading)
        /// </summary>
        /// <param name="text">Text content for the sub-heading</param>
        /// <param name="fontSize">Font size in half-points (default: 20 = 10pt)</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateStyledSubHeading(string text, string fontSize = "20")
        {
            var heading = new Paragraph();
            var headingRun = new Run();
            var headingRunProps = new RunProperties();
            headingRunProps.Append(new Bold());
            headingRunProps.Append(new FontSize() { Val = fontSize });
            headingRunProps.Append(new Color() { Val = Colors.DarkBlue }); // Add color for distinction
            headingRun.Append(headingRunProps);
            headingRun.Append(new Text(text));
            heading.Append(headingRun);
            return heading;
        }

        /// <summary>
        /// Creates a standard table with modern borders
        /// </summary>
        /// <param name="borderColor">Border color (hex without #)</param>
        /// <param name="columnWidths">Array of column widths</param>
        /// <returns>Configured Table</returns>
        public static Table CreateStyledTable(string borderColor = "2E74B5", int[]? columnWidths = null)
        {
            var table = new Table();

            // Add table properties
            var tblProp = new TableProperties();

            // Set table width to 100%
            var tblWidth = new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct };
            tblProp.Append(tblWidth);

            // Add modern borders
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new LeftBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new RightBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" }
            );
            tblProp.Append(tblBorders);

            // Add table look for better styling
            var tblLook = new TableLook()
            {
                Val = "04A0",
                FirstRow = true,
                LastRow = false,
                FirstColumn = true,
                LastColumn = false,
                NoHorizontalBand = false,
                NoVerticalBand = true
            };
            tblProp.Append(tblLook);

            table.Append(tblProp);

            // Define grid columns if widths provided
            if (columnWidths != null && columnWidths.Length > 0)
            {
                var tblGrid = new TableGrid();
                foreach (var width in columnWidths)
                {
                    tblGrid.Append(new GridColumn() { Width = width.ToString() });
                }
                table.Append(tblGrid);
            }

            return table;
        }

        /// <summary>
        /// Creates a table row with header styling
        /// </summary>
        /// <param name="headerTexts">Array of header text values</param>
        /// <param name="bgColor">Background color for headers</param>
        /// <param name="textColor">Text color for headers</param>
        /// <param name="fontSize">Font size in half-points (e.g., "20" = 10pt, "18" = 9pt)</param>
        /// <returns>Configured TableRow</returns>
        public static TableRow CreateHeaderRow(string[] headerTexts, string bgColor = "2E74B5", string textColor = "FFFFFF", string? fontSize = null)
        {
            var row = new TableRow();

            foreach (var headerText in headerTexts)
            {
                var cell = CreateStyledCell(headerText, isBold: true, bgColor: bgColor, textColor: textColor, fontSize: fontSize);
                row.Append(cell);
            }

            return row;
        }

        /// <summary>
        /// Creates a simple paragraph with text
        /// </summary>
        /// <param name="text">Paragraph text</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateSimpleParagraph(string text, bool isBold = false)
        {
            var paragraph = new Paragraph();
            var run = new Run();

            if (isBold)
            {
                var runProps = new RunProperties();
                runProps.Append(new Bold());
                run.Append(runProps);
            }

            run.Append(new Text(text));
            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Creates an empty paragraph (for spacing)
        /// </summary>
        /// <returns>Empty Paragraph</returns>
        public static Paragraph CreateEmptyParagraph()
        {
            return new Paragraph();
        }

        /// <summary>
        /// Adds a styled run to an existing paragraph
        /// </summary>
        /// <param name="paragraph">Paragraph to add run to</param>
        /// <param name="text">Text content</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <param name="isItalic">Whether text should be italic</param>
        /// <param name="textColor">Text color (hex without #)</param>
        public static void AddStyledRun(
            Paragraph paragraph,
            string text,
            bool isBold = false,
            bool isItalic = false,
            string? textColor = null)
        {
            var run = new Run();
            var runProps = new RunProperties();

            if (isBold)
                runProps.Append(new Bold());

            if (isItalic)
                runProps.Append(new Italic());

            if (!string.IsNullOrEmpty(textColor))
                runProps.Append(new Color() { Val = textColor });

            if (runProps.HasChildren)
                run.Append(runProps);

            run.Append(new Text(text));
            paragraph.Append(run);
        }

        /// <summary>
        /// Color constants for consistent styling
        /// </summary>
        public static class Colors
        {
            public const string Blue = "2E74B5";
            public const string DarkBlue = "4472C4";
            public const string Green = "70AD47";
            public const string Orange = "ED7D31";
            public const string Red = "C00000";
            public const string Gray = "D0D0D0";
            public const string LightGray = "F2F2F2";
            public const string White = "FFFFFF";
            public const string Black = "000000";
        }
    }
}