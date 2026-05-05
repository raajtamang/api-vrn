namespace EsquireVRN.Models.DTO
{
    public class DashboardCard
    {
        public long Brands { get; set; } = 0;
        public long Categories { get; set; } = 0;
        public long Orders { get; set; }= 0;
        public long Customers { get; set; } = 0;
        public decimal Sales { get; set; } = 0;
    }
}
