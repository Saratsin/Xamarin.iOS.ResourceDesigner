using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using Stubble.Core;
using Xamarin.iOS.ResourceDesigner.Dto;

namespace Xamarin.iOS.ResourceDesigner
{
    public class ResourceParser
    {
        private const string MustacheTemplateResourceName = "Xamarin.iOS.ResourceDesigner.Resources.Resource.designer.mustache";
        
        private readonly TaskLoggingHelper Log;
        private readonly string Template;

        public ResourceParser(TaskLoggingHelper log)
        {
            Log = log;

            Template = CreateTemplate();
        }

        private string CreateTemplate()
        {
            using var templateStream = GetType().Assembly.GetManifestResourceStream(MustacheTemplateResourceName);
            using var templateStreamReader = new StreamReader(templateStream);

            return templateStreamReader.ReadToEnd();
        }

        public string Parse(
            string projectNamespace,
            ImageAssetsRawDto imageAssets,
            ColorAssetRawDto colorAssets,
            InterfaceDefinitionsRawDto interfaceDefinitions,
            string resourceDesignerFilePath)
        {
            Log.LogMessage("Will start to build resources file");

            var resourcesDto = new ResourcesDto
            {
                Namespace = projectNamespace,
                ImageClass = CreateImageClassDto(imageAssets, resourceDesignerFilePath),
                ColorClass = CreateColorClassDto(colorAssets, resourceDesignerFilePath),
                NibClass = CreateNibClassDto(interfaceDefinitions, resourceDesignerFilePath),
                ReuseIdentifierClass = CreateReuseIdentifierClassDto(interfaceDefinitions, resourceDesignerFilePath)
            };

            return StaticStubbleRenderer.Render(Template, resourcesDto);
        }

        private ClassDto? CreateImageClassDto(ImageAssetsRawDto imageAssets, string resourceDesignerFilePath)
        {
            // NOTE Filtering image asset paths only to those that we support
            var imageAssetPaths = imageAssets.ImageAssetPaths
                .Where(path => Path.GetFileName(path) == "Contents.json")
                .Select(path => Directory.GetParent(path).FullName)
                .Where(path => !string.IsNullOrEmpty(Path.GetExtension(path)))
                .Where(path => Path.GetExtension(path).TrimStart('.') == "imageset")
                .ToArray();

            var classDto = CreateClassDto(
                "Image",
                imageAssetPaths,
                imageAssets.TrimmingPrefixes,
                imageAssets.FilenamesSeparatorChars,
                resourceDesignerFilePath);

            return classDto;
        }
        
        private ClassDto? CreateColorClassDto(ColorAssetRawDto colorAssets, string resourceDesignerFilePath)
        {
            // NOTE Filtering image asset paths only to those that we support
            var colorAssetPaths = colorAssets.ColorAssetPaths
                .Where(path => Path.GetFileName(path) == "Contents.json")
                .Select(path => Directory.GetParent(path).FullName)
                .Where(path => !string.IsNullOrEmpty(Path.GetExtension(path)))
                .Where(path => Path.GetExtension(path).TrimStart('.') == "colorset")
                .ToArray();

            var classDto = CreateClassDto(
                "Color",
                colorAssetPaths,
                colorAssets.TrimmingPrefixes,
                colorAssets.FilenamesSeparatorChars,
                resourceDesignerFilePath);

            return classDto;
        }


        private ClassDto? CreateNibClassDto(InterfaceDefinitionsRawDto interfaceDefinitions, string resourceDesignerFilePath)
        {
            // NOTE Filtering xib paths only
            var xibPaths = interfaceDefinitions.InterfaceDefinitionPaths
                .Where(path => Path.GetExtension(path).TrimStart('.') == "xib")
                .ToArray();

            var classDto = CreateClassDto(
                "Nib",
                xibPaths,
                Array.Empty<string>(),
                Array.Empty<char>(),
                resourceDesignerFilePath);

            return classDto;
        }

        private ClassDto? CreateReuseIdentifierClassDto(InterfaceDefinitionsRawDto interfaceDefinitions, string resourceDesignerFilePath)
        {
            // NOTE Filtering xib paths only
            var xibCellPaths = interfaceDefinitions.InterfaceDefinitionPaths
                .Where(path => Path.GetExtension(path).TrimStart('.') == "xib")
                .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith("Cell"))
                .ToArray();

            var classDto = CreateClassDto(
                "ReuseIdentifier",
                xibCellPaths,
                Array.Empty<string>(),
                Array.Empty<char>(),
                resourceDesignerFilePath);

