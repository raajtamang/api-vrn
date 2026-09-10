using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Roles = "Reseller")]
        public async Task<IActionResult> Create([FromForm] EFTPaymentSetup model)
        {
            try
            {

                if (model == null)
                {
                    return BadRequest(new
                    {
                        Message = "Request data is required."
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
                var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/";
                // Handle uploaded logo
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    var fileSize = model.LogoFile.Length;
                    if ((fileSize / 1048576.0) > 5)
                    {
                        return StatusCode(400, new { error = "Image exceeds 5mb size limit." });
                    }

                    // Generate a unique file name
                    var folderName = Path.Combine("Resources", "Images", "EFT");
                    var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                    if (!Directory.Exists(pathToSave))
                    {
                        Directory.CreateDirectory(pathToSave);
                    }
                    string imgname = model.LogoFile.FileName;
                    var can_continue = false;
                    var extension = Path.GetExtension(imgname);
                    int i = 1;
                    while (!can_continue)
                    {
                        bool imgExists = System.IO.File.Exists(Path.Combine(pathToSave, imgname));
                        if (!imgExists)
                        {
                            can_continue = true;
                        }
                        if (imgExists)
                        {
                            if (imgname.Contains("-" + (i - 1) + extension))
                            {
                                imgname = imgname.Replace("-" + (i - 1) + extension, "") + "-" + i + extension;
                            }
                            else
                            {
                                imgname = imgname.Replace(extension, "") + "-" + i + extension;
                            }
                            i++;

                        }
                    }
                    string filePath = Path.Combine(pathToSave, imgname.Replace(" ", "-"));
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        model.LogoFile.CopyTo(fs);
                    }
                    model.LogoUrl = requestUrl + "Resources/Images/EFT/" + imgname.Replace(" ", "-");

                }

                var id = await Shared.InsertEFTPaymentSetupAsync(model);

                model.Id = id;
                var EFTPaymentSetup = await Shared.GetEFTPaymentSetupByIdAsync(id);
                return Ok(new { message = "EFT Payment Setup added successfully.", EFTPaymentSetup });
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
        [HttpPut("{id}")]
        [Authorize(Roles = "Reseller")]
        public async Task<IActionResult> Update(long id, [FromForm] EFTPaymentSetup model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new
                    {
                        Message = "Request data is required."
                    });
                }

                if (id <= 0)
                {
                    return BadRequest(new
                    {
                        Message = "Valid Id is required."
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

                // Get existing EFT Payment Setup
                var existingEFTPaymentSetup =
                    await Shared.GetEFTPaymentSetupByIdAsync(id);

                if (existingEFTPaymentSetup == null)
                {
                    return NotFound(new
                    {
                        Message = "EFT Payment Setup not found."
                    });
                }

                var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/";

                // Handle uploaded logo
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    // Validate file size
                    var fileSize = model.LogoFile.Length;

                    if ((fileSize / 1048576.0) > 5)
                    {
                        return BadRequest(new
                        {
                            error = "Image exceeds 5mb size limit."
                        });
                    }

                    // Folder
                    var folderName = Path.Combine(
                        "Resources",
                        "Images",
                        "EFT"
                    );

                    var pathToSave = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        folderName
                    );

                    if (!Directory.Exists(pathToSave))
                    {
                        Directory.CreateDirectory(pathToSave);
                    }

                    // Generate file name
                    string imgname = model.LogoFile.FileName;

                    // Remove unsafe path information
                    imgname = Path.GetFileName(imgname);

                    var extension = Path.GetExtension(imgname);

                    bool can_continue = false;
                    int i = 1;

                    while (!can_continue)
                    {
                        bool imgExists = System.IO.File.Exists(
                            Path.Combine(
                                pathToSave,
                                imgname.Replace(" ", "-")
                            )
                        );

                        if (!imgExists)
                        {
                            can_continue = true;
                        }
                        else
                        {
                            if (imgname.Contains("-" + (i - 1) + extension))
                            {
                                imgname = imgname.Replace(
                                    "-" + (i - 1) + extension,
                                    ""
                                ) + "-" + i + extension;
                            }
                            else
                            {
                                imgname = imgname.Replace(
                                    extension,
                                    ""
                                ) + "-" + i + extension;
                            }

                            i++;
                        }
                    }

                    imgname = imgname.Replace(" ", "-");

                    string filePath = Path.Combine(
                        pathToSave,
                        imgname
                    );

                    // Save new file
                    await using (FileStream fs = new FileStream(
                        filePath,
                        FileMode.Create
                    ))
                    {
                        await model.LogoFile.CopyToAsync(fs);
                    }

                    // Delete old logo file if it exists
                    if (!string.IsNullOrWhiteSpace(existingEFTPaymentSetup.LogoUrl))
                    {
                        try
                        {
                            var oldFileName = Path.GetFileName(
                                new Uri(existingEFTPaymentSetup.LogoUrl).AbsolutePath
                            );

                            var oldFilePath = Path.Combine(
                                pathToSave,
                                oldFileName
                            );

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        catch
                        {
                            // Ignore old file deletion errors
                            // so the database update can continue.
                        }
                    }

                    // Set new logo URL
                    model.LogoUrl =
                        requestUrl +
                        "Resources/Images/EFT/" +
                        imgname;
                }
                else
                {
                    // No new file uploaded,
                    // keep the existing logo
                    model.LogoUrl = existingEFTPaymentSetup.LogoUrl;
                }

                // Make sure the correct ID is used
                model.Id = id;

                // Update database
                await Shared.UpdateEFTPaymentSetupAsync(model);

                // Get updated record
                var EFTPaymentSetup =
                    await Shared.GetEFTPaymentSetupByIdAsync(id);

                return Ok(new
                {
                    message = "EFT Payment Setup updated successfully.",
                    EFTPaymentSetup
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
        [Authorize(Roles = "Reseller")]

        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { Message = "Invalid Id." });
                }

                var existingEFTPaymentSetup = await Shared.GetEFTPaymentSetupByIdAsync(id);
                if (existingEFTPaymentSetup == null)
                {
                    return NotFound(new { Message = "EFT Payment Setup not found." });
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

                if (!string.IsNullOrWhiteSpace(existingEFTPaymentSetup.LogoUrl))
                {
                    var folderName = Path.Combine("Resources", "Images", "EFT");

                    var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                    try
                    {
                        var oldFileName = Path.GetFileName(new Uri(existingEFTPaymentSetup.LogoUrl).AbsolutePath);

                        var oldFilePath = Path.Combine(pathToSave, oldFileName);

                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    catch
                    {
                        // Ignore old file deletion errors
                        // so the database update can continue.
                    }
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
        [Authorize(Roles = "Reseller")]
        public async Task<IActionResult> Reorder([FromBody] EFTPaymentSetupReorderRequest request)
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
