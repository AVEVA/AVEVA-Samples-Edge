using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace EDSAnalytics
{
    public class Program
    {
        private static HttpClient _httpClient = new HttpClient();
        private static HttpClient _httpClientGzip = new HttpClient();

        private static int _port;
        private static string _tenantId;
        private static string _namespaceId;

        public static async Task Main()
        {
            await MainAsync();
            Console.WriteLine();
            Console.WriteLine("Demo Application Ran Successfully!");
        }

        public static async Task<bool> MainAsync()
        {
            // ==== Client constants ====
            _port = Constants.DefaultPortNumber;    // defaults to 5590
            _tenantId = Constants.TenantId;
            _namespaceId = Constants.NamespaceId;

            _httpClientGzip.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

            try
            {   // ====================== Data Filtering portion ======================
                Console.WriteLine();
                Console.WriteLine("================= Data Filtering =================");
                
                // Step 1 - Create SineWave type
                var sineWaveProperties = new List<SdsTypeProperty>
                    {
                        CreateSdsTypePropertyOfTypeDateTime(nameof(SineData.Timestamp), true),
                        CreateSdsTypePropertyOfTypeDouble(nameof(SineData.Value), false)
                    };

                var sineWaveType = await AsyncCreateTypeAsync(Constants.SineWaveStream, Constants.SineWaveStream, sineWaveProperties);

                // Step 2 - Create SineWave stream        
                var sineWaveStream = await AsyncCreateStreamAsync(sineWaveType, Constants.SineWaveStream, Constants.SineWaveStream);

                // Step 3 - Create a list of events of SineData objects. The value property of the SineData object is intitialized to a value between -1.0 and 1.0
                Console.WriteLine("Initializing SineData Events");
                var waveList = new List<SineData>();
                var firstTimestamp = DateTime.UtcNow;
                // numberOfEvents must be an integer > 1
                int numberOfEvents = 100;
                for (int i = 0; i < numberOfEvents; i++)
                {
                    waveList.Add(new SineData(i)
                    {
                        Timestamp = firstTimestamp.AddSeconds(i)
                    });
                }

                await AsyncWriteDataToStreamAsync(waveList, sineWaveStream);

                // Step 4 - Ingress the sine wave data from the SineWave stream
                var returnData = await AsyncQuerySineDataAsync(sineWaveStream, waveList[0].Timestamp, numberOfEvents);

                // Step 5 - Create FilteredSineWaveStream
                var filteredSineWaveStream = await AsyncCreateStreamAsync(sineWaveType, Constants.FilteredSineWaveStream, Constants.FilteredSineWaveStream);

                // Step 6 - Populate FilteredSineWaveStream with filtered data
                var filteredWave = new List<SineData>();
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

                await AsyncWriteDataToStreamAsync(filteredWave, filteredSineWaveStream);

                // ====================== Data Aggregation portion ======================
                Console.WriteLine();
                Console.WriteLine("================ Data Aggregation ================");

                // Step 7 - Create aggregatedDataType type   
                var aggregatedData = new List<SdsTypeProperty>
                    {
                        CreateSdsTypePropertyOfTypeDateTime(Constants.AggregatedDataTimestampProperty, true),
                        CreateSdsTypePropertyOfTypeDouble(Constants.AggregatedDataMeanProperty, false),
                        CreateSdsTypePropertyOfTypeDouble(Constants.AggregatedDataMinimumProperty, false),
                        CreateSdsTypePropertyOfTypeDouble(Constants.AggregatedDataMaximumProperty, false),
                        CreateSdsTypePropertyOfTypeDouble(Constants.AggregatedDataRangeProperty, false)
                    };

                var aggregatedDataType = await AsyncCreateTypeAsync(Constants.AggregatedDataStream, Constants.AggregatedDataStream, aggregatedData);

                // Step 8 - Create CalculatedAggregatedData stream
                var calculatedAggregatedDataStream = await AsyncCreateStreamAsync(aggregatedDataType, 
                    Constants.CalculatedAggregatedDataStream, Constants.CalculatedAggregatedDataStream);

                // Step 9 - Calculate mean, min, max, and range using c# libraries and send to the CalculatedAggregatedData Stream
                Console.WriteLine("Calculating mean, min, max, and range");
                var sineDataValues = new List<double>();
                for (int i = 0; i < numberOfEvents; i++)
                {
                    sineDataValues.Add(returnData[i].Value);
                    numberOfValidValues++;
                }

                var calculatedData = new AggregateData
                {
                    Timestamp = firstTimestamp,
                    Mean = returnData.Average(rd => rd.Value),
                    Minimum = sineDataValues.Min(),
                    Maximum = sineDataValues.Max(),
                    Range = sineDataValues.Max() - sineDataValues.Min()
                };

                Console.WriteLine("    Mean = " + calculatedData.Mean);
                Console.WriteLine("    Minimum = " + calculatedData.Minimum);
                Console.WriteLine("    Maximum = " + calculatedData.Maximum);
                Console.WriteLine("    Range = " + calculatedData.Range);
                await AsyncWriteDataToStreamAsync(calculatedData, calculatedAggregatedDataStream);

                // Step 10 - Create EdsApiAggregatedData stream
                var edsApiAggregatedDataStream = await AsyncCreateStreamAsync(aggregatedDataType, 
                    Constants.EdsApiAggregatedDataStream, Constants.EdsApiAggregatedDataStream);

                // Step 11 - Use EDS’s standard data aggregate API calls to ingress aggregated data calculated by EDS and send to EdsApiAggregatedData stream
                var summaryData = await AsyncQuerySummaryDataAsync(sineWaveStream, calculatedData.Timestamp, firstTimestamp.AddSeconds(numberOfEvents));
                var edsApi = new AggregateData
                {
                    Timestamp = firstTimestamp,
                    Mean = GetValue(summaryData, Constants.AggregatedDataMeanProperty),
                    Minimum = GetValue(summaryData, Constants.AggregatedDataMinimumProperty),
                    Maximum = GetValue(summaryData, Constants.AggregatedDataMaximumProperty),
                    Range = GetValue(summaryData, Constants.AggregatedDataRangeProperty)
                };

                await AsyncWriteDataToStreamAsync(edsApi, edsApiAggregatedDataStream);

                Console.WriteLine();
                Console.WriteLine("==================== Clean-Up =====================");

                // Step 12 - Delete Streams and Types
                await AsyncDeleteStreamAsync(sineWaveStream);
                await AsyncDeleteStreamAsync(filteredSineWaveStream);
                await AsyncDeleteStreamAsync(calculatedAggregatedDataStream);
                await AsyncDeleteStreamAsync(edsApiAggregatedDataStream);
                await AsyncDeleteTypeAsync(sineWaveType);
                await AsyncDeleteTypeAsync(aggregatedDataType);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            finally
            {
                _httpClient?.Dispose();
                _httpClientGzip?.Dispose();
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

        private static async Task AsyncDeleteStreamAsync(SdsStream stream)
        {
            Console.WriteLine("Deleting " + stream.Id + " Stream");
            
            var responseDeleteStream =
                await _httpClient.DeleteAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/{stream.Id}");
            CheckIfResponseWasSuccessful(responseDeleteStream);
        }

        private static async Task AsyncDeleteTypeAsync(SdsType type)
        {
            Console.WriteLine("Deleting " + type.Id + " Type");
            
            var responseDeleteType =
                await _httpClient.DeleteAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Types/{type.Id}");
            CheckIfResponseWasSuccessful(responseDeleteType);
        }

        private static async Task<SdsStream> AsyncCreateStreamAsync(SdsType type, string id, string name)
        {
            var stream = new SdsStream
            {
                TypeId = type.Id,
                Id = id,
                Name = name
            };

            Console.WriteLine("Creating " + stream.Id + " Stream");
            
            using (var stringStream = new StringContent(JsonSerializer.Serialize(stream)))
            {
                var responseCreateStream =
                    await _httpClient.PostAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/{stream.Id}", stringStream);

                CheckIfResponseWasSuccessful(responseCreateStream);
            }
            return stream;
        }

        private static async Task<SdsType> AsyncCreateTypeAsync(string id, string name, List<SdsTypeProperty> properties)
        {
            var type = new SdsType
            {
                Id = id,
                Name = name,
                SdsTypeCode = 1,
                Properties = properties
            };

            Console.WriteLine("Creating " + type.Id + " Type");
            
            using (var stringType = new StringContent(JsonSerializer.Serialize(type)))
            {
                var responseType =
                    await _httpClient.PostAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Types/{type.Id}", stringType);

                CheckIfResponseWasSuccessful(responseType);
            }
            return type;
        }

        private static async Task<List<SineData>> AsyncQuerySineDataAsync(SdsStream stream, DateTime timestamp, int numberOfEvents)
        {
            Console.WriteLine("Ingressing data from " + stream.Id + " stream");
            
            using (var responseIngress =
                await _httpClientGzip.GetAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/" +
                $"{stream.Id}/Data?startIndex={timestamp.ToString("o")}&count={numberOfEvents}"))
            {
                CheckIfResponseWasSuccessful(responseIngress);
                var ms = await AsyncDecompressGzipAsync(responseIngress);
                using (var sr = new StreamReader(ms))
                {
                    return await JsonSerializer.DeserializeAsync<List<SineData>>(ms);
                }
            }
        }

        private static async Task<string> AsyncQuerySummaryDataAsync(SdsStream stream, DateTime startTimestamp, DateTime endTimestamp)
        {
            Console.WriteLine("Ingressing Data from " + stream.Id + " Stream Summary");
            
            using (var responseIngress =
                await _httpClientGzip.GetAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/" +
                $"{stream.Id}/Data/Summaries?startIndex={startTimestamp.ToString("o")}&endIndex={endTimestamp.ToString("o")}&count=1"))
            {
                CheckIfResponseWasSuccessful(responseIngress);
                var ms = await AsyncDecompressGzipAsync(responseIngress);
                using (var sr = new StreamReader(ms))
                {
                    var objectSummaryData = await JsonSerializer.DeserializeAsync<object>(ms);
                    return objectSummaryData.ToString().TrimStart(new char[] { '[' }).TrimEnd(new char[] { ']' });
                }
            }
        }

        private static async Task<MemoryStream> AsyncDecompressGzipAsync(HttpResponseMessage httpMessage)
        {
            using (var response = await httpMessage.Content.ReadAsStreamAsync())
            {
                var destination = new MemoryStream();
                using (var decompressor = (Stream)new GZipStream(response, CompressionMode.Decompress, true))
                {
                    decompressor.CopyToAsync(destination).Wait();
                }
                destination.Seek(0, SeekOrigin.Begin);
                return destination;
            }
        }

        private static async Task AsyncWriteDataToStreamAsync(List<SineData> list, SdsStream stream)
        {
            Console.WriteLine("Writing Data to " + stream.Id + " stream");

            using (var serializedData = new StringContent(JsonSerializer.Serialize(list)))
            {
                var responseWriteDataToStream =
                    await _httpClient.PostAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/{stream.Id}/Data", serializedData);
                CheckIfResponseWasSuccessful(responseWriteDataToStream);
            }
        }

        private static async Task AsyncWriteDataToStreamAsync(AggregateData data, SdsStream stream)
        {
            var dataList = new List<AggregateData>();
            dataList.Add(data);
            Console.WriteLine("Writing Data to " + stream.Id + " stream");
            
            using (var serializedData = new StringContent(JsonSerializer.Serialize(dataList)))
            {
                var responseWriteDataToStream =
                    await _httpClient.PostAsync($"http://localhost:{_port}/api/v1/Tenants/{_tenantId}/Namespaces/{_namespaceId}/Streams/{stream.Id}/Data", serializedData);
                CheckIfResponseWasSuccessful(responseWriteDataToStream);
            }
        }

        private static SdsTypeProperty CreateSdsTypePropertyOfTypeDouble(string idAndName, bool isKey)
        {
            var property = new SdsTypeProperty
            {
                Id = idAndName,
                Name = idAndName,
                IsKey = isKey,
                SdsType = new SdsType
                {
                    Name = Constants.DoubleTypeName,
                    SdsTypeCode = 14 // 14 is the SdsTypeCode for a Double type. Go to the SdsTypeCode section in EDS documentation for more information.
                }
            };

            return property;
        }

        private static SdsTypeProperty CreateSdsTypePropertyOfTypeDateTime(string idAndName, bool isKey)
        {
            var property = new SdsTypeProperty
            {
                Id = idAndName,
                Name = idAndName,
                IsKey = isKey,
                SdsType = new SdsType
                {
                    Name = Constants.DateTimeTypeName,
                    SdsTypeCode = 16 // 16 is the SdsTypeCode for a DateTime type. Go to the SdsTypeCode section in EDS documentation for more information.
                }
            };

            return property;
        }

        private static double GetValue(string json, string property)
        {
            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                var summaryElement = root.GetProperty(Constants.SummariesProperty);
                if (summaryElement.TryGetProperty(property, out JsonElement propertyElement))
                {
                    if (propertyElement.TryGetProperty(Constants.ValueProperty, out JsonElement valueElement))
                    {
                        Console.WriteLine("    " + property + " = " + valueElement.ToString());
                        return Convert.ToDouble(valueElement.ToString());
                    }
                }
                return 0;
            }
        }
    }
}
