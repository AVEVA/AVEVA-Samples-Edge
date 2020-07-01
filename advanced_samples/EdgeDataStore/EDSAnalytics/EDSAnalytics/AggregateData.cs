using System;

namespace EDSAnalytics
{
    public class AggregateData
    {
        public DateTime Timestamp { get; set; }
        
        public double Mean { get; set; }
        
        public double Minimum { get; set; }
        
        public double Maximum { get; set; }
     
        public double Range { get; set; }
    }
}
