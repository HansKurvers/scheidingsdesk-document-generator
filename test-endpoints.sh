#!/bin/bash

# Test script for Azure Functions endpoints

echo "Testing Azure Functions Document Processor"
echo "========================================="

# Test health check
echo -e "\n1. Testing Health Check endpoint..."
curl -X GET http://localhost:7071/api/health -v

# Test process endpoint without file (should return error)
echo -e "\n\n2. Testing Process endpoint without file (expecting error)..."
curl -X POST http://localhost:7071/api/process \
  -H "x-functions-key: test" \
  -H "Content-Type: multipart/form-data" \
  -v

# Test process endpoint with empty form data
echo -e "\n\n3. Testing Process endpoint with empty form data (expecting error)..."
curl -X POST http://localhost:7071/api/process \
  -H "x-functions-key: test" \
  -F "dummy=test" \
  -v

# Test with sample file (if exists)
if [ -f "sample.docx" ]; then
    echo -e "\n\n4. Testing Process endpoint with sample file..."
    curl -X POST http://localhost:7071/api/process \
      -H "x-functions-key: test" \
      -F "document=@sample.docx" \
      -o processed.docx \
      -v
    echo -e "\nProcessed file saved as: processed.docx"
else
    echo -e "\n\n4. Skipping file test (create sample.docx to test file processing)"
fi

echo -e "\n\nTest completed!"