using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EFTPaymentSetupController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await Shared.GetAllEFTPaymentSetupsAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving EFT payment setups.",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get EFT payment setup by Id.
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await Shared.GetEFTPaymentSetupByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        Message = "EFT payment setup not found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving the EFT payment setup.",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get EFT payment setups for an organization.
        /// </summary>
        [HttpGet("organization/{orgId:long}")]
        public async Task<IActionResult> GetByOrganization(long orgId)
        {
            try
            {
                var result = await Shared.GetEFTPaymentSetupsByOrgIdAsync(orgId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving EFT payment setups.",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new EFT payment setup.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EFTPaymentSetup model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        Message = "Request body is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return BadRequest(new
                    {
                        Message = "Name is required."
                    });
                }

                if (model.OrgID <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Valid OrgID is required."
                    });
                }

                var id = await Shared.InsertEFTPaymentSetupAsync(model);

                model.Id = id;

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = id },
                    model
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while creating the EFT payment setup.",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing EFT payment setup.
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] EFTPaymentSetup model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        Message = "Request body is required."
                    });
                }

                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Invalid Id."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return BadRequest(new
                    {
                        Message = "Name is required."
                    });
                }

                if (model.OrgID <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Valid OrgID is required."
                    });
                }

                model.Id = id;

                var affectedRows =
                    await Shared.UpdateEFTPaymentSetupAsync(model);

                if (affectedRows == 0)
                {
                    return NotFound(new
                    {
                        Message = "EFT payment setup not found."
                    });
                }

                return Ok(new
                {
                    Message = "EFT payment setup updated successfully.",
                    Data = model
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while updating the EFT payment setup.",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete an EFT payment setup.
        /// </summary>
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Invalid Id."
                    });
                }

                var affectedRows =
                    await Shared.DeleteEFTPaymentSetupAsync(id);

                if (affectedRows == 0)
                {
                    return NotFound(new
                    {
                        Message = "EFT payment setup not found."
                    });
                }

                return Ok(new
                {
                    Message = "EFT payment setup deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while deleting the EFT payment setup.",
                    Error = ex.Message
                });
            }
        }

        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(
    [FromBody] EFTPaymentSetupReorderRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        Message = "Request body is required."
                    });
                }

                if (request.OrgID <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Valid OrgID is required."
                    });
                }

                if (request.Ids == null || request.Ids.Count == 0)
                {
                    return BadRequest(new
                    {
                        Message = "At least one EFT payment setup Id is required."
                    });
                }

                if (request.Ids.Any(x => x <= 0))
                {
                    return BadRequest(new
                    {
                        Message = "All Ids must be greater than zero."
                    });
                }

                if (request.Ids.Count != request.Ids.Distinct().Count())
                {
                    return BadRequest(new
                    {
                        Message = "Duplicate Ids are not allowed."
                    });
                }

                var result = await Shared.ReorderEFTPaymentSetupsAsync(
                    request.OrgID,
                    request.Ids);

                if (!result)
                {
                    return BadRequest(new
                    {
                        Message = "One or more EFT payment setups do not belong to the specified organization."
                    });
                }

                return Ok(new
                {
                    Message = "EFT payment setups reordered successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while reordering EFT payment setups.",
                    Error = ex.Message
                });
            }
        }

        public class EFTPaymentSetupReorderRequest
        {
            public long OrgID { get; set; }

            public List<long> Ids { get; set; } = new();
        }

    }
}
