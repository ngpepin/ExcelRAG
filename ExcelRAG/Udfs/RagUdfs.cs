using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelDna.Integration;
using ExcelRAG.Infrastructure;
using ExcelRAG.Services;

namespace ExcelRAG.Udfs
{
	public static class RagUdfs
	{
	[ExcelFunction(Description = "Create a stable SourceId (caller-scoped) for a given key.")]
	public static object RAG_SOURCE_ID(object key, object trigger)
	{
		var callerKey = CallerKey.Current();
		var cacheKey = CallerScopedCache.MakeKey(callerKey, nameof(RAG_SOURCE_ID));
		var currentTrigger = TriggerKey.From(trigger);

		var cache = CallerScopedCache.TryGet(cacheKey);
		if (cache.IsHit && currentTrigger == cache.LastTriggerKey && cache.Value is string)
			return (string)cache.Value;

		var stable = "SRC-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
		CallerScopedCache.Set(cacheKey, currentTrigger, stable);
		return stable;
	}

	[ExcelFunction(Description = "Create a stable SourceId (caller-scoped) for a given key.")]
	public static object RAG_SOURCE_ID(object key)
	{
		return RAG_SOURCE_ID(key, ExcelMissing.Value);
	}

	[ExcelFunction(Description = "Chunk content into sentence-aware chunks (triggered, async).")]
	public static object RAG_CHUNK(object sourceId, object content, object maxChars, object trigger)
	{
		var callerKey = CallerKey.Current();
		var cacheKey = CallerScopedCache.MakeKey(callerKey, nameof(RAG_CHUNK));
		var currentTrigger = TriggerKey.From(trigger);

		var cache = CallerScopedCache.TryGet(cacheKey);
		if (cache.IsHit && currentTrigger == cache.LastTriggerKey)
			return cache.Value ?? ExcelEmpty.Value;

		return ExcelAsyncUtil.RunTask(nameof(RAG_CHUNK), new object[] { sourceId, content, maxChars, trigger }, () =>
			RunChunkAsync(cacheKey, currentTrigger, sourceId, content, maxChars));
	}

	[ExcelFunction(Description = "Chunk content into sentence-aware chunks (triggered, async).")]
	public static object RAG_CHUNK(object sourceId, object content)
	{
		return RAG_CHUNK(sourceId, content, 512, ExcelMissing.Value);
	}

	[ExcelFunction(Description = "Chunk content into sentence-aware chunks (triggered, async).")]
	public static object RAG_CHUNK(object sourceId, object content, object maxChars)
	{
		return RAG_CHUNK(sourceId, content, maxChars, ExcelMissing.Value);
	}

	private static async Task<object> RunChunkAsync(string cacheKey, TriggerKey currentTrigger, object sourceId, object content, object maxChars)
	{
		await Task.Yield();

		var sId = Convert.ToString(sourceId) ?? string.Empty;
		var text = Convert.ToString(content) ?? string.Empty;
		var max = 512;
		int parsed;
		if (maxChars != null && int.TryParse(Convert.ToString(maxChars), out parsed) && parsed > 0)
			max = parsed;

		RagProgressHub.Publish("chunk", "Running");

		var chunks = RagChunker.ChunkBySentence(sId, text, max);
		var result = new object[chunks.Count + 1, 6];
		result[0, 0] = "ChunkId";
		result[0, 1] = "SourceId";
		result[0, 2] = "ChunkText";
		result[0, 3] = "TokenCount";
		result[0, 4] = "OffsetStart";
		result[0, 5] = "OffsetEnd";

		for (var i = 0; i < chunks.Count; i++)
		{
			var c = chunks[i];
			result[i + 1, 0] = c.ChunkId;
			result[i + 1, 1] = c.SourceId;
			result[i + 1, 2] = c.ChunkText;
			result[i + 1, 3] = c.TokenCount;
			result[i + 1, 4] = c.OffsetStart;
			result[i + 1, 5] = c.OffsetEnd;
		}

		CallerScopedCache.Set(cacheKey, currentTrigger, result);
		RagProgressHub.Publish("chunk", "Done");
		return result;
	}

	[ExcelFunction(Description = "Observe latest RAG status for a topic.")]
	public static object RAG_STATUS(object topic)
	{
		var t = Convert.ToString(topic) ?? "chunk";
		return ExcelAsyncUtil.Observe(nameof(RAG_STATUS), new object[] { t }, () => new StatusObservable(t));
	}

	[ExcelFunction(Description = "Observe latest RAG status for a topic.")]
	public static object RAG_STATUS()
	{
		return RAG_STATUS("chunk");
	}

	private sealed class StatusObservable : IExcelObservable
	{
		private readonly string _topic;

		public StatusObservable(string topic)
		{
			_topic = topic;
		}

		public IDisposable Subscribe(IExcelObserver observer)
		{
			observer.OnNext("Idle");
			return RagProgressHub.Subscribe(_topic, new ExcelObserverAdapter(observer));
		}
	}

	private sealed class ExcelObserverAdapter : IObserver<object>
	{
		private readonly IExcelObserver _observer;

		public ExcelObserverAdapter(IExcelObserver observer)
		{
			_observer = observer;
		}

		public void OnCompleted()
		{
			_observer.OnCompleted();
		}

		public void OnError(Exception error)
		{
			_observer.OnError(error);
		}

		public void OnNext(object value)
		{
			_observer.OnNext(value);
		}
	}
}

}
