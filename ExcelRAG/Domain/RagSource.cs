namespace ExcelRAG.Domain
{
	public sealed class RagSource
	{
		public string SourceId { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public string Owner { get; set; }
		public string Tags { get; set; }
	}
}
