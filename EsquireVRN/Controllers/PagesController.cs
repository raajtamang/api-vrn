using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagesController : ControllerBase
    {
        // GET: api/Brand
        [HttpGet]
        public IEnumerable<Pages> Get()
        {
            return Shared.GetPages();
        }

        // GET api/Brand/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            Pages page=Shared.GetPage(id);
            if(page== null)
            {
                return NotFound(new {error="Page doesn't exist."});
            }

            return Ok(page);
        }

        [HttpGet]
        [Route("ByMetaTitle")]
        public IActionResult GetByMetaTitle(string meta_title)
        {
            Pages page = Shared.GetPageByMetaTitle(meta_title);
            if (page == null)
            {
                return NotFound(new { error = "Page doesn't exist." });
            }
            var Products = Shared.GetPageProducts(page.Id);
            List<SubCategory> SubCategories = new();
            List<Brand> Brands = new();
            if (Products != null && Products.Any())
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
                string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
                using (var db = new SqlConnection(Shared.connString))
                {
                    var result = db.QueryMultiple(strBrandQuery, commandTimeout: 60);
                    if (result != null)
                    {
                        Brands = result.Read<Brand>().DistinctBy(x => x.Name).ToList();
                        SubCategories = result.Read<SubCategory>().DistinctBy(x => x.Title).ToList();
                    }
                }
            }
            return Ok(new { pageDetail = page, Products,SubCategories,Brands });
        }

     
        [HttpGet]
        [Route("GetProducts")]
        public IActionResult GetPageProducts(int PageId)
        {
            List<Product_View> products = Shared.GetPageProducts(PageId);
            return Ok(products);
        }
       
    }
}
