namespace EsquireVRN.Models.DTO
{
    public class CreateDeliveryDTO
    {
        public required long DeliveryDescID { get; set; }
        public long? OrgID { get; set; }
        public string? Area { get; set; }
    }
}
