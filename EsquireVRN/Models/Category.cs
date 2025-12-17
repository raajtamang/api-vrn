using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Category
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]

        public long Id { get; set; }
        public string? Title { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }
        public long? ItemCount { get; set; }
        public bool? Featured { get; set; }
        public long? Position { get; set; }
    }
}
