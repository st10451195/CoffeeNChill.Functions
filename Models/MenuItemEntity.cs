/* S-CODE ATTRIBUTION
TITLE: Get started with Azure Table storage and the Azure Cosmos DB Table API using .NET
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/azure/cosmos-db/table/how-to-use-net
USAGE: Implementation of the ITableEntity interface, property mapping, and ETag handling.
*/

/* S-CODE ATTRIBUTION
TITLE: Azure.Data.Tables Namespace - Azure SDK for .NET
AUTHOR: Microsoft Learn
DATE: 2023
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables?view=azure-dotnet
USAGE: Utilizing the Azure.Data.Tables library and its core data contracts.
*/

/* S-CODE ATTRIBUTION
TITLE: Pro C# 10 with .NET 6: Foundational Principles and Practices in Programming
AUTHOR: Troelsen, A. and Japikse, P.
DATE: 2022
VERSION: 11th Edition
AVAILABLE: Apress
USAGE: Principles of encapsulation, automatic properties, and default value initialization in C#.
*/

/* S-CODE ATTRIBUTION
TITLE: Pro ASP.NET Core 6: Develop Cloud-Ready Web Applications Using MVC, Blazor, and Razor Pages
AUTHOR: Freeman, A.
DATE: 2020
VERSION: 9th Edition
AVAILABLE: Apress
USAGE: Domain model design patterns and data contract structuring for cloud-native web APIs.
*/


using Azure;
using Azure.Data.Tables;

namespace CoffeeNChill.Functions.Models
{
    public class MenuItemEntity : ITableEntity
    {
        // Category (e.g. "Hot Drinks", "Pastries")
        public string PartitionKey { get; set; } = string.Empty;

        // Unique SKU / ID (e.g. "COF-001", "PAS-104")
        public string RowKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}