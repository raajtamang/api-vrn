using EsquireVRN.Utils;
using EsquireVRN.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PopupController : ControllerBase
    {
        // GET: api/<PopupController>
        [HttpGet]
        public IEnumerable<PopupMessage> Get()
        {
            return Shared.GetPopup();
        }
        [HttpGet("{popupFor}")]
        public PopupMessage Get(string popupFor)
        {
            return Shared.GetPopupFor(popupFor);
        }       
    }
}
