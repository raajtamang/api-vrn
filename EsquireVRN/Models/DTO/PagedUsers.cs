namespace EsquireVRN.Models.DTO
{
    public class PagedUsers
    {
        public long page_count { get; set; } = 1;
        public List<Customer>? Users { get; set; }
    }
}
