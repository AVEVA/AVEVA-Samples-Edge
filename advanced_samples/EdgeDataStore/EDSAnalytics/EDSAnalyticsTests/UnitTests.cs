using Xunit;

namespace EDSAnalyticsTest
{
    public class UnitTests 
    {
        [Fact]
        public void EDSAnalyticsTest()
        {
            Assert.True(EDSAnalytics.Program.MainAsync().Result);           
        }
    }
}
