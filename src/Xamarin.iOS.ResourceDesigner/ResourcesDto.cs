using System.Collections.Generic;

namespace Xamarin.iOS.ResourceDesigner
{
	public record ResourcesDto
	{
		public string? Namespace { get; init; }
		public List<ClassDto> ClassesList { get; init; } = new();
	}

	public record ClassDto
	{
		public string? ClassName { get; init; }
		public List<LazyFieldDto> LazyFieldsList { get; init; } = new();
		public List<PropertyDto> PropertiesList { get; init; } = new();
	}

	public record LazyFieldDto
	{
		public string? ItemType { get; init; }
		public string? FieldName { get; init; }
		public string? CreateItemAction { get; init; }
		public string? ResourceId { get; init; }
		public string? ResourcePath { get; init; }
	}

	public record PropertyDto
	{
		public string? ItemType { get; init; }
		public string? PropertyName { get; init; }
		public string? FieldName { get; init; }
		public string? ResourcePath { get; init; }
	}
}
