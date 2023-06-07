using System.Collections.Generic;

namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ResourcesDto
	{
		public string? Namespace { get; init; }
		public List<ClassDto> ClassesList { get; init; } = new();
	}
}
