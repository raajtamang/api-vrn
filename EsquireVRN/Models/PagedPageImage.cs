using EsquireVRN.Models;

namespace EsquireVRN.Models
{
    public class PagedPageImage
    {
        public List<PageImage>? Images { get; set; }
        public long PageCount { get; set; }
    }
}
