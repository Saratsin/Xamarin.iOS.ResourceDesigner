using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.iOS.ResourceDesigner
{
    public class GenerateResourceDesignerTask : Task
    {
        [Required]
        public ITaskItem[]? ImageAssets { get; set; }

        [Required]
        public string? ResourceDesignerFilePath { get; set; }

        [Required]
        public string? ProjectNamespace { get; set; }

        [Required]
        public string? ImageAssetsTrimmingPrefixes { get; set; }

        [Required]
        public string? ImageAssetFilenamesSeparatorChars { get; set; }

        public override bool Execute()
        {
            var resourceDesignerFilename = Path.GetFileName(ResourceDesignerFilePath);
            Log.LogMessage($"Started {resourceDesignerFilename} creation :D");

            var imageAssetFilePaths = ImageAssets!.Select(asset => asset.GetMetadata("FullPath")).ToArray();
            var imageAssetsTrimmingPrefixes = ImageAssetsTrimmingPrefixes!.Split('|');
            var imageAssetFilenamesSeparatorChars = ImageAssetFilenamesSeparatorChars!.ToCharArray();

            var parser = new ResourceParser(Log);
            var resourceDesignerFileString = parser.Parse(
                ProjectNamespace!,
                imageAssetFilePaths,
                imageAssetsTrimmingPrefixes,
                imageAssetFilenamesSeparatorChars,
                ResourceDesignerFilePath!);

            File.WriteAllText(ResourceDesignerFilePath!, resourceDesignerFileString);

            Log.LogMessage($"Finished {resourceDesignerFilename} creation");

            return !Log.HasLoggedErrors;
        }
    }
}
