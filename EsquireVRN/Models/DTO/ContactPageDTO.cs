namespace EsquireVRN.Models.DTO
{
    public class ContactPageDTO
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Facebook { get; set; }
        public string? Twitter { get; set; }
        public string? Youtube { get; set; }
        public string? LinkedIn { get; set; }
        public string? Instagram { get; set; }
        public string? Map_IFrame { get; set; }
        public long OrgId { get; set; }
        public string? WebsiteName { get; set; }
        public string? WebsiteDescription { get; set; }
        public DateTime Created_Date { get; set; } = DateTime.Now;
        public DateTime? Updated_Date { get; set; }
        public string? WebsiteLogoURL { get; set; }
        public IFormFile? Logo { get; set; }
    }
}
