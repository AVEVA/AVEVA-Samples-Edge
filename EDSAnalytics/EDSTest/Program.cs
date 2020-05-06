using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json.Linq;

namespace EDSAnalytics
{
    public class Program 
    {
        private static string port;
        private static string tenantId;
        private static string namespaceId;
        private static string apiVersion;

        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }
        public static async Task<bool> MainAsync()
        {
            Console.WriteLine("Getting configuration from appsettings.json");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            // ==== Client constants ====
            port = configuration["EDSPort"];
            tenantId = configuration["TenantId"];
            namespaceId = configuration["NamespaceId"];
            apiVersion = configuration["apiVersion"];

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                try
                {   // ====================== Data Filtering portion ======================
                    Console.WriteLine();
                    Console.WriteLine("================= Data Filtering =================");
                    // Step 1 - Create SineWave type
                    // create Timestamp property
                    SdsTypeProperty timestamp = new SdsTypeProperty
                    {
                        Id = "Timestamp",
                        Name = "Timestamp",
                        IsKey = true,
                        SdsType = new SdsType
                        {
                            Name = "DateTime",
                            SdsTypeCode = 16
                        }
                    };
                    SdsType sineWaveType = new SdsType
                    {
                        Id = "SineWave",
                        Name = "SineWave",
                        SdsTypeCode = 1,
                        Properties = new List<SdsTypeProperty>()
                        {
                            timestamp,
                            CreateSdsTypePropertyOfTypeDouble("Value", false)
                        }
                    };
                    await CreateType(sineWaveType);

                    // Step 2 - Create SineWave stream        
                    SdsStream sineWaveStream = CreateStream(sineWaveType, "SineWave", "SineWave").Result;

                    // Step 3 - Create events of SineData objects. The value property of the SineData object is intitialized to value between -1.0 and 1.0
                    Console.WriteLine("Initializing SineData Events");
                    List<SineData> waveList = new List<SineData>();
                    DateTime firstTimestamp = new DateTime();
                    firstTimestamp = DateTime.UtcNow;
                    // numberOfEvents must be an integer > 1
                    int numberOfEvents = 100;
                    for (int i = 0; i < numberOfEvents; i++)
                    {
                        SineData newEvent = new SineData(i)
                        {
                            Timestamp = firstTimestamp.AddSeconds(i).ToString("o")
                        };
                        waveList.Add(newEvent);
                    }
                    await WriteDataToStream(waveList, sineWaveStream);

                    // Step 4 - Ingress the sine wave data from the SineWave stream
                    var returnData = await IngressSineData(sineWaveStream, waveList[0].Timestamp, numberOfEvents);

                    // Step 5 - Create FilteredSineWaveStream
                    SdsStream filteredSineWaveStream = CreateStream(sineWaveType, "FilteredSineWave", "FilteredSineWave").Result;

                    // Step 6 - Populate FilteredSineWaveStream with filtered data
                    List<SineData> filteredWave = new List<SineData>();
                    int numberOfValidValues = 0;
                    Console.WriteLine("Filtering Data");
                    for (int i = 0; i < numberOfEvents; i++)
                    {
                        // filters the data to only include values outside the range -0.9 to 0.9 
                        // change this conditional to apply the type of filter you desire
                        if (returnData[i].Value > .9 || returnData[i].Value < -.9)
                        {
                            filteredWave.Add(returnData[i]);
                            numberOfValidValues++;
                        }
                    }
                    await WriteDataToStream(filteredWave, filteredSineWaveStream);

                    // ====================== Data Aggregation portion ======================
                    Console.WriteLine();
                    Console.WriteLine("================ Data Aggregation ================");
                    // Step 7 - Create aggregatedDataType type                  
                    SdsType aggregatedDataType = new SdsType
                    {
                        Id = "AggregatedData",
                        Name = "AggregatedData",
                        SdsTypeCode = 1,
                        Properties = new List<SdsTypeProperty>()
                        {
                            timestamp,
                            CreateSdsTypePropertyOfTypeDouble("Mean", false),
                            CreateSdsTypePropertyOfTypeDouble("Minimum", false),
                            CreateSdsTypePropertyOfTypeDouble("Maximum", false),
                            CreateSdsTypePropertyOfTypeDouble("Range", false)
                        }
                    };
                    await CreateType(aggregatedDataType);

                    // Step 8 - Create CalculatedAggregatedData stream
                    SdsStream calculatedAggregatedDataStream = CreateStream(aggregatedDataType, "CalculatedAggregatedData", "CalculatedAggregatedData").Result;

                    // Step 9 - Calculate mean, min, max, and range using c# libraries and send to DataAggregation Stream
                    Console.WriteLine("Calculating mean, min, max, and range");
                    double mean = returnData.Average(rd => rd.Value);
                    Console.WriteLine("Mean = " + mean);
                    var values = new List<double>();
                    for (int i = 0; i < numberOfEvents; i++)
                    {
                        values.Add(returnData[i].Value);
                        numberOfValidValues++;
                    }
                    var min = values.Min();
                    Console.WriteLine("Min = " + min);
                    var max = values.Max();
                    Console.WriteLine("Max = " + max);
                    var range = max - min;
                    Console.WriteLine("Range = " + range);         
                    AggregateData calculatedData = new AggregateData
                    {
                        Timestamp = firstTimestamp.ToString("o"),
                        Mean = mean,
                        Minimum = min,
                        Maximum = max,
                        Range = range
                    };
                    await WriteDataToStream(calculatedData, calculatedAggregatedDataStream);

                    // Step 10 - Create EdsApiAggregatedData stream
                    SdsStream edsApiAggregatedDataStream = CreateStream(aggregatedDataType, "EdsApiAggregatedData", "EdsApiAggregatedData").Result;

                    // Step 11 - Use EDS’s standard data aggregate API calls to ingress aggregation data calculated by EDS
                    string summaryData = await IngressSummaryData(sineWaveStream, calculatedData.Timestamp, firstTimestamp.AddMinutes(numberOfEvents).ToString("o"));
                    summaryData = summaryData.TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' });
                    var data = JObject.Parse(summaryData)["Summaries"].ToString();
                    string meanObject = JObject.Parse(data)["Mean"].ToString();
                    string minObject = JObject.Parse(data)["Minimum"].ToString();
                    string maxObject = JObject.Parse(data)["Maximum"].ToString();
                    string rangeObject = JObject.Parse(data)["Range"].ToString();
                    double summaryMean = Convert.ToDouble(JObject.Parse(meanObject)["Value"].ToString());
                    double summaryMinimum = Convert.ToDouble(JObject.Parse(minObject)["Value"].ToString());
                    double summaryMaximum = Convert.ToDouble(JObject.Parse(maxObject)["Value"].ToString());
                    double summaryRange = Convert.ToDouble(JObject.Parse(rangeObject)["Value"].ToString());
                    Console.WriteLine("Mean = " + summaryMean);
                    Console.WriteLine("Min = " + summaryMinimum);
                    Console.WriteLine("Max = " + summaryMaximum);
                    Console.WriteLine("Range = " + summaryRange);
                    /*var valueData = Convert.ToDouble(JObject.Parse(meanData)["Value"].ToString()); //.ToString();                    
                    Console.WriteLine();
                    Console.WriteLine(msg);
                    Console.WriteLine();
                    */
                    /*
                    string json = summaryData;
                    using JsonDocument doc = JsonDocument.Parse(json)
                    {
                        doc.P
                    }
                    */
                    


                    AggregateData edsApi = new AggregateData
                    {
                        Timestamp = firstTimestamp.ToString("o"),
                        Mean = summaryMean,
                        Minimum = summaryMinimum,
                        Maximum = summaryMaximum,
                        Range = summaryMaximum
                    };
                    await WriteDataToStream(edsApi, edsApiAggregatedDataStream);

                    Console.WriteLine();
                    Console.WriteLine("==================== Clean-Up =====================");
                    
                    // Step 12 - Delete Streams and Types
                    await DeleteStream(sineWaveStream);
                    await DeleteStream(filteredSineWaveStream);
                    await DeleteStream(calculatedAggregatedDataStream);
                    await DeleteStream(edsApiAggregatedDataStream);
                    await DeleteType(sineWaveType);
                    await DeleteType(aggregatedDataType);                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw e;
                }
                finally
                {
                    (configuration as IDisposable)?.Dispose();
                    Console.WriteLine();
                    Console.WriteLine("Demo Application Ran Successfully!");
                }
            }
            return true;
        }

        private static void CheckIfResponseWasSuccessful(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(response.ToString());
            }
        }

        private static async Task DeleteStream(SdsStream stream)
        {
            HttpClient httpClient = new HttpClient();
            Console.WriteLine("Deleting " + stream.Id + " Stream");
            HttpResponseMessage responseDeleteStream =
                await httpClient.DeleteAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}");
            CheckIfResponseWasSuccessful(responseDeleteStream);
        }

        private static async Task DeleteType(SdsType type)
        {
            HttpClient httpClient = new HttpClient();
            Console.WriteLine("Deleting " + type.Id + " Type");
            HttpResponseMessage responseDeleteType =
                await httpClient.DeleteAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Types/{type.Id}");
            CheckIfResponseWasSuccessful(responseDeleteType);
        }

        private static async Task<SdsStream> CreateStream(SdsType type, string id, string name) 
        {
            HttpClient httpClient = new HttpClient();
            SdsStream stream = new SdsStream
            {
                TypeId = type.Id,
                Id = id,
                Name = name
            };
            Console.WriteLine("Creating " + stream.Id + " Stream");
            StringContent stringStream = new StringContent(JsonSerializer.Serialize(stream));
            HttpResponseMessage responseCreateStream =
                await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}", stringStream);
            CheckIfResponseWasSuccessful(responseCreateStream);
            return stream;
        }

        private static async Task CreateType(SdsType type)
        {
            HttpClient httpClient = new HttpClient();
            Console.WriteLine("Creating " + type.Id + " Type");
            StringContent stringType = new StringContent(JsonSerializer.Serialize(type));
            HttpResponseMessage responseType =
                await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Types/{type.Id}", stringType);
            CheckIfResponseWasSuccessful(responseType);

        }

        private static async Task<List<SineData>> IngressSineData(SdsStream stream, string timestamp, int numberOfEvents)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            Console.WriteLine("Ingressing data from " + stream.Id + " stream");
            var responseIngress =
                await httpClient.GetAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/" +
                $"{stream.Id}/Data?startIndex={timestamp}&count={numberOfEvents}");
            CheckIfResponseWasSuccessful(responseIngress);
            MemoryStream ms = await DecompressGzip(responseIngress);
            var returnData = new List<SineData>();
            using (var sr = new StreamReader(ms))
            {
                returnData = await JsonSerializer.DeserializeAsync<List<SineData>>(ms);
            }
            return returnData;
        }

        private static async Task<string> IngressSummaryData(SdsStream stream, string startTimestamp, string endTimestamp)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            Console.WriteLine("Ingressing Data from " + stream.Id + " Stream Summary");
            var responseIngress =
                await httpClient.GetAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/" +
                $"{stream.Id}/Data/Summaries?startIndex={startTimestamp}&endIndex={endTimestamp}&count=1");
            CheckIfResponseWasSuccessful(responseIngress);
            MemoryStream ms = await DecompressGzip(responseIngress);
            using (var sr = new StreamReader(ms))
            {
                var objectSummaryData = await JsonSerializer.DeserializeAsync<object>(ms);
                return objectSummaryData.ToString();
            }
        }

        private static async Task<MemoryStream> DecompressGzip(HttpResponseMessage httpMessage)
        {
            var response = await httpMessage.Content.ReadAsStreamAsync();
            var destination = new MemoryStream();
            using (var decompressor = (Stream)new GZipStream(response, CompressionMode.Decompress, true))
            {
                decompressor.CopyToAsync(destination).Wait();
            }
            destination.Seek(0, SeekOrigin.Begin);
            return destination;
        }

        private static async Task WriteDataToStream(List<SineData> list, SdsStream stream)
        {
            HttpClient httpClient = new HttpClient();
            Console.WriteLine("Writing Data to " + stream.Id + " stream");
            StringContent serializedData = new StringContent(JsonSerializer.Serialize(list));
            HttpResponseMessage responseWriteDataToStream =
                await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}/Data", serializedData);
            CheckIfResponseWasSuccessful(responseWriteDataToStream);
        }

        private static async Task WriteDataToStream(AggregateData data, SdsStream stream)
        {
            HttpClient httpClient = new HttpClient();
            List<AggregateData> dataList = new List<AggregateData>();
            dataList.Add(data);
            Console.WriteLine("Writing Data to " + stream.Id + " stream");
            StringContent serializedData = new StringContent(JsonSerializer.Serialize(dataList));
            HttpResponseMessage responseWriteDataToStream =
                await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}/Data", serializedData);
            CheckIfResponseWasSuccessful(responseWriteDataToStream);
        }

        private static SdsTypeProperty CreateSdsTypePropertyOfTypeDouble(string idAndName, bool isKey)
        {
            SdsTypeProperty property = new SdsTypeProperty
            {
                Id = idAndName,
                Name = idAndName,
                IsKey = isKey,
                SdsType = new SdsType
                {
                    Name = "Double",
                    SdsTypeCode = 14
                }
            };
            return property;
        }

        /*
        private static double GetValue(string jsn, string property)
        {
            int meanStartIndex = jsn.IndexOf(property);
            // until reaches zero
            double meanDouble = Convert.ToDouble(jsn.Substring(meanStartIndex + 11 + property.Length, 16));
            Console.WriteLine(property + " = " + meanDouble);
            return meanDouble;
        }
        */

    }
}
