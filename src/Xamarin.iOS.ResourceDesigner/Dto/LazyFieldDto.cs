namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record LazyFieldDto
	{
		public string? ItemType { get; init; }
		public string? FieldName { get; init; }
		public string? CreateItemAction { get; init; }
		public string? ResourceId { get; init; }
		public string? ResourcePath { get; init; }
	}
}
