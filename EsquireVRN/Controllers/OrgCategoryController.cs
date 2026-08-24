using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Reseller")]
    public class OrgCategoryController : ControllerBase
    {
        // =========================
        // CREATE
        // POST: api/OrgCategory
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrgCategoryCreateDTO model)
        {
            if (model == null)
            {
                return BadRequest(new { error = "Invalid request." });
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                return BadRequest(new { error = "Category is required." });
            }

            try
            {
                var oOrgCategory = await Shared.GetOrgCategoriesByOrgId(Shared.GetOrgID());
                if (oOrgCategory != null && oOrgCategory.Any())
                {
                    var orgCategory = oOrgCategory.FirstOrDefault();

                    var updated = await Shared.UpdateOrgCategory(orgCategory.Id, model);
                    var nOrgCategory = await Shared.GetOrgCategoryById(orgCategory.Id);
                    return Ok(new { OrganisationCategory = nOrgCategory, message = "Organisation category updated successfully." });
                }
                else
                {

                    var id = await Shared.CreateOrgCategory(model);

                    var nOrgCategory = await Shared.GetOrgCategoryById(id);

                    return Ok(new { OrganisationCategory = nOrgCategory, message = "Organisation category added successfully." });

                }
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }


        // =========================
        // READ - Get by ID
        // GET: api/OrgCategory/1
        // =========================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid Id." });
            }

            try
            {
                var result = await Shared.GetOrgCategoryById(id);

                if (result == null)
                {
                    return NotFound(
                        new { error = $"Organisation Category with Id {id} does not exist." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }


        // =========================
        // READ - Get all
        // GET: api/OrgCategory
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await Shared.GetOrgCategories();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }


        // =========================
        // READ - Get by OrgId
        // GET: api/OrgCategory/org/10
        // =========================
        [HttpGet("org/{orgId:long}")]
        public async Task<IActionResult> GetByOrgId(long orgId)
        {
            if (orgId <= 0)
            {
                return BadRequest(new { error = "Invalid OrgId." });
            }

            try
            {
                var result = await Shared.GetOrgCategoriesByOrgId(orgId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }


        // =========================
        // UPDATE
        // PUT: api/OrgCategory/1
        // =========================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] OrgCategoryCreateDTO model)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid Id." });
            }

            if (model == null)
            {
                return BadRequest(new { error = "Invalid request." });
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                return BadRequest(new { error = "Category is required." });
            }

            try
            {

                // Check whether record exists
                var existing = await Shared.GetOrgCategoryById(id);

                if (existing == null)
                {
                    return NotFound(
                        new { error = $"OrgCategory with Id {id} was not found." });
                }

                var updated = await Shared.UpdateOrgCategory(id, model);

                if (!updated)
                {
                    return NotFound(
                        new { error = $"OrgCategory with Id {id} was not found." });
                }

                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }


        // =========================
        // DELETE
        // DELETE: api/OrgCategory/1
        // =========================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid Id." });
            }

            try
            {
                // Check whether record exists
                var existing = await Shared.GetOrgCategoryById(id);

                if (existing == null)
                {
                    return NotFound(
                        new { error = $"OrgCategory with Id {id} was not found." });
                }

                var deleted = await Shared.DeleteOrgCategory(id);

                if (!deleted)
                {
                    return NotFound(
                        new { error = $"OrgCategory with Id {id} was not found." });
                }

                return Ok(new
                {
                    message = new
                    {
                        error = "OrgCategory deleted successfully."
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }
    }
}
