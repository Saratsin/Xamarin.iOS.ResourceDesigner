namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ColorSetsRawDto
    {
        public string[] ColorSetPaths { get; init; } = { };
        public string[] TrimmingPrefixes { get; init; } = { };
        public char[] FilenamesSeparatorChars { get; init; } = { };
    }
}
