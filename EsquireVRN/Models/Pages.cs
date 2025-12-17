using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Pages
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(150),Required]
        public string Title { get; set; }
        [MaxLength(250), Required]
        public string ShortDescription { get; set; }
        [MaxLength(250), Required]
        public string MetaDescription { get; set; }
        public string Tags { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        [SwaggerSchema(ReadOnly =true)]
        public string? MetaTitle { get; set; }
        public string[]? Product_Code { get; set; }

    }
}
