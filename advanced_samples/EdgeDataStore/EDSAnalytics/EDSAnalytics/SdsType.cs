using System.Collections.Generic;

namespace EDSAnalytics
{
    public class SdsType
    {
        public SdsType()
        {
        }

        public SdsType(string id, string name, int sdsTypeCode, IList<SdsTypeProperty> properties)
        {
            Id = id;
            Name = name;
            SdsTypeCode = sdsTypeCode;
            Properties = properties;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int SdsTypeCode { get; set; }

        public IList<SdsTypeProperty> Properties { get; }
    }
}
