using System;
using Microsoft.Build.Utilities;

namespace Xamarin.iOS.ResourceDesigner
{
    public static class LoggerExtensions
    {
        public static void LogWarningEx(this TaskLoggingHelper logger, string message, string? filePath = null)
        {
            logger.LogWarning(
                subcategory: null,
                warningCode: null,
                helpKeyword: null,
                file: filePath,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message);
        }
    }
}

