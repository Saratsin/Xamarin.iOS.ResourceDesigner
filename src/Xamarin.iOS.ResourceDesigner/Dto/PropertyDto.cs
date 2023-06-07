namespace Xamarin.iOS.ResourceDesigner.Dto
{
    public record PropertyDto
	{
		public string? ItemType { get; init; }
		public string? PropertyName { get; init; }
		public string? FieldName { get; init; }
		public string? ResourcePath { get; init; }
	}
}
