namespace EsquireVRN.Models
{
    public class Menu
    {
        public int Id { get; set; }
        public string Department { get; set; }
        public string Contents { get; set; }
        public DateTime? Date { get; set; }
        public long? OrgId { get; set; }
        public string? ImageUrl { get;set; }
        public int? Position { get; set; }
    }
}
