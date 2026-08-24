using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        // GET: api/Brand
        [HttpGet]
        public IActionResult Get(long? page_number, long? page_size, string? search)
        {
            return Ok(Shared.GetBrands(page_number, page_size, search));
        }

        [HttpGet]
        [Route("Popular")]
        public IActionResult GetPopularBrands(int? page_number, int? page_size)
        {
            return Ok(Shared.GetPopularBrands(page_number, page_size));
        }

        // GET api/Brand/5
        [HttpGet("{id}")]
        public IActionResult Get(long? id)
        {
            var brand = Shared.GetBrand(id);
            if (brand == null)
            {
                return NotFound(new { error = "Brand doesn't exist." });
            }
            return Ok(brand);
        }

        //[HttpGet("GetAllBrands")]
        ////[Authorize(Roles = "Reseller")]
        //public IActionResult GetAllBrands(long? page_number, long? page_size, string? search)
        //{
        //    return Ok(Shared.GetAllBrands(page_number, page_size, search));

        //}

        //[HttpPost("UpdateBrandList")]
        ////[Authorize(Roles = "Reseller")]
        //public IActionResult UpdateBrands([FromBody] UpdateBrandModel reqModel)
        //{
        //    try
        //    {
        //        var orgId = Shared.GetOrgID();
        //        if (reqModel?.AddIdList?.Count > 0)
        //        {
        //            long position = Shared.GetVRNBrandLastPosition(orgId);
        //            List<VRNBrands> subCategories = [];
        //            foreach (var item in reqModel.AddIdList)
        //            {
        //                if (!Shared.VRNBrandsExists(item))
        //                {
        //                    VRNBrands sCategory = new()
        //                    {
        //                        BrandId = item,
        //                        OrgId = orgId,
        //                        Position = position,
        //                        CreatedDate = DateTime.Now
        //                    };
        //                    subCategories.Add(sCategory);
        //                    position++;
        //                }
        //            }
        //            if (subCategories.Count > 0)
        //            {
        //                Shared.AddVRNBrands(subCategories);
        //            }
        //        }
        //        if (reqModel?.RemoveIdList?.Count > 0)
        //        {
        //            Shared.RemoveVRNBrands(reqModel.RemoveIdList, Shared.GetOrgID());
        //        }
        //        return Ok(new { message = "Brand list updated successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = "Something went wrong. Please try again." });
        //    }
        //}

        [HttpGet]
        [Route("Products")]
        public IActionResult GetProducts(long id)
        {
            Brand Brand = Shared.GetBrand(id);
            if (Brand == null)
            {
                return StatusCode(404, new { error = "Brand doesn't exist anymore." });
            }
            List<Product_View> Products = Shared.GetBrandProducts(id);
            List<SubCategory> SubCategories = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                if (!string.IsNullOrWhiteSpace(subcategories))
                {
                    string strBrandQuery = "SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
                    if (!string.IsNullOrWhiteSpace(strBrandQuery))
                    {
                        using var db = new SqlConnection(Shared.connString);
                        SubCategories = db.Query<SubCategory>(strBrandQuery).DistinctBy(x => x.Title).ToList();
                    }
                }
            }
            return Ok(new { Brand, Products, SubCategories });

        }

        // DELETE api/<BrandController>/5
        //[HttpDelete("{id}")]
        //[Authorize(Roles = "Reseller")]
        //public IActionResult Delete(long? id)
        //{
        //    try
        //    {
        //        Brand oBrand = Shared.GetBrand(id);
        //        if (oBrand == null)
        //        {
        //            return StatusCode(404, new { error = "Brand doesn't exist anymore." });
        //        }
        //        //if (!Shared.CanDeleteBrand(id))
        //        //{
        //        //    return StatusCode(400, new { error = "Brand has items assigned to it. Please remove all the products assigned to it and try again." });
        //        //}
        //        if (Shared.DeleteBrand(id))
        //        {
        //            var folderName = Path.Combine("Resources", "Images", "Brands");
        //            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        //            if (!Directory.Exists(pathToSave))
        //            {
        //                Directory.CreateDirectory(pathToSave);
        //            }
        //            if (!string.IsNullOrWhiteSpace(oBrand.Link))
        //            {
        //                string oldImage = oBrand.Link.Split('/').LastOrDefault();
        //                if (!string.IsNullOrWhiteSpace(oldImage))
        //                {
        //                    string oldImagePt = Path.Combine(pathToSave, oldImage);
        //                    if (System.IO.File.Exists(oldImagePt))
        //                    {
        //                        System.IO.File.Delete(oldImagePt);
        //                    }
        //                }
        //            }
        //            return Ok(new { message = "Brand removed successfully" });
        //        }
        //        else
        //        {
        //            return StatusCode(500, new { error = "Something went wrong with the server. Please try again." });
        //        }
        //    }
        //    catch
        //    {
        //        return StatusCode(500, new { error = "Something went wrong with the server. Please try again." });
        //    }

        //}
    }
}
