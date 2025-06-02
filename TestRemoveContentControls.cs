using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Scheidingsdesk
{
    class TestRemoveContentControls
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run --project . -- <input_file_path> [output_file_path]");
                Console.WriteLine("Example: dotnet run --project . -- test.docx processed_test.docx");
                return;
            }

            string inputPath = args[0];
            string? outputPath = args.Length > 1 ? args[1] : null;

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: File '{inputPath}' not found.");
                return;
            }

            try
            {
                ProcessDocument(inputPath, outputPath);
                Console.WriteLine("Processing completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing failed: {ex.Message}");
            }
        }

        static void ProcessDocument(string inputFilePath, string? outputFilePath = null)
        {
            outputFilePath ??= Path.Combine(
                Path.GetDirectoryName(inputFilePath) ?? ".",
                Path.GetFileNameWithoutExtension(inputFilePath) + "_processed" + Path.GetExtension(inputFilePath)
            );

            Console.WriteLine($"Processing document: {inputFilePath}");
            Console.WriteLine($"Output will be saved to: {outputFilePath}");

            byte[] fileContent = File.ReadAllBytes(inputFilePath);
            
            if (fileContent.Length == 0)
            {
                throw new InvalidOperationException("File is empty or could not be read.");
            }

            using var inputStream = new MemoryStream(fileContent);
            using var outputStream = new MemoryStream();
            
            using (WordprocessingDocument doc = WordprocessingDocument.Open(inputStream, false))
            {
                Console.WriteLine("Document opened successfully.");
                
                using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, doc.DocumentType))
                {
                    foreach (var part in doc.Parts)
                    {
                        outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                    }
                    
                    var mainPart = outputDoc.MainDocumentPart;
                    if (mainPart != null)
                    {
                        RemoveEmptyArticles(mainPart.Document);
                        ProcessContentControls(mainPart.Document);
                        mainPart.Document.Save();
                        Console.WriteLine("Content controls processed successfully.");
                    }
                }
            }
            
            outputStream.Position = 0;
            File.WriteAllBytes(outputFilePath, outputStream.ToArray());
            Console.WriteLine($"Document saved successfully to: {outputFilePath}");
        }

        static void RemoveEmptyArticles(OpenXmlElement element)
        {
            Console.WriteLine("Scanning for empty or placeholder content controls to remove...");
            
            var sdtElements = element.Descendants<SdtElement>().ToList();
            
            Console.WriteLine($"Found {sdtElements.Count} content controls to analyze.");
            
            foreach (var sdt in sdtElements)
            {
                var contentText = GetSdtContentText(sdt);
                
                // Check if content control contains "#" or is empty/whitespace
                if (string.IsNullOrWhiteSpace(contentText) || contentText.Contains('#'))
                {
                    Console.WriteLine($"Found problematic content control: '{contentText}'");
                    
                    // Instead of removing parent elements, just replace the content control with empty content
                    // This is much safer and more predictable
                    ReplaceContentControlWithEmpty(sdt);
                }
            }
            
            Console.WriteLine($"Processed problematic content controls by clearing their content.");
        }
        
        static void ReplaceContentControlWithEmpty(SdtElement sdt)
        {
            try
            {
                // Find the content element within the SDT
                var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElement != null)
                {
                    // Clear all content from the SDT but keep the structure
                    contentElement.RemoveAllChildren();
                    Console.WriteLine("Cleared content control content");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear content control: {ex.Message}");
            }
        }
        
        
        
        
        static string GetSdtContentText(SdtElement sdt)
        {
            var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElements == null) return "";
            
            return contentElements.Descendants<Text>().Aggregate("", (current, text) => current + text.Text);
        }
        
        static void ProcessContentControls(OpenXmlElement element)
        {
            var sdtElements = element.Descendants<SdtElement>().ToList();
            Console.WriteLine($"Found {sdtElements.Count} content controls to process.");
            
            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;
                
                var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElements == null) continue;
                
                var contentToPreserve = contentElements.ChildElements.ToList();
                
                if (contentToPreserve.Count > 0)
                {
                    foreach (var child in contentToPreserve)
                    {
                        var clonedChild = child.CloneNode(true);
                        
                        foreach (var run in clonedChild.Descendants<Run>())
                        {
                            var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());
                            
                            var colorElements = runProps.Elements<Color>().ToList();
                            foreach (var color in colorElements)
                            {
                                runProps.RemoveChild(color);
                            }
                            
                            runProps.AppendChild(new Color() { Val = "000000" });
                            
                            var shadingElements = runProps.Elements<Shading>().ToList();
                            foreach (var shading in shadingElements)
                            {
                                runProps.RemoveChild(shading);
                            }
                        }
                        
                        parent.InsertBefore(clonedChild, sdt);
                    }
                }
                
                parent.RemoveChild(sdt);
            }
        }
    }
}