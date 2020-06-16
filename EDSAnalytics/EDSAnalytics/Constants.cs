namespace EDSAnalytics
{
    public class Constants
    {
        public const string FilteredSineWaveStream = "FilteredSineWave";
        public const string AggregatedDataStream = "AggregatedData";
        public const string SineWaveStream = "SineWave";
        public const string CalculatedAggregatedDataStream = "CalculatedAggregatedData";
        public const string EdsApiAggregatedDataStream = "EdsApiAggregatedData";

        public const string AggregatedDataTimestampProperty = "Timestamp";
        public const string AggregatedDataMeanProperty = "Mean";
        public const string AggregatedDataMinimumProperty = "Minimum";
        public const string AggregatedDataMaximumProperty = "Maximum";
        public const string AggregatedDataRangeProperty = "Range";

        public const string SummariesProperty = "Summaries";
        public const string ValueProperty = "Value";

        public const string DoubleTypeName = "Double";
        public const string DateTimeTypeName = "DateTime";

        public const int DefaultPortNumber = 5590;
        public const string TenantId = "default";
        public const string NamespaceId = "default";
    }
}
