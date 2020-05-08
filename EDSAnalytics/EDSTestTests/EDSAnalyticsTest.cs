using System;
using Xunit;

namespace EDSAnalyticsTest
{
    public class EDSAnalyticsTest 
    {
        [Fact]
        public void Test1()
        {
            Assert.True(EDSAnalytics.Program.MainAsync().Result);           
        }
    }
}
