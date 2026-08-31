using EsquireVRN.Utils;
using EsquireVRN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   // [Authorize(Roles ="Reseller")]
    public class BannerController : ControllerBase
    {
        // GET: api/<BannerController>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(Shared.GetBanners());
        }

        // GET api/<BannerController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var banner = Shared.GetBanner(id);
            if (banner == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Banner doesn't exist." });
            }
            return Ok(banner);
        }

        // POST api/<BannerController>
        [HttpPost]
        public IActionResult Post([FromBody] Banner banner)
        {
            try
            {
                banner.OrgID = Shared.GetOrgID();
                banner.CreateDate = DateTime.UtcNow.AddHours(2);
                var n_banner = Shared.CreateBanner(banner);
                return Ok(n_banner);
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }
        }

        // PUT api/<BannerController>/5
        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody] Banner banner)
        {
            try
            {
                var o_banner = Shared.GetBanner(id);
                if (o_banner == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "Banner doesn't exist." });
                }
                banner.OrgID = o_banner.OrgID;
                banner.CreateDate = o_banner.CreateDate;
                var n_banner = Shared.UpdateBanner(id, banner);
                return Ok(n_banner);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }
        }

        // DELETE api/<BannerController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            try
            {
                var o_banner = Shared.GetBanner(id);
                if (o_banner == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "Banner doesn't exist." });
                }
                Shared.DeleteBanner(id);
                return Ok(new { message = "Banner removed successfully" });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }
        }
    }
}
