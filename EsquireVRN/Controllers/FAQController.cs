using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class FAQController : ControllerBase
    {
        //[HttpPost]
        //public async Task<IActionResult> Post([FromBody] CreatePageDto dto)
        //{
        //    long orgId = Shared.GetOrgID();
        //    var opage = await Shared.GetContentPageById(orgId, "faq");
        //    if (opage != null)
        //    {
        //        return StatusCode(StatusCodes.Status403Forbidden, new { error = "There is a FAQ page. Please try editing if you want to make changes to it." });
        //    }
        //    var page = new ContentPage
        //    {
        //        Type = "faq",
        //        OrgId = Shared.GetOrgID(),
        //        Content = dto.Content,
        //        Created_Date = DateTime.UtcNow
        //    };

        //    var id = await Shared.AddContentPage(page);
        //    var npage = await Shared.GetContentPageById(orgId, "faq");
        //    return Ok(npage?.Content);
        //}

        [HttpGet]
        // GET: api/pages/{id}
        public async Task<IActionResult> Get()
        {
            long orgId = Shared.GetOrgID();
            var page = await Shared.GetContentPageById(orgId, "faq");

            if (page == null)
                return NotFound(new { error = "FAQ page doesn't exist. Please try creating one." });

            return Ok(page.Content);
        }

        [HttpPut]
        [Authorize(Roles ="Reseller")]
        public async Task<IActionResult> Put([FromBody] UpdatePageDto dto)
        {
            long OrgId = Shared.GetOrgID();
            var existing = await Shared.GetContentPageById(OrgId, "faq");

            if (existing == null)
            {
                var page = new ContentPage
                {
                    Type = "faq",
                    OrgId = Shared.GetOrgID(),
                    Content = dto.Content,
                    Created_Date = DateTime.UtcNow
                };

                var id = await Shared.AddContentPage(page);
                var npage = await Shared.GetContentPageById(OrgId, "faq");
                return Ok(npage?.Content);
            }
            else
            {

                existing.Type = "faq";
                existing.OrgId = OrgId;
                existing.Content = dto.Content;
                existing.Updated_Date = DateTime.UtcNow;

                var success = await Shared.UpdateContentPage(OrgId, "faq", existing);

                var npage = await Shared.GetContentPageById(OrgId, "faq");
                return Ok(npage?.Content);
            }
        }
    }
}
