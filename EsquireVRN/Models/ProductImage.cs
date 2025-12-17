using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EsquireVRN.Models
{
    public class ProductImage
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public long ProdImgID { get; set; }
        public long ProdID { get; set; }
        public string? ImageURL { get; set; }
        public string? BigImageURL { get; set; }
        [NotMapped] 
        public List<IFormFile>? Image { get; set; }
    }
}
