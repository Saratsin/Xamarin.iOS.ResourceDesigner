using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text;
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

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, "Started Resource.designer.cs creation :D");

            var parser = new ResourceParser(Log, ResourceDesignerFilePath!);
            var assets = ImageAssets!.Select(asset => Path.GetFullPath(asset.ItemSpec));
            var resourceDesignerFileString = parser.Parse(ProjectNamespace!, assets);

            File.WriteAllText(ResourceDesignerFilePath!, resourceDesignerFileString);

            Log.LogMessage(MessageImportance.Normal, "Finished Resource.designer.cs creation");

            return !Log.HasLoggedErrors;
        }
    }
}
