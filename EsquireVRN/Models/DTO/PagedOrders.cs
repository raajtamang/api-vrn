namespace EsquireVRN.Models.DTO
{
    public class PagedOrders
    {
        public int page_count { get; set; } = 1;
        public List<OrderWithCustomer>? Orders { get; set; }
    }
}
