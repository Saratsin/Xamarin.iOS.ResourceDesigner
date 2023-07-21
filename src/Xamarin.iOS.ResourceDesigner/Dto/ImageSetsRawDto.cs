namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ImageSetsRawDto
    {
        public string[] ImageSetPaths { get; init; } = { };
        public string[] TrimmingPrefixes { get; init; } = { };
        public char[] FilenamesSeparatorChars { get; init; } = { };
    }
}
