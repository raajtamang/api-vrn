using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class PageImage
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public long Id { get; set; }
        [MaxLength(250)]
        public string Title { get; set; }
        [MaxLength(250)]
        public string Image { get; set; }
        [MaxLength(350)]
        public string Url { get; set; }
        public DateTime CreatedDate { get; set; }
    } 
}
