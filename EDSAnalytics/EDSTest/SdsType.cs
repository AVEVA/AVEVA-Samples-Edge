using System.Collections.Generic;

namespace EDSAnalytics
{
    public class SdsType
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int SdsTypeCode { get; set; }

        public IList<SdsTypeProperty> Properties { get; set; }
    }
}

