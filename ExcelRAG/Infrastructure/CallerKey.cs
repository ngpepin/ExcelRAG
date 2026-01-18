using ExcelDna.Integration;

namespace ExcelRAG.Infrastructure
{
	internal struct CallerKey
	{
		public CallerKey(string value)
		{
			Value = value;
		}

		public string Value { get; private set; }

		public static CallerKey Current()
		{
			var caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
			return new CallerKey(caller.GetHashCode().ToString());
		}

		public override string ToString()
		{
			return Value;
		}
	}
}
