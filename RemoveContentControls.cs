using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Scheidingsdesk
{
    public class RemoveContentControls
    {
        private readonly ILogger<RemoveContentControls> _logger;

        public RemoveContentControls(ILogger<RemoveContentControls> logger)
        {
            _logger = logger;
        }

        [Function("RemoveContentControls")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to remove content controls.");

            // Get the uploaded file
            var formData = await req.ReadFormAsync();
            var file = formData.Files.GetFile("document") ?? formData.Files.GetFile("file");            
            if (file == null)
            {
                return new BadRequestObjectResult("Please upload a Word document.");
            }

            // Create memory streams for input and output
            using var inputStream = new MemoryStream();
            await file.CopyToAsync(inputStream);
            inputStream.Position = 0;
            
            using var outputStream = new MemoryStream();
            
            // Process the document
            try
            {  
                // Process the document directly from the input stream
                using (WordprocessingDocument doc = WordprocessingDocument.Open(inputStream, false))
                {
                    _logger.LogInformation("Document opened successfully.");
                    
                    // Create a memory copy with the document to process
                    using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, doc.DocumentType))
                    {
                        // Import all parts from the original document
                        foreach (var part in doc.Parts)
                        {
                            outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                        }
                        
                        // Get the main document part
                        var mainPart = outputDoc.MainDocumentPart;
                        if (mainPart != null)
                        {
                            // Process the document with article/subsection logic
                            ProcessDocumentWithArticles(mainPart.Document);
                            
                            // Save the changes
                            mainPart.Document.Save();
                            _logger.LogInformation("Document processed successfully.");
                        }
                    }
                }
                
                // Reset the position for reading
                outputStream.Position = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing document: {ex.Message}");
                return new ObjectResult($"Error processing document: {ex.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        
            // Return the processed document
            return new FileContentResult(outputStream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                FileDownloadName = "ProcessedDocument.docx"
            };
        }

        private void ProcessDocumentWithArticles(Document document)
        {
            var body = document.Body;
            if (body == null) return;

            // Step 1: Identify and process articles
            var articles = IdentifyArticles(body);
            var articlesToKeep = new List<Article>();

            foreach (var article in articles)
            {
                // Process content controls within this article
                var subsections = ProcessArticleContentControls(article);
                
                if (subsections.Count > 0)
                {
                    // Article has valid subsections, keep it
                    article.Subsections = subsections;
                    articlesToKeep.Add(article);
                }
                else
                {
                    // Article has no valid subsections, mark for removal
                    RemoveArticleContent(article);
                }
            }

            // Step 2: Rebuild document with renumbered articles
            RebuildDocumentWithArticles(body, articlesToKeep);
        }

        private List<Article> IdentifyArticles(Body body)
        {
            var articles = new List<Article>();
            var elements = body.ChildElements.ToList();
            
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] is Paragraph paragraph)
                {
                    var text = GetParagraphText(paragraph);
                    var articleMatch = Regex.Match(text, @"^(Artikel|Article)\s+(\d+)\.?\s*(.*)$", RegexOptions.IgnoreCase);
                    
                    if (articleMatch.Success)
                    {
                        var article = new Article
                        {
                            OriginalNumber = int.Parse(articleMatch.Groups[2].Value),
                            TitleParagraph = paragraph,
                            Title = articleMatch.Groups[3].Value,
                            ContentElements = new List<OpenXmlElement>()
                        };
                        
                        // Collect all elements until the next article
                        for (int j = i + 1; j < elements.Count; j++)
                        {
                            var nextText = elements[j] is Paragraph p ? GetParagraphText(p) : "";
                            if (Regex.IsMatch(nextText, @"^(Artikel|Article)\s+\d+", RegexOptions.IgnoreCase))
                            {
                                break;
                            }
                            article.ContentElements.Add(elements[j]);
                        }
                        
                        articles.Add(article);
                    }
                }
            }
            
            return articles;
        }

        private List<Subsection> ProcessArticleContentControls(Article article)
        {
            var subsections = new List<Subsection>();
            var subsectionNumber = 1;

            // Process each content element in the article
            foreach (var element in article.ContentElements.ToList())
            {
                // Find all content controls in this element
                var sdtElements = element.Descendants<SdtElement>().ToList();
                
                foreach (var sdt in sdtElements)
                {
                    var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                    if (contentElement == null) continue;
                    
                    // Get the text content
                    var textContent = string.Join("", contentElement.Descendants<Text>().Select(t => t.Text ?? "")).Trim();
                    
                    if (!string.IsNullOrWhiteSpace(textContent))
                    {
                        // Create a subsection
                        var subsection = new Subsection
                        {
                            Number = subsectionNumber++,
                            Content = textContent,
                            OriginalElement = sdt
                        };
                        subsections.Add(subsection);
                    }
                }
            }

            return subsections;
        }

        private void RemoveArticleContent(Article article)
        {
            // Remove the article title paragraph
            article.TitleParagraph?.Remove();
            
            // Remove all content elements
            foreach (var element in article.ContentElements)
            {
                element?.Remove();
            }
        }

        private void RebuildDocumentWithArticles(Body body, List<Article> articlesToKeep)
        {
            // Clear existing article content (already removed)
            // Now rebuild with proper numbering
            
            var articleNumber = 1;
            foreach (var article in articlesToKeep)
            {
                // Update article title with new number
                var titleParagraph = article.TitleParagraph;
                if (titleParagraph != null)
                {
                    var firstText = titleParagraph.Descendants<Text>().FirstOrDefault();
                    if (firstText != null)
                    {
                        firstText.Text = Regex.Replace(firstText.Text, 
                            @"^(Artikel|Article)\s+\d+", 
                            $"$1 {articleNumber}", 
                            RegexOptions.IgnoreCase);
                    }
                }

                // Create subsection paragraphs
                foreach (var subsection in article.Subsections)
                {
                    var subsectionParagraph = CreateSubsectionParagraph(articleNumber, subsection.Number, subsection.Content);
                    
                    // Find where to insert the subsection
                    var insertAfter = article.TitleParagraph;
                    if (subsection.Number > 1 && article.Subsections.Count >= subsection.Number - 1)
                    {
                        // Insert after previous subsection
                        insertAfter = body.Descendants<Paragraph>()
                            .FirstOrDefault(p => GetParagraphText(p).StartsWith($"{articleNumber}.{subsection.Number - 1}"));
                    }
                    
                    if (insertAfter != null)
                    {
                        insertAfter.InsertAfterSelf(subsectionParagraph);
                    }
                }

                // Remove original content controls
                foreach (var element in article.ContentElements.ToList())
                {
                    var sdts = element.Descendants<SdtElement>().ToList();
                    foreach (var sdt in sdts)
                    {
                        sdt.Remove();
                    }
                    
                    // Remove empty paragraphs
                    if (element is Paragraph p && string.IsNullOrWhiteSpace(GetParagraphText(p)))
                    {
                        element.Remove();
                    }
                }

                articleNumber++;
            }
        }

        private Paragraph CreateSubsectionParagraph(int articleNumber, int subsectionNumber, string content)
        {
            var paragraph = new Paragraph();
            var run = new Run();
            var text = new Text($"{articleNumber}.{subsectionNumber} {content}");
            
            run.Append(text);
            paragraph.Append(run);
            
            return paragraph;
        }

        private string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text ?? "")).Trim();
        }

        private class Article
        {
            public int OriginalNumber { get; set; }
            public Paragraph TitleParagraph { get; set; }
            public string Title { get; set; }
            public List<OpenXmlElement> ContentElements { get; set; }
            public List<Subsection> Subsections { get; set; }
        }

        private class Subsection
        {
            public int Number { get; set; }
            public string Content { get; set; }
            public SdtElement OriginalElement { get; set; }
        }
    }
}