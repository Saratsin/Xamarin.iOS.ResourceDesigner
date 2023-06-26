using System.Collections.Generic;

namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ResourcesDto
	{
		public string? Namespace { get; init; }

		public ClassDto? ImageClass { get; init; }

		public ClassDto? NibClass { get; init; }

		public ClassDto? ReuseIdentifierClass { get; init; }
	}
}
