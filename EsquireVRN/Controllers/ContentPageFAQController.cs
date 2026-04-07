using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentPageFAQController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContentPageFAQ model)
        {
            try
            {
                model.Created_Date = DateTime.UtcNow;
                model.Updated_Date = DateTime.UtcNow;

                var result = await Shared.CreatePageContentFAQ(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{page_id}")]
        public async Task<IActionResult> Get(long page_id)
        {
            try
            {
                var faqs = await Shared.GetAllPageContentFAQByPageId(page_id);
                return Ok(faqs);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] ContentPageFAQ model)
        {
            try
            {
                model.Id = id;
                model.Updated_Date = DateTime.UtcNow;

                var result = await Shared.UpdatePageContentFAQ(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await Shared.DeletePageContentFAQ(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}