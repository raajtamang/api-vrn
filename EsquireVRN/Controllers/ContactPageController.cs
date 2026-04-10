using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactPageController : ControllerBase
    {

        [HttpGet]
        public IActionResult GetById()
        {
            long orgId = Shared.GetOrgID();
            var data = Shared.GetContactPageById(orgId);

            string? Content = null;
            if (data == null)
                return Ok(new { Content });

            return Ok(data);
        }

        [HttpPut]
        [Authorize(Roles = "Reseller")]
        public IActionResult Update([FromForm] ContactPageDTO model)
        {
            try
            {
                long orgId = Shared.GetOrgID();
                var data = Shared.GetContactPageById(orgId);

                var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/";
                string imgUrl = "";
                if (!string.IsNullOrEmpty(model.WebsiteLogoURL))
                {
                    imgUrl = model.WebsiteLogoURL;
                }

                if (model.Logo != null && model.Logo.Length > 0)
                {
                    var fileSize = model.Logo.Length;
                    if ((fileSize / 1048576.0) > 5)
                    {
                        return StatusCode(400, "Image exceeds 5mb size limit.");
                    }
                    else
                    {
                        var folderName = Path.Combine("Resources", "Images");
                        var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                        if (!Directory.Exists(pathToSave))
                        {
                            Directory.CreateDirectory(pathToSave);
                        }
                        string imgname = model.Logo.FileName;
                        var can_continue = false;
                        var extension = Path.GetExtension(imgname);
                        int i = 1;
                        if (data != null && data.WebsiteLogoURL != null)
                        {
                            var imageName = data.WebsiteLogoURL.Split("/").LastOrDefault();
                            if (!string.IsNullOrEmpty(imageName))
                            {
                                string oldFilePath = Path.Combine(pathToSave, imageName);
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                            }
                        }
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
                            model.Logo.CopyTo(fs);
                        }
                        imgUrl = requestUrl + "Resources/Images/" + imgname.Replace(" ", "-");
                    }

                }
                model.WebsiteName = Shared.GetOrgName();
                if (data == null)
                {
                    ContactPage cpage = new()
                    {
                        Address = model.Address,
                        WebsiteLogoURL = imgUrl,
                        WebsiteName = model.WebsiteName,
                        Facebook = model.Facebook,
                        Instagram = model.Instagram,
                        Youtube = model.Youtube,
                        LinkedIn = model.LinkedIn,
                        Twitter = model.Twitter,
                        Phone = model.Phone,
                        Email = model.Email,
                        Map_IFrame = model.Map_IFrame,
                        OrgId = orgId,
                        Created_Date = DateTime.Now,
                        Updated_Date = null,
                        WebsiteDescription = model.WebsiteDescription
                    };
                    var result = Shared.CreateContactPage(cpage);
                    var rPage = Shared.GetContactPageById(orgId);
                    return Ok(rPage);
                }
                else
                {
                    ContactPage cpage = new()
                    {
                        Address = model.Address,
                        WebsiteLogoURL = imgUrl,
                        WebsiteName = model.WebsiteName,
                        Facebook = model.Facebook,
                        Instagram = model.Instagram,
                        Youtube = model.Youtube,
                        LinkedIn = model.LinkedIn,
                        Twitter = model.Twitter,
                        Phone = model.Phone,
                        Email = model.Email,
                        Map_IFrame = model.Map_IFrame,
                        OrgId = orgId,
                        Created_Date = DateTime.Now,
                        Updated_Date = null,
                        WebsiteDescription = model.WebsiteDescription
                    };
                    var result = Shared.UpdateContactPage(cpage);
                    var cPage = Shared.GetContactPageById(orgId);
                    return Ok(cPage);
                }
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong. Please try again." });
            }
        }
    }
}
