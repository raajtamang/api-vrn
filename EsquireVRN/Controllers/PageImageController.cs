using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PageImageController : ControllerBase
    {
        // GET: api/<PageImageController>
        [HttpGet]
        public IActionResult Get(int? pageSize, int? pageNum)
        {
            pageSize ??= 12;
            pageNum ??= 1;
            return Ok(Shared.GetProductImages("", pageSize, pageNum));
        }

        // GET api/<PageImageController>/5
        [HttpGet]
        [Route("Search")]
        public IActionResult SearchImage(string searchText)
        {
            return Ok(Shared.SearchProductImages(searchText));
        }

        // POST api/<PageImageController>
        [HttpPost]
        //[Authorize(Roles = "Reseller")]
        public IActionResult Post([FromForm] List<IFormFile> Images)
        {
            var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/";
            int pageSize = 12;
            int pageNum = 1;
            string searchText = "";
            string errorMesage = "";
            List<PageImage> imageList = new();
            foreach (var item in Images)
            {
                if (item != null && item.Length > 0)
                {
                    var fileSize = item.Length;
                    if ((fileSize / 1048576.0) > 5)
                    {
                        errorMesage += item.FileName + " exceeds 5mb. \n";
                    }
                    else
                    {
                        var folderName = Path.Combine("Resources", "Images", "GalleryImage");
                        var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                        if (!Directory.Exists(pathToSave))
                        {
                            Directory.CreateDirectory(pathToSave);
                        }
                        string imgname = item.FileName, imageSlug = Shared.GenerateSlug(Path.GetFileNameWithoutExtension(item.FileName));

                        var can_continue = false;
                        var extension = Path.GetExtension(imgname);
                        int i = 1;
                        while (!can_continue)
                        {
                            bool imgExists = Shared.CheckImageExists(imgname);
                            if (!imgExists)
                            {
                                can_continue = true;
                            }
                            if (imgExists)
                            {
                                if (imageSlug.Contains("-" + (i - 1)))
                                {
                                    imageSlug = imageSlug.Replace("-" + (i - 1) + extension, "") + "-" + i;
                                }
                                else
                                {
                                    imageSlug = imageSlug.Replace(extension, "") + "-" + i;
                                }
                                i++;

                            }
                        }
                        string filePath = Path.Combine(pathToSave, imageSlug + extension);
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            item.CopyTo(fs);
                        }
                        PageImage pImage = new()
                        {
                            Title = imgname.Replace(extension, ""),
                            CreatedDate = DateTime.Now,
                            Image = imageSlug + extension,
                            Url = requestUrl + "Resources/Images/GalleryImage/" + imageSlug + extension,
                            OrgID = Shared.GetOrgID()
                        };
                        PageImage returnPageImge = Shared.SavePageImage(pImage);
                        returnPageImge.OrgID = Shared.GetOrgID();
                        imageList.Add(returnPageImge);
                    }
                }

            }
            return Ok(new { message = errorMesage, imageList });
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Reseller")]
        public IActionResult Delete(long id)
        {
            PageImage pImage = Shared.GetPageImage(id);
            if (pImage == null)
            {
                return NotFound(new { error = "Page Image doesn't exist anymore." });
            }
            try
            {
                Shared.DeletePageImage(id);
                if (!string.IsNullOrWhiteSpace(pImage.Image))
                {
                    var folderName = Path.Combine("Resources", "Images", "GalleryImage", pImage.Image);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                return Ok(new { message = "Page Image removed successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }
    }
}
