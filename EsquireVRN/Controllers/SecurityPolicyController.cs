using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityPolicyController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreatePageDto dto)
        {
            long orgId = Shared.GetOrgID();
            var opage = await Shared.GetContentPageById(orgId, "security_policy");
            if (opage != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "There is a security policy page. Please try editing if you want to make changes to it." });
            }
            var page = new ContentPage
            {
                Type = "security_policy",
                OrgId = Shared.GetOrgID(),
                Content = dto.Content,
                Created_Date = DateTime.UtcNow
            };

            var id = await Shared.AddContentPage(page);
            var npage = await Shared.GetContentPageById(orgId, "security_policy");
            return Ok(npage?.Content);
        }

        [HttpGet]
        // GET: api/pages/{id}
        public async Task<IActionResult> Get()
        {
            long orgId = Shared.GetOrgID();
            var page = await Shared.GetContentPageById(orgId, "security_policy");

            if (page == null)
                return NotFound(new { error = "Security policy page doesn't exist. Please try creating one." });

            return Ok(page.Content);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdatePageDto dto)
        {
            long OrgId = Shared.GetOrgID();
            var existing = await Shared.GetContentPageById(OrgId, "security_policy");

            if (existing == null)
                return NotFound(new { error = "Security policy page doesn't exist. Please try creating one." });

            existing.Type = "security_policy";
            existing.OrgId = OrgId;
            existing.Content = dto.Content;
            existing.Updated_Date = DateTime.UtcNow;

            var success = await Shared.UpdateContentPage(OrgId, "security_policy", existing);

            var npage = await Shared.GetContentPageById(OrgId, "security_policy");
            return Ok(npage?.Content);
        }
    }
}
