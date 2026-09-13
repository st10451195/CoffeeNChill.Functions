/* S-CODE ATTRIBUTION
TITLE: Patterns of Enterprise Application Architecture
AUTHOR: Fowler, M.
DATE: 2002
VERSION: Addison-Wesley Professional
AVAILABLE: Addison-Wesley
USAGE: Data Transfer Object (DTO) architectural design pattern separating network models from storage entities.
*/

/* S-CODE ATTRIBUTION
TITLE: Model validation in ASP.NET Core
AUTHOR: Microsoft Learn
DATE: 2024
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
USAGE: Structuring decoupled request contract schemas to facilitate defensive input validation.
*/

/* S-CODE ATTRIBUTION
TITLE: C# in Depth
AUTHOR: Skeet, J.
DATE: 2019
VERSION: 4th Edition
AVAILABLE: Manning Publications
USAGE: Nullable reference types and nullable value types (double?, bool?) to distinguish missing vs default values.
*/

/* S-CODE ATTRIBUTION
TITLE: JSON serialization and deserialization in C# - .NET
AUTHOR: Microsoft Learn
DATE: 2023
VERSION: No version specified
AVAILABLE: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/
USAGE: Defining PascalCase properties mapped dynamically from incoming camelCase/case-insensitive HTTP request bodies.
*/


using System;
using System.Collections.Generic;
using System.Text;

namespace CoffeeNChill.Functions.Models
{
    public class CreateMenuItemDto
    {
        public string? Category { get; set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public bool? IsAvailable { get; set; }
    }

    public class UpdateMenuItemDto
    {
        public double? Price { get; set; }
        public bool? IsAvailable { get; set; }
    }
}