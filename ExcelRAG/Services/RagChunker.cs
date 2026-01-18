using System;
using System.Collections.Generic;
using ExcelRAG.Domain;

namespace ExcelRAG.Services
{
	internal static class RagChunker
	{
		public static IReadOnlyList<RagChunk> ChunkBySentence(string sourceId, string content, int maxChars)
		{
		if (content == null)
			content = string.Empty;
		var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
		var chunks = new List<RagChunk>();
		var offset = 0;
		var chunkIndex = 0;

		foreach (var sentenceRaw in sentences)
		{
			var sentence = sentenceRaw.Trim();
			if (sentence.Length == 0)
				continue;

			var start = content.IndexOf(sentence, offset, StringComparison.Ordinal);
			if (start < 0)
				start = offset;
			var end = Math.Min(start + sentence.Length, content.Length);
			offset = end;

			foreach (var part in SplitByMaxChars(sentence, maxChars))
			{
				var partStart = start;
				var partEnd = Math.Min(start + part.Length, content.Length);

				chunks.Add(new RagChunk
				{
					ChunkId = $"CH-{chunkIndex:0000}",
					SourceId = sourceId,
					ChunkText = part,
					TokenCount = EstimateTokens(part),
					OffsetStart = partStart,
					OffsetEnd = partEnd,
				});
				chunkIndex++;
				start = partEnd;
			}
		}

			return chunks;
		}

	private static IEnumerable<string> SplitByMaxChars(string text, int maxChars)
	{
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			yield return text;
			yield break;
		}

		var index = 0;
		while (index < text.Length)
		{
			var len = Math.Min(maxChars, text.Length - index);
			yield return text.Substring(index, len);
			index += len;
		}
	}

	private static int EstimateTokens(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return 0;
		return text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
	}
	}
}
