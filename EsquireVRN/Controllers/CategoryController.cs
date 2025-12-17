using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        // GET: api/<CategoryController>
        [HttpGet]
        public IEnumerable<Category> Get()
        {
            return Shared.GetCategories();
        }

        [HttpGet]
        [Route("Popular")]
        public IActionResult GetPopularCategory(int page_number, int page_size)
        {
            return Ok(Shared.GetPopularCategories(page_number, page_size));
        }

        // GET api/Category/5
        [HttpGet("{id}")]
        public IActionResult Get(long? id)
        {
            Category category = Shared.GetCategory(id);
            if (category == null)
            {
                return NotFound(new { error = "Category doesn't exist." });
            }
            return Ok(category);
        }        

        [HttpGet]
        [Route("Products")]
        public IActionResult GetProducts(long id)
        {
            Category Category=Shared.GetCategory(id);
            if (Category == null)
            {
                return StatusCode(404, new { error = "Category doesn't exist anymore." });
            }
            List<Product_View> Products = Shared.GetCategoryProducts(id);
            List<Brand> Brands = [];
            List<SubCategory> SubCategories = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "'" + x.ManufacturerName.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
                StringBuilder sb = new("");
                if (!string.IsNullOrEmpty(brandIds))
                {
                    sb.Append("Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");");
                }
                if (!string.IsNullOrEmpty(subcategories)) {
                    sb.Append("SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");");
                }
                string strBrandQuery = sb.ToString();
                using (var db = new SqlConnection(Shared.connString))
                {
                    var result = db.QueryMultiple(strBrandQuery, commandTimeout: 60);
                    if (result != null)
                    {
                        Brands = [.. result.Read<Brand>().DistinctBy(x => x.Name)];
                        SubCategories = [.. result.Read<SubCategory>().DistinctBy(x => x.Title)];
                    }
                }
            }
            return Ok(new { Category, Products, Brands,SubCategories });

        }

    }
}
