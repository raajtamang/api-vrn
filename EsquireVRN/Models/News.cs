using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace EsquireVRN.Models
{
    public class News
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public long Id { get; set; }
        [MaxLength(250)]
        public string Title { get; set; }
        [MaxLength(500)]
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        [MaxLength(250)]
        public string? MetaTitle { get; set; }
        public DateTime? CreatedDate { get; set; }
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        [MaxLength(250)]
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        [MaxLength(500)]
        public string? LastUpdatedBy { get; set; }
        public IFormFile? Image { get; set; }
    }
}
