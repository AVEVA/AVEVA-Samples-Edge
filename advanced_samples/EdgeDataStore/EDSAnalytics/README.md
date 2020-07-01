# EDS Analytics Sample and Test

**Version:**: 1.0.0

[![Build Status](https://dev.azure.com/osieng/engineering/_apis/build/status/product-readiness/Edge/EDS_Analytics_DotNet?branchName=master)](https://dev.azure.com/osieng/engineering/_build?definitionId=1827&branchName=master)

This sample demonstrates how to build an application that can run analytics against streams in Edge Data Store and write results back to Edge Data Store.

## Requirements

- .NET Core 3.1 must be installed
- Edge Data Store must be installed and running on the local machine.

## Running the sample

### Using Visual Studio

1. Load the .csproj in this directory
1. Rebuild project
1. Run it
1. The outputs tell you which step has completed/ if an error has occurred

### Using Command Line

- Make sure you have the install location of dotnet added to your path
- Run the following command from the location of this project:

```shell
dotnet run
```

## Running the automated test

### Test from Visual Studio

- Load the .csproj from the EDSAnalytics directory above this in Visual Studio
- Rebuild project
- Open Test Explorer and make sure there is one test called EDSAnalyticsTest showing
- Run the test

### Test from Command Line

- Make sure you have the install location of dotnet added to your path
- Run the following command from the location of the EDSTest project:

```shell
dotnet test
```

For the main EDS page [ReadMe](https://osisoft.github.io/Edge-Data-Store-Docs/V1/)  
For the main samples page on master [ReadMe](https://github.com/osisoft/OSI-Samples)

## Sample Application Contents

### DeadBand Filter

The purpose of this portion is to demonstrate the EDS's ability to apply a filter to a stream. This application reads in data from a stream of sine wave data (between 1.0 and -1.0)
and filters out the values between -0.9 to 0.9. The data that is left is sent to a new stream in EDS. This is an example of how exception reporting can be used with EDS.

Step 1

- Creates the SineWave type using SDS.

Step 2

- Creates the SineWave stream using SDS.

Step 3

- New events are initialized with sine wave data ranging from -1.0 to 1.0. This data is sent to the SineWave stream using SDS

Step 4

- Data from the SineWave stream is ingressed and stored in a list of SineWave objects. Since data is encoded using GZIP, some decoding is necessary.

Step 5

- Creates the FilteredSineWave stream using SDS.

Step 6

- The filter is a deadband filter that accepts only data greater than 0.9 or less than -0.9. This step can be altered to be any type of filter.
  The data is copied into a new list of SineWave objects and is sent to the FilteredSineWave stream.

### Data Aggregation

This portion will read data from a stream of sine wave points, calculate the mean, minimum, maximum, and range, and write the result to a new stream.
It also uses EDS's standard data aggregate API calls to return the mean, minimum, maximum, and standard deviation. For more information on the EDS's standard data aggregate API reference: [EDS Summaries](https://osisoft.github.io/Edge-Data-Store-Docs/V1/SDS/Reading_Data_API.html#get-summaries)

Step 7

- Creates the AggregatedData type using SDS.

Step 8

- Creates the CalculatedAggregatedData stream using SDS.

Step 9

- Calculates mean, minimum, maximum, and range using standard c# functions. This data is sent to the AggregatedData stream using SDS.

Step 10

- Creates the EdsApiAggregatedData stream using SDS

Step 11

- Data is ingressed from EDS�s standard data aggregate API calls and stored in an object. Since data is encoded using GZIP, some decoding is completed to extract the mean, minimum, maximum, and range. This data is copied into an AggregateData object and sent to the EdsApiAggregatedData stream

Step 12

- The Types and Streams created by the application are deleted.

---

Tested against DotNet 3.1.

For the Edge Data Store advanced samples landing page [ReadMe](../)  
For the Edge advanced samples landing page [ReadMe](../../)  
For the main Edge page [ReadMe](https://github.com/osisoft/OSI-Samples-Edge)  
For the main samples page on master [ReadMe](https://github.com/osisoft/OSI-Samples)
