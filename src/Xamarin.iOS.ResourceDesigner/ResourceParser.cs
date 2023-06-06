using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using Stubble.Core;

namespace Xamarin.iOS.ResourceDesigner
{
    public class ResourceParser
    {
        private static readonly string[] TrimmingStarts = { "ic", "img" };
        private readonly TaskLoggingHelper Log;
        private readonly string ResourceDesignerFilePath;
        private readonly string Template;

        public ResourceParser(TaskLoggingHelper log, string resourceDesignerFilePath)
        {
            Log = log;
            ResourceDesignerFilePath = resourceDesignerFilePath;

            using var templateStream = GetType().Assembly.GetManifestResourceStream("Xamarin.iOS.ResourceDesigner.Resources.Resource.designer.mustache");
            using var templateStreamReader = new StreamReader(templateStream);

            Template = templateStreamReader.ReadToEnd();
        }


        public string Parse(string projectNamespace, IEnumerable<string> imageAssets)
        {
            var assets = imageAssets
                .Where(path => Path.GetFileName(path) == "Contents.json")
                .Select(path => Directory.GetParent(path).FullName)
                .Where(path => !string.IsNullOrEmpty(Path.GetExtension(path)))
                .Where(path => Path.GetExtension(path).TrimStart('.') != "xcassets")
                .GroupBy(assetPath => Path.GetExtension(assetPath).TrimStart('.'), assetPath => assetPath)
                .Where(group => group.Any() && group.Key == "imageset");

            Log.LogMessage("Will start to build resources file");

            var duplicates = new Dictionary<string, List<string>>();
            var ambigiousPropertyNames = new Dictionary<string, List<string>>();

            var resourcesDto = new ResourcesDto
            {
                Namespace = projectNamespace,
                ClassesList = assets.Select(assetGroup =>
                {
                    var className = GetClassName(assetGroup.Key);
                    var itemType = GetItemType(className);
                    var lazyFieldsList = new List<LazyFieldDto>();
                    var propertiesList = new List<PropertyDto>();

                    foreach (var assetPath in assetGroup)
                    {
                        var assetName = Path.GetFileNameWithoutExtension(assetPath);
                        if (lazyFieldsList.Any(field => field.ResourceId == assetName))
                        {
                            if (!duplicates.TryGetValue(assetName, out var assetDuplicates))
                            {
                                duplicates[assetName] = new()
                                {
                                    lazyFieldsList.Single(field => field.ResourceId == assetName).ResourcePath!
                                };
                            }

                            duplicates[assetName].Add(assetPath);

                            continue;
                        }

                        var propertyName = GetPropertyName(assetName);
                        if (propertiesList.Any(property => property.PropertyName == propertyName))
                        {
                            if (!ambigiousPropertyNames.TryGetValue(propertyName, out var ambiguities))
                            {
                                ambigiousPropertyNames[propertyName] = new()
                                {
                                    propertiesList.Single(property => property.PropertyName == propertyName).ResourcePath!
                                };
                            }

                            ambigiousPropertyNames[propertyName].Add(assetPath);

                            var suffix = ambigiousPropertyNames[propertyName].Count - 1;
                            propertyName = $"{propertyName}_{suffix}";
                        }

                        if (propertyName == className)
                        {
                            propertyName = $"{propertyName}Resource";
                            Log.LogWarning(
                                subcategory: null,
                                warningCode: null,
                                helpKeyword: null,
                                file: ResourceDesignerFilePath,
                                lineNumber: 0,
                                columnNumber: 0,
                                endLineNumber: 0,
                                endColumnNumber: 0,
                                message: $"Asset {assetName} produces the same property name as its enclosing class.\nAdding suffix in order to avoid compilation error.\nPropery name will be {propertyName}");
                        }

                        var lazyFieldName = GetLazyFieldName(propertyName);
                        var createItemAction = GetCreateItemAction(className, assetName);

                        lazyFieldsList.Add(new()
                        {
                            ItemType = itemType,
                            FieldName = lazyFieldName,
                            CreateItemAction = createItemAction,
                            ResourceId = assetName,
                            ResourcePath = assetPath
                        });

                        propertiesList.Add(new()
                        {
                            ItemType = itemType,
                            FieldName = lazyFieldName,
                            PropertyName = propertyName,
                            ResourcePath = assetPath
                        }) ;
                    }

                    return new ClassDto
                    {
                        ClassName = className,
                        LazyFieldsList = lazyFieldsList,
                        PropertiesList = propertiesList
                    };
                }).ToList()
            };

            foreach (var duplicateList in duplicates)
            {
                var assetsPathesString = string.Join("\n", duplicateList.Value);
                Log.LogWarning(
                    subcategory: null,
                    warningCode: null,
                    helpKeyword: null,
                    file: ResourceDesignerFilePath,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: $"Asset {duplicateList.Key} has {duplicateList.Value.Count - 1} duplicates.\nHere is the full list with pathes to all identical assets:\n{assetsPathesString}");
            }

            foreach (var ambigiousPropertyNameList in ambigiousPropertyNames)
            {
                var propertyName = ambigiousPropertyNameList.Key;
                var ambiguities = ambigiousPropertyNameList.Value;
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Property {propertyName} was ambigious and could be related to several assets.");
                stringBuilder.AppendLine("Due to this fact, first asset was named as is, and the following amigious asset properties were appended with number suffix.");
                stringBuilder.AppendLine("Here is the mapping table of the ambigious assets to their property names:");
                stringBuilder.AppendLine($"{propertyName} => {Path.GetFileNameWithoutExtension(ambiguities[0])} ({ambiguities[0]})");
                for (var i = 1; i < ambiguities.Count; ++i)
                {
                    stringBuilder.AppendLine($"{propertyName}_{i} => {Path.GetFileNameWithoutExtension(ambiguities[i])} ({ambiguities[i]})");
                }

                Log.LogWarning(
                    subcategory: null,
                    warningCode: null,
                    helpKeyword: null,
                    file: ResourceDesignerFilePath,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: stringBuilder.ToString());
            }

            return StaticStubbleRenderer.Render(Template, resourcesDto);
        }
        
        private string GetClassName(string assetType)
        {
            return assetType switch
            {
                "imageset" => "Image",
                _ => char.ToUpperInvariant(assetType[0]) + assetType.Substring(1)
            };
        }

        private string GetItemType(string className)
        {
            return className switch
            {
                "Image" => "UIImage",
                _ => throw new NotSupportedException($"Not supported class: {className}")
            };
        }

        private string GetCreateItemAction(string className, string assetName)
        {
            return className switch
            {
                "Image" => $"() => UIImage.FromBundle(\"{assetName}\")",
                _ => throw new NotSupportedException($"Not supported class: {className}")
            };
        }


        private string GetLazyFieldName(string propertyName)
        {
            return "_" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1) + "Lazy";
        }

        private string GetPropertyName(string assetName)
        {
            var propertyNameParts = assetName
                .Split(new[] { "_", "-", "+", "." }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Regex.Replace(s, "[^a-zA-Z0-9]", string.Empty))
                .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1))
                .ToList();

            if (TrimmingStarts.Any(trimmingStart => trimmingStart.Equals(propertyNameParts.First(), StringComparison.InvariantCultureIgnoreCase)))
            {
                propertyNameParts.RemoveAt(0);
            }

            var propertyName = string.Join(string.Empty, propertyNameParts);
            if (char.IsDigit(propertyName[0]))
            {
                propertyName = $"R{propertyName}";
            }

            return propertyName;
        }
    }
}
