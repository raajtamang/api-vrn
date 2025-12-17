using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Brand
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Logo { get; set; }
        public string? Link { get; set; }
        public string? MetaTitle{ get; set; }
        public string? MetaDescription { get; set;}
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
        public bool? Featured { get; set; }
        public long? Position { get; set; }
    }
}
