using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Slider
    {
        [Key]
        [SwaggerSchema(ReadOnly = true)]
        public int Id { get; set; }
        public string SliderType { get; set; }
        public long OrgId { get; set; }
        public string Content { get; set; }
    }
}
