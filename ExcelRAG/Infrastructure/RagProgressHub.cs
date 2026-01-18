using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ExcelRAG.Infrastructure
{
	internal static class RagProgressHub
	{
		private static readonly ConcurrentDictionary<string, IObserver<object>> _observers =
			new ConcurrentDictionary<string, IObserver<object>>(StringComparer.Ordinal);
		private static readonly ConcurrentDictionary<string, object> _latest =
			new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

		public static IDisposable Subscribe(string topic, IObserver<object> observer)
		{
			_observers[topic] = observer;
			object value;
			if (_latest.TryGetValue(topic, out value))
				observer.OnNext(value);
			return new Subscription(topic, observer);
		}

		public static void Publish(string topic, object value)
		{
			_latest[topic] = value;
			IObserver<object> observer;
			if (_observers.TryGetValue(topic, out observer))
				observer.OnNext(value);
		}

		private sealed class Subscription : IDisposable
		{
			private readonly string _topic;
			private readonly IObserver<object> _observer;

			public Subscription(string topic, IObserver<object> observer)
			{
				_topic = topic;
				_observer = observer;
			}

			public void Dispose()
			{
				IObserver<object> removed;
				_observers.TryRemove(_topic, out removed);
			}
		}
	}
}
