using System;

namespace EDSAnalytics
{
    public class SineData
    {
        public SineData()
        {
            Value = 0;
        }

        public SineData(int angle)
        {
            Value = Math.Sin(angle * .2);
        }

        public double Value { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
