using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xamarin.iOS.ResourceDesigner.Dto;

namespace Xamarin.iOS.ResourceDesigner
{
    public class GenerateResourceDesignerTask : Task
    {
        [Required]
        public ITaskItem[]? ImageAssets { get; set; }
        
        [Required]
        public ITaskItem[]? ColorAssets { get; set; }

        [Required]
        public ITaskItem[]? InterfaceDefinitions { get; set; }

        [Required]
        public string? ResourceDesignerFilePath { get; set; }

        [Required]
        public string? ProjectNamespace { get; set; }
        
        [Required]
        public string? ColorAssetsTrimmingPrefixes { get; set; }

        [Required]
        public string? ImageAssetsTrimmingPrefixes { get; set; }
        
        [Required]
        public string? ColorAssetFilenamesSeparatorChars { get; set; }

        [Required]
        public string? ImageAssetFilenamesSeparatorChars { get; set; }

        public override bool Execute()
        {
            var resourceDesignerFilename = Path.GetFileName(ResourceDesignerFilePath!);
            Log.LogMessage($"Started {resourceDesignerFilename} creation :D");

            var images = new ImageAssetsRawDto
            {
                ImageAssetPaths = ImageAssets!.Select(asset => asset.GetMetadata("FullPath")).ToArray(),
                TrimmingPrefixes = ImageAssetsTrimmingPrefixes!.Split('|'),
                FilenamesSeparatorChars = ImageAssetFilenamesSeparatorChars!.ToCharArray()
            };
            
            var colors = new ColorAssetRawDto
            {
                ColorAssetPaths = ColorAssets!.Select(asset => asset.GetMetadata("FullPath")).ToArray(),
                TrimmingPrefixes = ColorAssetsTrimmingPrefixes!.Split('|'),
                FilenamesSeparatorChars = ColorAssetFilenamesSeparatorChars!.ToCharArray()
            };

            var interfaceDefinitions = new InterfaceDefinitionsRawDto
            {
                InterfaceDefinitionPaths = InterfaceDefinitions!.Select(interfaceDefinition => interfaceDefinition.GetMetadata("FullPath")).ToArray()
            };

            var parser = new ResourceParser(Log);
            var resourceDesignerFileString = parser.Parse(
                ProjectNamespace!,
                images,
                colors,
                interfaceDefinitions,
                ResourceDesignerFilePath!);

            File.WriteAllText(ResourceDesignerFilePath!, resourceDesignerFileString);

            Log.LogMessage($"Finished {resourceDesignerFilename} creation");

            return !Log.HasLoggedErrors;
        }
    }
}
