using System;
using System.Globalization;

namespace ExcelRAG.Infrastructure
{
	public struct TriggerKey : IEquatable<TriggerKey>
	{
		public static TriggerKey Empty
		{
			get { return new TriggerKey(string.Empty); }
		}

		public TriggerKey(string value)
		{
			Value = value ?? string.Empty;
		}

		public string Value { get; private set; }

		public static TriggerKey From(object trigger)
		{
			if (trigger == null)
				return Empty;

			var s = trigger as string;
			if (s != null)
				return new TriggerKey(s);

			return new TriggerKey(Convert.ToString(trigger, CultureInfo.InvariantCulture) ?? string.Empty);
		}

		public bool Equals(TriggerKey other)
		{
			return StringComparer.Ordinal.Equals(Value, other.Value);
		}
		public override bool Equals(object obj)
		{
			return obj is TriggerKey && Equals((TriggerKey)obj);
		}
		public override int GetHashCode()
		{
			return StringComparer.Ordinal.GetHashCode(Value);
		}
		public override string ToString()
		{
			return Value;
		}

		public static bool operator ==(TriggerKey left, TriggerKey right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(TriggerKey left, TriggerKey right)
		{
			return !left.Equals(right);
		}
	}
}
