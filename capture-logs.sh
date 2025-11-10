#!/bin/bash

# Script to capture Azure Functions logs with timestamps
# This will help debug the 500 error by capturing the full exception details

LOG_FILE="func-logs-$(date +%Y%m%d-%H%M%S).log"

echo "Starting Azure Functions with log capture..."
echo "Logs will be saved to: $LOG_FILE"
echo "Press Ctrl+C to stop"
echo "----------------------------------------"

# Run func start and capture both stdout and stderr
func start 2>&1 | tee "$LOG_FILE"

echo ""
echo "Function stopped. Logs saved to: $LOG_FILE"
echo ""
echo "To search for errors in the log file, use:"
echo "  grep -i error $LOG_FILE"
echo "  grep -i exception $LOG_FILE"
echo "  grep -i 'correlation' $LOG_FILE"