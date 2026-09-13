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