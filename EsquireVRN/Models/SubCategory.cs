using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class SubCategory
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public long Id { get; set; }
        public long Category_Id { get; set; }
        public string Title { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}
