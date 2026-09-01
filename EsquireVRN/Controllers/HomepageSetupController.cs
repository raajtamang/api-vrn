using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
   [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class HomepageSetupController : ControllerBase
    {
        // GET: api/<HomepageSetupController>
        [HttpGet]
        public IActionResult Get()
        {
            List<HomepageSetup> setups = Shared.GetHomepageSetups();
            return Ok(setups);
        }

        // GET api/<HomepageSetupController>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var setups = Shared.GetHomepageSetup(id);
            return Ok(setups);
        }

        // POST api/<HomepageSetupController>
        [HttpPost]
        public IActionResult Post([FromBody] HomepageSetup setup)
        {
            try
            {
                if (setup.Position != null)
                {
                    var old_homepageSetup = Shared.GetHomepageSetupByPosition(setup.Position);
                    if (old_homepageSetup != null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "There is a section at this position. Please check and try again." });
                    }
                }
                else
                {
                    setup.Position = Shared.GetHomepageSetups().Count;
                }
                setup.CreateDate = DateTime.UtcNow.AddHours(2);
                setup.OrgID = Shared.GetOrgID();
                setup.Status = setup.Status == null ? true : setup.Status;
                var newPage = Shared.SaveHomepageSetup(setup);
                return Ok(newPage);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }

        }

        // PUT api/<HomepageSetupController>/5
        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody] HomepageSetup setup)
        {
            try
            {
                var old_homepageSetup = Shared.GetHomepageSetup(id);
                if (old_homepageSetup == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "There is no section with this id. Please check and try again." });
                }
                setup.Position ??= Shared.GetHomepageSetups().Count;               
                setup.OrgID = old_homepageSetup.OrgID;
                setup.CreateDate = old_homepageSetup.CreateDate;
                var updated_setup = Shared.UpdateHomepageSetup(id, setup);
                return Ok(updated_setup);

            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }
        }

        // DELETE api/<HomepageSetupController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            try
            {
                var old_homepageSetup = Shared.GetHomepageSetup(id);
                if (old_homepageSetup == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "There is no section with this id. Please check and try again." });
                }

                Shared.DeleteHomepageSetup(id);
                return Ok(new { message = "Homepage Section removed successfully." });

            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong with the server. Please try again." });
            }
        }

        [HttpPost]
        [Route("OrderSections")]
        public IActionResult OrderSections([FromBody] HomepageSectionIdList menuIdList)
        {
            try
            {
                List<Shared.SelectOption> IdPositionList = new();
                int postion = 1;
                foreach (var item in menuIdList.Ids)
                {
                    Shared.SelectOption selectOption = new()
                    {
                        Id = item,
                        Position = postion
                    };
                    IdPositionList.Add(selectOption);
                    postion++;
                }
                List<HomepageSetup> menus = Shared.UpdateHomePageSectionOrder(IdPositionList);
                return Ok(menus);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

        }

        public class HomepageSectionIdList
        {
            public required List<long> Ids { get; set; }
        }

       
    }
}
