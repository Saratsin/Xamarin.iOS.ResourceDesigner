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
        
        private readonly TaskLoggingHelper _log;
        private readonly string _template;

        public ResourceParser(TaskLoggingHelper log)
        {
            _log = log;

            _template = CreateTemplate();
        }

        private string CreateTemplate()
        {
            using var templateStream = GetType().Assembly.GetManifestResourceStream(MustacheTemplateResourceName);
            using var templateStreamReader = new StreamReader(templateStream);

            return templateStreamReader.ReadToEnd();
        }

        public string Parse(
            string projectNamespace,
            ImageSetsRawDto imageSets,
            ColorSetsRawDto colorSets,
            InterfaceDefinitionsRawDto interfaceDefinitions,
            string resourceDesignerFilePath)
        {
            _log.LogMessage("Will start to build resources file");

            var resourcesDto = new ResourcesDto
            {
                Namespace = projectNamespace,
                ImageClass = CreateImageClassDto(imageSets, resourceDesignerFilePath),
                ColorClass = CreateColorClassDto(colorSets, resourceDesignerFilePath),
                NibClass = CreateNibClassDto(interfaceDefinitions, resourceDesignerFilePath),
                ReuseIdentifierClass = CreateReuseIdentifierClassDto(interfaceDefinitions, resourceDesignerFilePath)
            };

            return StaticStubbleRenderer.Render(_template, resourcesDto);
        }

        private ClassDto? CreateImageClassDto(ImageSetsRawDto imageSets, string resourceDesignerFilePath)
        {
            var classDto = CreateClassDto(
                "Image",
                imageSets.ImageSetPaths,
                imageSets.TrimmingPrefixes,
                imageSets.FilenamesSeparatorChars,
                resourceDesignerFilePath);

            return classDto;
        }
        
        private ClassDto? CreateColorClassDto(ColorSetsRawDto colorSets, string resourceDesignerFilePath)
        {
            // NOTE Filtering image asset paths only to those that we support
            var classDto = CreateClassDto(
                "Color",
                colorSets.ColorSetPaths,
                colorSets.TrimmingPrefixes,
                colorSets.FilenamesSeparatorChars,
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
            var ambiguousPropertyNames = new Dictionary<string, List<string>>();

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
                    if (!ambiguousPropertyNames.TryGetValue(propertyName, out var ambiguities))
                    {
                        ambiguities = new()
                        {
                            resourcesList.Single(property => property.PropertyName == propertyName).ResourcePath!
                        };

                        ambiguousPropertyNames.Add(propertyName, ambiguities);
                    }

                    ambiguities.Add(resourcePath);

                    var suffix = ambiguities.Count - 1;
                    propertyName = $"{propertyName}_{suffix}";
                }

                // NOTE Checking if property has same name as a class
                if (propertyName == className)
                {
                    propertyName = $"{propertyName}Resource";
                    _log.LogWarningEx($"Asset {resourceName} produces the same property name as its enclosing class.\nAdding suffix in order to avoid compilation error.\nProperty name will be {propertyName}", resourceDesignerFilePath);
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
                var resourcePathsString = string.Join("\n", duplicateList.Value);
                _log.LogWarningEx($"Resource {duplicateList.Key} has {duplicateList.Value.Count - 1} duplicates.\nHere is the full list with paths to all identical resources:\n{resourcePathsString}", resourceDesignerFilePath);
            }

            foreach (var ambiguousPropertyNameList in ambiguousPropertyNames)
            {
                var propertyName = ambiguousPropertyNameList.Key;
                var ambiguities = ambiguousPropertyNameList.Value;
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Property {propertyName} was ambiguous and could be related to several resources.");
                stringBuilder.AppendLine("Due to this fact, first resource was named as is, and the following ambiguous resource properties were appended with number suffix.");
                stringBuilder.AppendLine("Here is the mapping table of the ambiguous resources to their property names:");
                stringBuilder.AppendLine($"{propertyName} => {Path.GetFileNameWithoutExtension(ambiguities[0])}\n{ambiguities[0]}");
                for (var i = 1; i < ambiguities.Count; ++i)
                {
                    stringBuilder.AppendLine($"{propertyName}_{i} => {Path.GetFileNameWithoutExtension(ambiguities[i])}\n{ambiguities[i]}");
                }

                _log.LogWarningEx(stringBuilder.ToString(), resourceDesignerFilePath);
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
