using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class PopupMessage
    {
        [Key]
        public int Id { get; set; }
        public string Content { get; set; }
        [MaxLength(20)]
        public string Type { get; set; }
        [MaxLength(25)]
        public string PopupFor { get; set; }
    }
}
