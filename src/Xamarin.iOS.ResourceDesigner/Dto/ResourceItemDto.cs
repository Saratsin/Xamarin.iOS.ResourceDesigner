namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record ResourceItemDto
	{
		public string? PropertyName { get; init; }
		public string? LazyFieldName { get; init; }
		public string? ResourceId { get; init; }
		public string? ResourcePath { get; init; }
	}
}
