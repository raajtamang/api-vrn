using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class NormalSpecialPage
    {
        [Key]
        public long Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime? Date { get; set; }
        [SwaggerSchema(ReadOnly = true)]
        public string? MetaTitle { get;set; }
        public string? PageType { get; set; }
    }
}
