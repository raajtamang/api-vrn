namespace EsquireVRN.Models.DTO
{
    public class PagedWebOrderWithCustomer
    {
        public List<WebOrderWithCustomer>? Orders { get; set; }
        public long page_count { get; set; } = 1;
    }
}
