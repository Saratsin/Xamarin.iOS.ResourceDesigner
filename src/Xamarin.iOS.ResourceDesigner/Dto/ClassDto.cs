using System.Collections.Generic;

namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ClassDto
	{
		public List<ResourceItemDto> ResourceItems { get; init; } = new();
	}
}
