namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ImageAssetsRawDto
    {
        public string[] ImageAssetPaths { get; init; } = { };
        public string[] TrimmingPrefixes { get; init; } = { };
        public char[] FilenamesSeparatorChars { get; init; } = { };
    }
}
