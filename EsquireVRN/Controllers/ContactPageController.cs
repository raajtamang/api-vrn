using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactPageController : ControllerBase
    {
        [HttpPost("create")]
        public IActionResult Create([FromBody] ContactPage model)
        {

            try
            {
                long orgId = Shared.GetOrgID();
                var data = Shared.GetContactPageById(orgId);

                if (data != null)
                    return NotFound(new { error = "Contact page contents exist. Please try updating." });

                model.OrgId = orgId;
                var result = Shared.CreateContactPage(model);
                var rPage = Shared.GetContactPageById(orgId);
                return Ok(rPage);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong. Please try again." });
            }
        }

        [HttpGet]
        public IActionResult GetById()
        {
            long orgId = Shared.GetOrgID();
            var data = Shared.GetContactPageById(orgId);

            if (data == null)
                return NotFound(new { error = "Contact page contents doesn't exist. Please try creating one." });

            return Ok(data);
        }

        [HttpPut("update")]
        public IActionResult Update([FromBody] ContactPage model)
        {
            try
            {
                long orgId = Shared.GetOrgID();
                var data = Shared.GetContactPageById(orgId);

                if (data == null)
                    return NotFound(new { error = "Contact page contents doesn't exist. Please try creating one." });
                model.OrgId = orgId;
                var result = Shared.UpdateContactPage(model);
                var cPage=Shared.GetContactPageById(orgId);
                return Ok(cPage);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong. Please try again." });
            }
        }
    }
}
