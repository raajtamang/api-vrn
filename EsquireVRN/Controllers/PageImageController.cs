using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PageImageController : ControllerBase
    {
        // GET: api/<PageImageController>
        [HttpGet]
        public IActionResult Get(int? pageSize, int? pageNum)
        {
            pageSize ??= 12;
            pageNum ??= 1;
            return Ok(Shared.GetProductImages("", pageSize, pageNum));
        }

        // GET api/<PageImageController>/5
        [HttpGet]
        [Route("Search")]
        public IActionResult SearchImage(string searchText)
        {           
            return Ok(Shared.SearchProductImages(searchText));
        }
    }
}
