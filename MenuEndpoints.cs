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
    }
}