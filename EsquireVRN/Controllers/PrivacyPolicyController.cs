using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrivacyPolicyController : ControllerBase
    {       
        [HttpGet]
        // GET: api/pages/{id}
        public async Task<IActionResult> Get()
        {
            long orgId = Shared.GetOrgID();
            var page = await Shared.GetContentPageById(orgId, "privacy_policy");

            string? Content = null;
            if (page == null)
                return Ok(new { Content });

            var FAQs = await Shared.GetAllPageContentFAQByPageId(page.Id);
            return Ok(new { content = page.Content, id=page.Id, faqs = FAQs });
        }

        [HttpPut]
        [Authorize(Roles = "Reseller")]
        public async Task<IActionResult> Put([FromBody] UpdatePageDto dto)
        {
            long OrgId = Shared.GetOrgID();
            var existing = await Shared.GetContentPageById(OrgId, "privacy_policy");

            if (existing == null)
            {
                var page = new ContentPage
                {
                    Type = "privacy_policy",
                    OrgId = Shared.GetOrgID(),
                    Content = dto.Content,
                    Created_Date = DateTime.UtcNow
                };

                var id = await Shared.AddContentPage(page);
                var npage = await Shared.GetContentPageById(OrgId, "privacy_policy");
                return Ok(npage?.Content);
            }
            else
            {
                existing.Type = "privacy_policy";
                existing.OrgId = OrgId;
                existing.Content = dto.Content;
                existing.Updated_Date = DateTime.UtcNow;

                var success = await Shared.UpdateContentPage(OrgId, "privacy_policy", existing);

                var npage = await Shared.GetContentPageById(OrgId, "privacy_policy");
                return Ok(npage?.Content);
            }
        }
    }
}
