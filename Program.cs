/* S-CODE ATTRIBUTION
TITLE: Guide for running C# Azure Functions in an isolated worker process
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
USAGE: HostBuilder configuration, isolated worker lifecycle, and ConfigureFunctionsWebApplication bootstrap.
*/

/* S-CODE ATTRIBUTION
TITLE: Dependency injection in .NET
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
USAGE: Service lifetime management registering TableClient as a thread-safe Singleton instance.
*/

/* S-CODE ATTRIBUTION
TITLE: Azure.Data.Tables client library samples
AUTHOR: Azure SDK for .NET Documentation
DATE: 2023
VERSION: No version specified
AVAILABLE: https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/tables/Azure.Data.Tables/samples
USAGE: Instantiation of TableServiceClient and TableClient.CreateIfNotExists pattern.
*/

/* S-CODE ATTRIBUTION
TITLE: Use the Azurite emulator for local Azure Storage development
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite
USAGE: Resolving environment configuration fallback targeting UseDevelopmentStorage=true for local emulation.
*/


using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Connect to local Azurite container via development storage string
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? "UseDevelopmentStorage=true";

        // Register TableClient for the 'MenuItems' table as a singleton
        services.AddSingleton(sp =>
        {
            var tableServiceClient = new TableServiceClient(connectionString);
            var tableClient = tableServiceClient.GetTableClient("MenuItems");
            tableClient.CreateIfNotExists();
            return tableClient;
        });
    })
    .Build();

host.Run();