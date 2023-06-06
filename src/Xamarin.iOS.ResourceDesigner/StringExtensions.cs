using System;
using System.Linq;

namespace Xamarin.iOS.ResourceDesigner
{
    public static class StringExtensions
    {
        public static string AppendToEachLine(this string text, string appendix)
        {
            var lines = text.Split('\n');
            var appendedLines = lines.Select(line => appendix + line);
            var appendedText = string.Join("\n", appendedLines);

            return appendedText;
        }
    }
}