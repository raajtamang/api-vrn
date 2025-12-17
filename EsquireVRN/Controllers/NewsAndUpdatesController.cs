using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsAndUpdatesController : ControllerBase
    {
        // GET: api/<NewsAndUpdatesController>
        [HttpGet]
        public IEnumerable<News> Get()
        {
            return Shared.GetNews(); ;
        }

        // GET api/<NewsAndUpdatesController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            News news = Shared.GetNewsDetail(id);
            if (news == null)
            {
                return NotFound(new { error = "Product specification doesn't exist anymore." });
            }
            return Ok(news);
        }

        [HttpGet]
        [Route("DetailByMetaTitle")]
        public IActionResult ByMetaTitle(string meta_title)
        {
            News news = Shared.GetNewsDetailByMetaTitle(meta_title);
            if (news == null)
            {
                return NotFound(new { error = "Product specification doesn't exist anymore." });
            }
            return Ok(news);
        }
    }
}
