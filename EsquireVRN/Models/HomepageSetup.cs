using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class HomepageSetup
    {
        [Key]
        public long Id { get; set; }
        public string? ContentType { get; set; }
        public long ContentId { get; set; }
        public string? Title { get; set; }
        public int? Position { get; set; }
        public bool? Status { get; set; }
        public DateTime CreateDate { get; set; }
        public long? OrgID { get; set; }
    }
}
