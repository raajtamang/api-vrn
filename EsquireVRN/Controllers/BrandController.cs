using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        // GET: api/Brand
        [HttpGet]
        public IEnumerable<Brand> Get()
        {
            return Shared.GetBrands();
        }
       
        [HttpGet]
        [Route("Popular")]
        public IActionResult GetPopularBrands(int?page_number,int? page_size)
        {
            return Ok(Shared.GetPopularBrands(page_number, page_size));
        }

        // GET api/Brand/5
        [HttpGet("{id}")]
        public IActionResult Get(long? id)
        {
            Brand brand = Shared.GetBrand(id);
            if (brand == null)
            {
                return NotFound(new { error = "Brand doesn't exist." });
            }
            return Ok(brand);
        }

       

        [HttpGet]
        [Route("Products")]
        public IActionResult GetProducts(long id)
        {
            Brand Brand=Shared.GetBrand(id);
            if (Brand == null)
            {
                return StatusCode(404, new { error = "Brand doesn't exist anymore." });
            }
            List<Product_View> Products = Shared.GetBrandProducts(id);
            List<SubCategory> SubCategories = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x=>x.GroupName!=null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
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
            return Ok(new { Brand,Products, SubCategories });

        }
    }
}
