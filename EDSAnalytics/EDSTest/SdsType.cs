using System;
using System.Collections.Generic;
using System.Text;

namespace EDSAnalytics
{
    class SdsType
    {
        public string Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public int SdsTypeCode
        {
            get;
            set;
        }
        public IList<SdsTypeProperty> Properties
        {
            get;
            set;
        }
    }
}

