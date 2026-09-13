/* S-CODE ATTRIBUTION
TITLE: Azure Functions HTTP trigger
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=isolated-process
USAGE: Implementing HTTP-triggered Azure Functions with custom route mapping in isolated worker model.
*/

/* S-CODE ATTRIBUTION
TITLE: Architectural Styles and the Design of Network-based Software Architectures (REST dissertation)
AUTHOR: Fielding, R.
DATE: 2000
VERSION: Dissertation
AVAILABLE: University of California, Irvine
USAGE: Standard HTTP status codes (200 OK, 201 Created, 204 NoContent, 400 BadRequest, 404 NotFound, 409 Conflict).
*/

/* S-CODE ATTRIBUTION
TITLE: TableClient.CreateQueryFilter and QueryAsync
AUTHOR: Azure SDK for .NET Reference
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient.createqueryfilter?view=azure-dotnet
USAGE: Generating parameterized OData filter expressions for safe querying against PartitionKey.
*/

/* S-CODE ATTRIBUTION
TITLE: System.Text.Json namespace - High-performance JSON processing in .NET
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/dotnet/api/system.text.json?view=net-8.0
USAGE: Deserializing asynchronous request streams using case-insensitive property name matching.
*/


using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class MenuEndpoints
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<MenuEndpoints> _logger;

        public MenuEndpoints(TableClient tableClient, ILogger<MenuEndpoints> logger)
        {
            _tableClient = tableClient;
            _logger = logger;
        }

        // 1. POST /api/menu - Inserts a new menu entity with comprehensive validation
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Processing incoming request to create a new menu item.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Request body cannot be empty.");
                return badRequest;
            }

            CreateMenuItemDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<CreateMenuItemDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Malformed JSON received in CreateMenuItem.");
                var badJson = req.CreateResponse(HttpStatusCode.BadRequest);
                await badJson.WriteStringAsync("Malformed JSON payload.");
                return badJson;
            }

            // Robust validation rules
            if (dto == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid menu item data.");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(dto.Category))
            {
                var badCategory = req.CreateResponse(HttpStatusCode.BadRequest);
                await badCategory.WriteStringAsync("Category (PartitionKey) is required and cannot be blank.");
                return badCategory;
            }

            if (string.IsNullOrWhiteSpace(dto.Sku))
            {
                var badSku = req.CreateResponse(HttpStatusCode.BadRequest);
                await badSku.WriteStringAsync("SKU (RowKey) is required and cannot be blank.");
                return badSku;
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                var badName = req.CreateResponse(HttpStatusCode.BadRequest);
                await badName.WriteStringAsync("Item Name is required.");
                return badName;
            }

            if (!dto.Price.HasValue || dto.Price.Value < 0)
            {
                var badPrice = req.CreateResponse(HttpStatusCode.BadRequest);
                await badPrice.WriteStringAsync("Price must be a non-negative numeric value.");
                return badPrice;
            }

            var entity = new MenuItemEntity
            {
                PartitionKey = dto.Category.Trim(),
                RowKey = dto.Sku.Trim().ToUpperInvariant(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Price = Math.Round(dto.Price.Value, 2),
                IsAvailable = dto.IsAvailable ?? true
            };

            try
            {
                await _tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Successfully created menu item {Sku} under category {Category}.", entity.RowKey, entity.PartitionKey);

                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 409)
            {
                _logger.LogWarning("Duplicate entity detected: SKU {Sku} already exists in {Category}.", entity.RowKey, entity.PartitionKey);
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync($"An item with SKU '{entity.RowKey}' already exists under category '{entity.PartitionKey}'.");
                return conflictResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating menu item {Sku}.", entity.RowKey);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An internal server error occurred while persisting the menu item.");
                return errorResponse;
            }
        }

        // GET /api/menu - Queries and returns all menu entities
        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Retrieving all menu items.");

            var items = new List<MenuItemEntity>();
            await foreach (var item in _tableClient.QueryAsync<MenuItemEntity>())
            {
                items.Add(item);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

    // GET /api/menu/category/{category} - Filters entities by PartitionKey
        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> GetMenuItemsByCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            _logger.LogInformation("Retrieving menu items for category: {Category}", category);

            if (string.IsNullOrWhiteSpace(category))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Category route parameter is required.");
                return badResponse;
            }

            var items = new List<MenuItemEntity>();

            // Build an OData query filter matching PartitionKey exactly
            string filter = TableClient.CreateQueryFilter($"PartitionKey eq {category}");

            await foreach (var item in _tableClient.QueryAsync<MenuItemEntity>(filter))
            {
                items.Add(item);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

        // 4. PUT /api/menu/{category}/{id} - Updates price or availability
        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> UpdateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category,
            string id)
        {
            _logger.LogInformation("Updating menu item {Id} in {Category}", id, category);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<UpdateMenuItemDto>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto == null || (dto.Price == null && dto.IsAvailable == null))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body must contain 'price' or 'isAvailable' to update.");
                return badResponse;
            }

            try
            {
                // Fetch the existing entity using PartitionKey and RowKey
                var existingEntity = await _tableClient.GetEntityAsync<MenuItemEntity>(category, id);
                var item = existingEntity.Value;

                if (dto.Price.HasValue)
                {
                    if (dto.Price < 0)
                    {
                        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        await badResponse.WriteStringAsync("Price cannot be negative.");
                        return badResponse;
                    }
                    item.Price = dto.Price.Value;
                }

                if (dto.IsAvailable.HasValue)
                {
                    item.IsAvailable = dto.IsAvailable.Value;
                }

                // Update entity in Azure Table Storage
                await _tableClient.UpdateEntityAsync(item, item.ETag, TableUpdateMode.Replace);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(item);
                return response;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Item with SKU '{id}' in category '{category}' was not found.");
                return notFoundResponse;
            }
        }

        // 5. DELETE /api/menu/{category}/{id} - Removes an item from the menu
        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> DeleteMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category,
            string id)
        {
            _logger.LogInformation("Deleting menu item {Id} in {Category}", id, category);

            try
            {
                // Verify existence first so we can return a proper 404 if it does not exist
                var existingEntity = await _tableClient.GetEntityAsync<MenuItemEntity>(category, id);

                await _tableClient.DeleteEntityAsync(category, id, existingEntity.Value.ETag);

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Item with SKU '{id}' in category '{category}' was not found.");
                return notFoundResponse;
            }
        }
    }
}