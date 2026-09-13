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