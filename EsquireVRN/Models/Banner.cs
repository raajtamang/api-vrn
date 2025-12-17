using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class Banner
    {
        [Key]
        public long Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public long? OrgID { get; set; }
        public DateTime CreateDate { get; set; }

    }
}
