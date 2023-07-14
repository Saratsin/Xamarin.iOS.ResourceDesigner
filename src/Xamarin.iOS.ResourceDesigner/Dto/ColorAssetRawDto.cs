namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ColorAssetRawDto
    {
        public string[] ColorAssetPaths { get; init; } = { };
        public string[] TrimmingPrefixes { get; init; } = { };
        public char[] FilenamesSeparatorChars { get; init; } = { };
    }
}
