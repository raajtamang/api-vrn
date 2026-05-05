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
    public class SubCategoryController : ControllerBase
    {
        // GET: api/<SubCategoryController>
        [HttpGet]
        public IEnumerable<SubCategory> Get()
        {
            return Shared.GetSubCategories();
        }

        [HttpGet("GetAllSubCategories")]
        [Authorize(Roles = "Reseller")]
        public IActionResult GetAllSubCategories()
        {
            return Ok(Shared.GetAllSubCategories());
        }

        [HttpGet]
        [Route("ByCategory")]
        public IEnumerable<SubCategory> ByCategory(long id)
        {
            return Shared.GetSubGetCategoriesByCategory(id);
        }

        // GET api/SubCategory/5
        [HttpGet("{id}")]
        public IActionResult Get(long? id)
        {
            SubCategory subCategory = Shared.GetSubCategory(id);
            if (subCategory == null)
            {
                return NotFound(new { error = "Sub Category doesn't exist." });
            }
            return Ok(subCategory);
        }

        [HttpGet]
        [Route("Products")]
        public IActionResult GetProducts(long id)
        {
            SubCategory oSubCategory = Shared.GetSubCategory(id);
            if (oSubCategory == null)
            {
                return StatusCode(404, new { error = "SubCategory doesn't exist anymore." });
            }

            List<Product_View> Products = Shared.GetSubCategoryProducts(oSubCategory.Title);
            List<Brand> Brands = [];
            if (Products != null && Products.Count > 0)
            {
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "'" + x.ManufacturerName.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
                if (!string.IsNullOrWhiteSpace(brandIds))
                {
                    string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");";
                    using (var db = new SqlConnection(Shared.connString))
                    {
                        Brands = [.. db.Query<Brand>(strBrandQuery)];
                    }
                }
            }
            return Ok(new { SubCategory = oSubCategory, Products, Brands });

        }

        [HttpPost("UpdateSubCategoryList")]
        [Authorize(Roles = "Reseller")]
        public IActionResult UpdateCategories([FromBody] UpdateCategoryModel reqModel)
        {
            try
            {
                var orgId = Shared.GetOrgID();
                if (reqModel?.AddIdList?.Count > 0)
                {
                    long position = Shared.GetVRNLastPosition(orgId);
                    List<VRNSubCategories> subCategories = [];
                    foreach (var item in reqModel.AddIdList)
                    {
                        long subCategoryId = Shared.GetSubCategoryId(item.SubCategory);
                        VRNSubCategories sCategory = new()
                        {
                            SubCategoryId = subCategoryId,
                            CategoryID = item.CategoryID,
                            OrgId = orgId,
                            Position = position
                        };
                        subCategories.Add(sCategory);
                        position++;
                    }
                    Shared.AddVRNCategories(subCategories);
                }
                if (reqModel?.RemoveIdList?.Count > 0)
                {
                    Shared.RemoveVRNCategories(reqModel.RemoveIdList, Shared.GetOrgID());
                }
                return Ok(new { message = "Category list updated successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }
    }
}
