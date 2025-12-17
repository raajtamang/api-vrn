namespace EsquireVRN.Models
{
    public class PagedOrderWithCustomer
    {
        public long PageCount { get; set; }
        public List<OrderWithCustomer>? Order { get; set; }
    }
}
