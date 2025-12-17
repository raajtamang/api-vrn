using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class MenuController : ControllerBase
    {
        // GET: api/<MenuController>
        [HttpGet]
        public IEnumerable<Menu> Get()
        {
            return Shared.GetMenu();
        }       
    }
}
