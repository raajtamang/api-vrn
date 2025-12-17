using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class PromotionSpecialPage
    {
        [Key]
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public bool Active { get; set; }       
        public string ShortDescription { get; set; }
        public string PageType { get;set; }
        public string MetaDescription { get; set; }
        [SwaggerSchema(ReadOnly = true)]
        public string? MetaTitle { get; set; }
        public bool? Demo { get; set; }
        public bool? Expired { get; set; }
    }
}
