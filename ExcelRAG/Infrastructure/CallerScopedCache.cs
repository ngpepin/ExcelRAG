using System;
using System.Collections.Concurrent;

namespace ExcelRAG.Infrastructure
{
	internal static class CallerScopedCache
	{
		private sealed class Entry
		{
			public TriggerKey LastTriggerKey;
			public object Value;
		}

		private static readonly ConcurrentDictionary<string, Entry> _cache =
			new ConcurrentDictionary<string, Entry>(StringComparer.Ordinal);

		public struct CacheResult
		{
			public bool IsHit;
			public object Value;
			public TriggerKey LastTriggerKey;
		}

		public static CacheResult TryGet(string cacheKey)
		{
			Entry entry;
			if (!_cache.TryGetValue(cacheKey, out entry))
				return new CacheResult { IsHit = false, Value = null, LastTriggerKey = TriggerKey.Empty };

			return new CacheResult { IsHit = true, Value = entry.Value, LastTriggerKey = entry.LastTriggerKey };
		}

		public static void Set(string cacheKey, TriggerKey triggerKey, object value)
		{
			var entry = _cache.GetOrAdd(cacheKey, _ => new Entry());
			entry.LastTriggerKey = triggerKey;
			entry.Value = value;
		}

		public static string MakeKey(CallerKey callerKey, string functionName, string localKey)
		{
			if (string.IsNullOrWhiteSpace(localKey))
				return callerKey + "|" + functionName;

			return callerKey + "|" + functionName + "|" + localKey;
		}

		public static string MakeKey(CallerKey callerKey, string functionName)
		{
			return MakeKey(callerKey, functionName, null);
		}
	}
}
