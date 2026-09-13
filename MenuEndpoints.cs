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

        // POST /api/menu - Inserts a new menu entity
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Processing request to create a new menu item.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<CreateMenuItemDto>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Category) ||
                string.IsNullOrWhiteSpace(dto.Sku) ||
                string.IsNullOrWhiteSpace(dto.Name) ||
                dto.Price == null || dto.Price < 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Validation failed: Category, Sku, Name, and a valid non-negative Price are required.");
                return badResponse;
            }

            var entity = new MenuItemEntity
            {
                PartitionKey = dto.Category.Trim(),
                RowKey = dto.Sku.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description ?? string.Empty,
                Price = dto.Price.Value,
                IsAvailable = dto.IsAvailable ?? true
            };

            await _tableClient.AddEntityAsync(entity);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(entity);
            return response;
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