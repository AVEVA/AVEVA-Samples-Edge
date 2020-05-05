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
        public static async Task<bool> MainAsync(bool test = false)
        {
            Console.WriteLine("Getting configuration from appsettings.json");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) 
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.test.json", optional: true);
            IConfiguration configuration = builder.Build();

            // ==== Client constants ====
            port = configuration["EDSPort"];
            tenantId = configuration["TenantId"];
            namespaceId = configuration["NamespaceId"];
            apiVersion = configuration["apiVersion"];

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("================= Data Filtering =================");
                    // Step 1 - create SineWave type
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

                    // Step 2 - create SineWave stream                   
                    SdsStream sineWaveStream = new SdsStream
                    {
                        TypeId = sineWaveType.Id,
                        Id = "SineWave",
                        Name = "SineWave"
                    };
                    await CreateStream(sineWaveStream);
                    
                    // Step 3 - create events with a sine wave of data ranging from -1 to 1
                    Console.WriteLine("Initializing Sine Wave Data Events");
                    List<SineData> wave = new List<SineData>();
                    DateTime current = new DateTime();
                    current = DateTime.UtcNow;
                    int count = 20;
                    for (int i = 0; i < count; i++)
                    {
                        SineData newEvent = new SineData(i);
                        newEvent.Timestamp = current.AddSeconds(i).ToString("o");
                        wave.Add(newEvent);
                    }
                    await WriteDataToStream(wave, sineWaveStream);

                    // Step 4 - read in the sine wave data from the SineWave stream
                    Console.WriteLine("Ingressing Sine Wave Data");
                    var responseSineDataIngress = 
                        await httpClient.GetAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{sineWaveStream.Id}/Data?startIndex={wave[0].Timestamp}&count={count}");
                    CheckIfResponseWasSuccessful(responseSineDataIngress);
                    var responseBodySineData = await responseSineDataIngress.Content.ReadAsStreamAsync();
                    var returnData = new List<SineData>(); 
                    // since the return values are in gzip, they must be decoded
                    if (responseSineDataIngress.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        var sineDataDestination = new MemoryStream();
                        using (var decompressor = (Stream)new GZipStream(responseBodySineData, CompressionMode.Decompress, true))
                        {
                            decompressor.CopyToAsync(sineDataDestination).Wait();
                        }
                        sineDataDestination.Seek(0, SeekOrigin.Begin);
                        var sineDataRequestContent = sineDataDestination;
                        using (var sr = new StreamReader(sineDataRequestContent))
                        {
                            returnData = await JsonSerializer.DeserializeAsync<List<SineData>>(sineDataRequestContent);                  
                        }
                    }
                    else
                    {
                        Console.Write("Count must be greater than one");
                    }

                    // Step 5 
                    // create FilteredSineWaveStream
                    SdsStream filteredSineWaveStream = new SdsStream
                    {
                        TypeId = sineWaveType.Id,
                        Id = "FilteredSineWave",
                        Name = "FilteredSineWave"
                    };
                    await CreateStream(filteredSineWaveStream);

                    // Step 6 
                    // populate FilteredSineWaveStream
                    List<SineData> filteredWave = new List<SineData>();
                    int numberOfValidValues = 0;
                    Console.WriteLine("Filtering Data");
                    for (int i = 0; i < count; i++)
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
                    // Step 7 - create aggregatedDataType type                  
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

                    // Step 8 - create CalculatedAggregatedData stream
                    SdsStream calculatedAggregatedDataStream = new SdsStream
                    {
                        TypeId = aggregatedDataType.Id,
                        Id = "CalculatedAggregatedData",
                        Name = "CalculatedAggregatedData"
                    };
                    await CreateStream(calculatedAggregatedDataStream);

                    // Step 9 - calculate mean, min, max, and range using c# libraries and send to DataAggregation Stream
                    Console.WriteLine("Calculating mean, min, max, and range");
                    double mean = returnData.Average(rd => rd.Value);
                    Console.WriteLine("Mean = " + mean);
                    var values = new List<double>();
                    for (int i = 0; i < count; i++)
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
                        Timestamp = current.ToString("o"),
                        Mean = mean,
                        Minimum = min,
                        Maximum = max,
                        Range = range
                    };
                    await WriteDataToStream(calculatedData, calculatedAggregatedDataStream);

                    // Step 10 - create EdsApiAggregatedData stream
                    SdsStream edsApiAggregatedDataStream = new SdsStream
                    {
                        TypeId = aggregatedDataType.Id,
                        Id = "EdsApiAggregatedData",
                        Name = "EdsApiAggregatedData"
                    };
                    await CreateStream(edsApiAggregatedDataStream);

                    // Step 11 - Uses EDS’s standard data aggregate API calls to ingress aggregation data calculated by EDS
                    var edsDataAggregationIngress =
                        await httpClient.GetAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{sineWaveStream.Id}" +
                        $"/Data/Summaries?startIndex={calculatedData.Timestamp}&endIndex={current.AddMinutes(count).ToString("o")}&count=1");
                    CheckIfResponseWasSuccessful(edsDataAggregationIngress);
                    var responseBodyDataAggregation = await edsDataAggregationIngress.Content.ReadAsStreamAsync();
                    // since response is gzipped, it must be decoded
                    var destinationAggregatedData = new MemoryStream();
                    using (var decompressor = (Stream)new GZipStream(responseBodyDataAggregation, CompressionMode.Decompress, true))
                    {
                        decompressor.CopyToAsync(destinationAggregatedData).Wait();
                    }
                    destinationAggregatedData.Seek(0, SeekOrigin.Begin);
                    var requestContentAggregatedData = destinationAggregatedData;
                    using (var sr = new StreamReader(requestContentAggregatedData))
                    {
                        var returnDataAggregation = await JsonSerializer.DeserializeAsync<object>(requestContentAggregatedData);
                        string stringReturn = returnDataAggregation.ToString();
                        // create a new aggregateData object from the api call
                        AggregateData edsApi = new AggregateData
                        {
                            Timestamp = current.ToString("o"),
                            Mean = GetValue(stringReturn, "Mean"),
                            Minimum = GetValue(stringReturn, "Minimum"),
                            Maximum = GetValue(stringReturn, "Maximum"),
                            Range = GetValue(stringReturn, "Range")
                        };
                        await WriteDataToStream(edsApi, edsApiAggregatedDataStream);
                    }

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
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine("Deleting " + stream.Id + " Stream");
                HttpResponseMessage responseDeleteStream = 
                    await httpClient.DeleteAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}");
                CheckIfResponseWasSuccessful(responseDeleteStream);
            }
        }

        private static async Task DeleteType(SdsType type)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine("Deleting " + type.Id + " Type");
                HttpResponseMessage responseDeleteType =
                    await httpClient.DeleteAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Types/{type.Id}");
                CheckIfResponseWasSuccessful(responseDeleteType);
            }
        }

        private static async Task CreateStream(SdsStream stream) 
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine("Creating " + stream.Id + " Stream");
                StringContent stringStream = new StringContent(JsonSerializer.Serialize(stream));
                HttpResponseMessage responseCreateStream =
                    await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}", stringStream);
                CheckIfResponseWasSuccessful(responseCreateStream);
            }
        }

        private static async Task CreateType(SdsType type)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine("Creating " + type.Id + " Type");
                StringContent stringType = new StringContent(JsonSerializer.Serialize(type));
                HttpResponseMessage responseType = 
                    await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Types/{type.Id}", stringType);
                CheckIfResponseWasSuccessful(responseType);
            }
        }

        private static async Task WriteDataToStream(List<SineData> list, SdsStream stream)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine("Writing Data to " + stream.Id + " stream");
                StringContent serializedData = new StringContent(JsonSerializer.Serialize(list));
                HttpResponseMessage responseWriteDataToStream =
                    await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}/Data", serializedData);
                CheckIfResponseWasSuccessful(responseWriteDataToStream); 
            }
        }

        private static async Task WriteDataToStream(AggregateData data, SdsStream stream)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                List<AggregateData> dataList = new List<AggregateData>();
                dataList.Add(data);
                Console.WriteLine("Writing Data to " + stream.Id + " stream");
                StringContent serializedData = new StringContent(JsonSerializer.Serialize(dataList));
                HttpResponseMessage responseWriteDataToStream =
                    await httpClient.PostAsync($"http://localhost:{port}/api/{apiVersion}/Tenants/{tenantId}/Namespaces/{namespaceId}/Streams/{stream.Id}/Data", serializedData);
                CheckIfResponseWasSuccessful(responseWriteDataToStream);
            }
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

        private static double GetValue(string jsn, string property)
        {
            int meanStartIndex = jsn.IndexOf(property);
            // until reaches zero
            double meanDouble = Convert.ToDouble(jsn.Substring(meanStartIndex + 11 + property.Length, 16));
            Console.WriteLine(property + " = " + meanDouble);
            return meanDouble;
        }

    }
}
