using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarrentyCentersController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreatePageDto dto)
        {
            long orgId = Shared.GetOrgID();
            var opage = await Shared.GetContentPageById(orgId, "warrenty_centers");
            if (opage != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "There is a warrenty center page. Please try editing if you want to make changes to it." });
            }
            var page = new ContentPage
            {
                Type = "warrenty_centers",
                OrgId = Shared.GetOrgID(),
                Content = dto.Content,
                Created_Date = DateTime.UtcNow
            };

            var id = await Shared.AddContentPage(page);
            var npage = await Shared.GetContentPageById(orgId, "warrenty_centers");
            return Ok(npage?.Content);
        }

        [HttpGet]
        // GET: api/pages/{id}
        public async Task<IActionResult> Get()
        {
            long orgId = Shared.GetOrgID();
            var page = await Shared.GetContentPageById(orgId, "warrenty_centers");

            if (page == null)
                return NotFound(new { error = "Warrenty center page doesn't exist. Please try creating one." });

            return Ok(page.Content);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdatePageDto dto)
        {
            long OrgId = Shared.GetOrgID();
            var existing = await Shared.GetContentPageById(OrgId, "warrenty_centers");

            if (existing == null)
                return NotFound(new { error = "Warrenty center page doesn't exist. Please try creating one." });

            existing.Type = "warrenty_centers";
            existing.OrgId = OrgId;
            existing.Content = dto.Content;
            existing.Updated_Date = DateTime.UtcNow;

            var success = await Shared.UpdateContentPage(OrgId, "warrenty_centers", existing);

            var npage = await Shared.GetContentPageById(OrgId, "warrenty_centers");
            return Ok(npage?.Content);
        }
    }
}
