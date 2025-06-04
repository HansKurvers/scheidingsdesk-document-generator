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

                // Read the body as bytes
                using var memoryStream = new MemoryStream();
                await request.Body.CopyToAsync(memoryStream);
                var bodyBytes = memoryStream.ToArray();
                
                if (bodyBytes.Length == 0)
                {
                    return new MultipartFormData { Files = new List<MultipartFile>() };
                }
                
                var files = new List<MultipartFile>();
                
                // Parse using byte operations for better reliability
                var boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);
                var positions = FindBoundaryPositions(bodyBytes, boundaryBytes);
                
                for (int i = 0; i < positions.Count - 1; i++)
                {
                    var startPos = positions[i] + boundaryBytes.Length;
                    var endPos = positions[i + 1];
                    
                    if (startPos >= endPos) continue;
                    
                    // Skip CRLF after boundary
                    if (startPos + 2 < bodyBytes.Length && bodyBytes[startPos] == 13 && bodyBytes[startPos + 1] == 10)
                    {
                        startPos += 2;
                    }
                    
                    // Find headers end (double CRLF)
                    var headerEndPos = FindDoubleNewline(bodyBytes, startPos, endPos);
                    if (headerEndPos == -1) continue;
                    
                    // Parse headers
                    var headerBytes = new byte[headerEndPos - startPos];
                    Array.Copy(bodyBytes, startPos, headerBytes, 0, headerBytes.Length);
                    var headers = Encoding.UTF8.GetString(headerBytes);
                    
                    // Check if this part contains a file
                    if (headers.Contains("Content-Disposition") && headers.Contains("filename="))
                    {
                        var name = ExtractValue(headers, "name=\"", "\"");
                        var filename = ExtractValue(headers, "filename=\"", "\"");
                        
                        // Content starts after double CRLF
                        var contentStart = headerEndPos + 4; // Skip \r\n\r\n
                        var contentEnd = endPos - 2; // Skip \r\n before next boundary
                        
                        if (contentStart < contentEnd && contentEnd <= bodyBytes.Length)
                        {
                            var contentLength = contentEnd - contentStart;
                            var fileData = new byte[contentLength];
                            Array.Copy(bodyBytes, contentStart, fileData, 0, contentLength);
                            
                            files.Add(new MultipartFile
                            {
                                Name = name,
                                FileName = filename,
                                Data = fileData
                            });
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
        
        private static List<int> FindBoundaryPositions(byte[] data, byte[] boundary)
        {
            var positions = new List<int>();
            for (int i = 0; i <= data.Length - boundary.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < boundary.Length; j++)
                {
                    if (data[i + j] != boundary[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    positions.Add(i);
                }
            }
            return positions;
        }
        
        private static int FindDoubleNewline(byte[] data, int start, int end)
        {
            // Look for \r\n\r\n
            for (int i = start; i < end - 3; i++)
            {
                if (data[i] == 13 && data[i + 1] == 10 && data[i + 2] == 13 && data[i + 3] == 10)
                {
                    return i;
                }
            }
            // Also check for \n\n
            for (int i = start; i < end - 1; i++)
            {
                if (data[i] == 10 && data[i + 1] == 10)
                {
                    return i;
                }
            }
            return -1;
        }
        
        private static string ExtractValue(string text, string startMarker, string endMarker)
        {
            var startIndex = text.IndexOf(startMarker);
            if (startIndex == -1) return null;
            
            startIndex += startMarker.Length;
            var endIndex = text.IndexOf(endMarker, startIndex);
            if (endIndex == -1) return null;
            
            return text.Substring(startIndex, endIndex - startIndex);
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