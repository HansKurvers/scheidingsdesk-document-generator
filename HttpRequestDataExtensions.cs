using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;

namespace Scheidingsdesk
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<MultipartFormData> ParseMultipartAsync(this HttpRequestData request)
        {
            try
            {
                string contentType = string.Empty;
                if (request.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    contentType = contentTypeValues.FirstOrDefault() ?? string.Empty;
                }
                
                var boundary = GetBoundary(contentType);
                
                if (string.IsNullOrEmpty(boundary))
                {
                    return new MultipartFormData { Files = new List<MultipartFile>() };
                }

            // Read the body as bytes first
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);
            var bodyBytes = memoryStream.ToArray();
            
            // Try to parse as text
            var content = Encoding.UTF8.GetString(bodyBytes);
            
            var files = new List<MultipartFile>();
            var parts = content.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                if (part.Trim() == "--" || string.IsNullOrWhiteSpace(part))
                    continue;
                    
                var lines = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var contentDisposition = lines.FirstOrDefault(l => l.StartsWith("Content-Disposition:", StringComparison.OrdinalIgnoreCase));
                
                if (contentDisposition != null && contentDisposition.Contains("filename="))
                {
                    var name = GetValue(contentDisposition, "name");
                    var filename = GetValue(contentDisposition, "filename");
                    
                    // Find the content start position in the original bytes
                    var headerEnd = content.IndexOf("\r\n\r\n", content.IndexOf(contentDisposition));
                    if (headerEnd == -1)
                        headerEnd = content.IndexOf("\n\n", content.IndexOf(contentDisposition));
                        
                    if (headerEnd > 0)
                    {
                        // Calculate byte position for binary data extraction
                        var headerBytes = Encoding.UTF8.GetByteCount(content.Substring(0, headerEnd + 4));
                        var nextBoundaryIndex = content.IndexOf("--" + boundary, headerEnd + 4);
                        
                        if (nextBoundaryIndex > 0)
                        {
                            var contentLength = Encoding.UTF8.GetByteCount(content.Substring(0, nextBoundaryIndex)) - headerBytes - 2; // -2 for \r\n
                            var fileData = new byte[contentLength];
                            Array.Copy(bodyBytes, headerBytes, fileData, 0, contentLength);
                            
                            files.Add(new MultipartFile
                            {
                                Name = name,
                                FileName = filename,
                                Data = fileData
                            });
                        }
                    }
                }
            }
            
                return new MultipartFormData { Files = files };
            }
            catch (Exception ex)
            {
                // Return empty result on error
                return new MultipartFormData { Files = new List<MultipartFile>() };
            }
        }
        
        private static string GetBoundary(string contentType)
        {
            var elements = contentType.Split(';');
            var boundaryElement = elements.FirstOrDefault(e => e.Trim().StartsWith("boundary="));
            
            if (boundaryElement != null)
            {
                var boundary = boundaryElement.Split('=')[1].Trim();
                return boundary.Trim('"');
            }
            
            return null;
        }
        
        private static string GetValue(string contentDisposition, string key)
        {
            var parts = contentDisposition.Split(';');
            var part = parts.FirstOrDefault(p => p.Trim().StartsWith(key + "="));
            
            if (part != null)
            {
                var value = part.Split('=')[1].Trim();
                return value.Trim('"');
            }
            
            return null;
        }
    }
    
    public class MultipartFormData
    {
        public List<MultipartFile> Files { get; set; }
    }
    
    public class MultipartFile
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
    }
}