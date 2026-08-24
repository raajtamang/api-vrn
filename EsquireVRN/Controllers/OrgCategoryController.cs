using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Reseller")]
    public class OrgCategoryController : ControllerBase
    {
        // =========================
        // CREATE
        // POST: api/OrgCategory
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrgCategory model)
        {
            if (model == null)
            {
                return BadRequest("Invalid request.");
            }

            if (model.OrgId <= 0)
            {
                return BadRequest("OrgId is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                return BadRequest("Category is required.");
            }

            try
            {
                var id = await Shared.CreateOrgCategory(model);

                var nOrgCategory = GetById(id);

                return Ok(new { nOrgCategory, message = "Organisation category added successfully." });
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
                return BadRequest("Invalid Id.");
            }

            try
            {
                var result = await Shared.GetOrgCategoryById(id);

                if (result == null)
                {
                    return NotFound(
                        $"Organisation Category with Id {id} does not exist.");
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
                return BadRequest("Invalid OrgId.");
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
            [FromBody] OrgCategory model)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid Id.");
            }

            if (model == null)
            {
                return BadRequest("Invalid request.");
            }

            if (model.OrgId <= 0)
            {
                return BadRequest("OrgId is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                return BadRequest("Category is required.");
            }

            try
            {
                // Make sure the URL Id is used
                model.Id = id;

                // Check whether record exists
                var existing = await Shared.GetOrgCategoryById(id);

                if (existing == null)
                {
                    return NotFound(
                        $"OrgCategory with Id {id} was not found.");
                }

                var updated = await Shared.UpdateOrgCategory(model);

                if (!updated)
                {
                    return NotFound(
                        $"OrgCategory with Id {id} was not found.");
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
                return BadRequest("Invalid Id.");
            }

            try
            {
                // Check whether record exists
                var existing = await Shared.GetOrgCategoryById(id);

                if (existing == null)
                {
                    return NotFound(
                        $"OrgCategory with Id {id} was not found.");
                }

                var deleted = await Shared.DeleteOrgCategory(id);

                if (!deleted)
                {
                    return NotFound(
                        $"OrgCategory with Id {id} was not found.");
                }

                return Ok(new
                {
                    message = "OrgCategory deleted successfully.",
                    id = id
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
