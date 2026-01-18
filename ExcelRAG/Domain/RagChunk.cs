namespace ExcelRAG.Domain
{
	public sealed class RagChunk
	{
		public string ChunkId { get; set; }
		public string SourceId { get; set; }
		public string ChunkText { get; set; }
		public int TokenCount { get; set; }
		public int OffsetStart { get; set; }
		public int OffsetEnd { get; set; }
	}
}