            return classDto;
        }

        private ClassDto? CreateClassDto(
            string className,
            string[] resourcePaths,
            string[] trimmingPrefixes,
            char[] filenamesSeparatorChars,
            string resourceDesignerFilePath)
        {
            if (resourcePaths.Length == 0)
            {
                return null;
            }

            var duplicates = new Dictionary<string, List<string>>();
            var ambigiousPropertyNames = new Dictionary<string, List<string>>();

            var resourcesList = new List<ResourceItemDto>();

            foreach (var resourcePath in resourcePaths)
            {
                var resourceName = Path.GetFileNameWithoutExtension(resourcePath);

                // NOTE Checking for duplicate names of the resource
                if (resourcesList.Any(field => field.ResourceId == resourceName))
                {
                    if (!duplicates.TryGetValue(resourceName, out var resourceDuplicates))
                    {
                        resourceDuplicates = new()
                        {
                            resourcesList.Single(field => field.ResourceId == resourceName).ResourcePath!
                        };

                        duplicates.Add(resourceName, resourceDuplicates);
                    }

                    resourceDuplicates.Add(resourcePath);
                    continue;
                }

                var propertyName = GetPropertyName(resourceName, filenamesSeparatorChars, trimmingPrefixes);

                // NOTE Checking for duplicate property name
                if (resourcesList.Any(property => property.PropertyName == propertyName))
                {
                    if (!ambigiousPropertyNames.TryGetValue(propertyName, out var ambiguities))
                    {
                        ambiguities = new()
                        {
                            resourcesList.Single(property => property.PropertyName == propertyName).ResourcePath!
                        };

                        ambigiousPropertyNames.Add(propertyName, ambiguities);
                    }

                    ambiguities.Add(resourcePath);

                    var suffix = ambiguities.Count - 1;
                    propertyName = $"{propertyName}_{suffix}";
                }

                // NOTE Checking if property has same name as a class
                if (propertyName == className)
                {
                    propertyName = $"{propertyName}Resource";
                    Log.LogWarningEx($"Asset {resourceName} produces the same property name as its enclosing class.\nAdding suffix in order to avoid compilation error.\nPropery name will be {propertyName}", resourceDesignerFilePath);
                }

                var lazyFieldName = GetLazyFieldName(propertyName);

                resourcesList.Add(new()
                {
                    LazyFieldName = lazyFieldName,
                    PropertyName = propertyName,
                    ResourceId = resourceName,
                    ResourcePath = resourcePath
                });
            }


            foreach (var duplicateList in duplicates)
            {
                var resourcePathesString = string.Join("\n", duplicateList.Value);
                Log.LogWarningEx($"Resource {duplicateList.Key} has {duplicateList.Value.Count - 1} duplicates.\nHere is the full list with pathes to all identical resources:\n{resourcePathesString}", resourceDesignerFilePath);
            }

            foreach (var ambigiousPropertyNameList in ambigiousPropertyNames)
            {
                var propertyName = ambigiousPropertyNameList.Key;
                var ambiguities = ambigiousPropertyNameList.Value;
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Property {propertyName} was ambigious and could be related to several resources.");
                stringBuilder.AppendLine("Due to this fact, first resource was named as is, and the following amigious resource properties were appended with number suffix.");
                stringBuilder.AppendLine("Here is the mapping table of the ambigious resources to their property names:");
                stringBuilder.AppendLine($"{propertyName} => {Path.GetFileNameWithoutExtension(ambiguities[0])}\n{ambiguities[0]}");
                for (var i = 1; i < ambiguities.Count; ++i)
                {
                    stringBuilder.AppendLine($"{propertyName}_{i} => {Path.GetFileNameWithoutExtension(ambiguities[i])}\n{ambiguities[i]}");
                }

                Log.LogWarningEx(stringBuilder.ToString(), resourceDesignerFilePath);
            }

            var classDto = new ClassDto { ResourceItems = resourcesList };
            return classDto;
        }

        private string GetLazyFieldName(string propertyName)
        {
            return "_" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1) + "Lazy";
        }

        private string GetPropertyName(string assetName, char[] filenamesSeparatorChars, string[] trimmingPrefixes)
        {
            var propertyNameParts = assetName
                .Split(filenamesSeparatorChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Regex.Replace(s, "[^a-zA-Z0-9]", string.Empty))
                .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1))
                .ToList();

            if (trimmingPrefixes.Any(trimmingPrefix => trimmingPrefix.Equals(propertyNameParts.First(), StringComparison.InvariantCultureIgnoreCase)))
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
