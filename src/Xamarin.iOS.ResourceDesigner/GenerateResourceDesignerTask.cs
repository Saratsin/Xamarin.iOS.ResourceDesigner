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
        public ITaskItem[]? Assets { get; set; }

        [Required]
        public ITaskItem[]? InterfaceDefinitions { get; set; }

        [Required]
        public string? ResourceDesignerFilePath { get; set; }

        [Required]
        public string? ProjectNamespace { get; set; }
        
        [Required]
        public string? ColorSetsTrimmingPrefixes { get; set; }

        [Required]
        public string? ImageSetsTrimmingPrefixes { get; set; }
        
        [Required]
        public string? ColorSetsFilenamesSeparatorChars { get; set; }

        [Required]
        public string? ImageSetsFilenamesSeparatorChars { get; set; }

        public override bool Execute()
        {
            var resourceDesignerFilename = Path.GetFileName(ResourceDesignerFilePath!);
            Log.LogMessage($"Started {resourceDesignerFilename} creation :D");
            
            // NOTE Filtering asset paths only to those that we support
            var assetPaths = Assets!
                .Select(a => a.GetMetadata("FullPath"))
                .Where(p => Path.GetFileName(p) == "Contents.json")
                .Select(p => Directory.GetParent(p)!.FullName)
                .Where(path => !string.IsNullOrEmpty(Path.GetExtension(path)))
                .ToLookup(Path.GetExtension);
            
            var images = new ImageSetsRawDto
            {
                ImageSetPaths = assetPaths[".imageset"].ToArray(),
                TrimmingPrefixes = ImageSetsTrimmingPrefixes!.Split('|'),
                FilenamesSeparatorChars = ImageSetsFilenamesSeparatorChars!.ToCharArray()
            };
            
            var colors = new ColorSetsRawDto
            {
                ColorSetPaths = assetPaths[".colorset"].ToArray(),
                TrimmingPrefixes = ColorSetsTrimmingPrefixes!.Split('|'),
                FilenamesSeparatorChars = ColorSetsFilenamesSeparatorChars!.ToCharArray()
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
