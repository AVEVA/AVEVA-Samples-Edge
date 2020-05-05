using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Xunit;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace EDSAnalyticsTest
{
    public class EDSAnalyticsTest 
    {
        [Fact]
        public void Test1()
        {
            Assert.True(EDSAnalytics.Program.MainAsync(true).Result);           
        }
    }
}
