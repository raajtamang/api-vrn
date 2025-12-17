using EsquireVRN.Utils;
using EsquireVRN.Models;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SliderController : ControllerBase
    {
        // GET: api/<SliderController>
        [HttpGet]
        public IEnumerable<Slider> Get()
        {
            return Shared.GetSlider();
        }

        [HttpGet]
        [Route("ClientSlider")]
        public IEnumerable<Slider> GetClientSlider()
        {
            return Shared.GetClientSlider();
        }

        [HttpGet]
        [Route("GetResellerSlider")]
        public IEnumerable<Slider> GetResellerSlider()
        {
            return Shared.GetResellerSlider();
        }      
    }
}
