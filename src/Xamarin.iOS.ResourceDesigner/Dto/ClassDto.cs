using System.Collections.Generic;

namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ClassDto
	{
		public string? ClassName { get; init; }
		public List<LazyFieldDto> LazyFieldsList { get; init; } = new();
		public List<PropertyDto> PropertiesList { get; init; } = new();
	}
}
