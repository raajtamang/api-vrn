using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionSpecialPageController : ControllerBase
    {
        // GET: api/<PromotionSpecialPageController>
        [HttpGet]
        public IEnumerable<PromotionSpecialPage> Get()
        {
            return Shared.GetPromotionalSpecialPage();
        }

        // GET api/<PromotionSpecialPageController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            PromotionSpecialPage page = Shared.GetPromotionalSpecialPageDetail(id);
            List<SpecialPageProduct> products = Shared.GetSpecialPageProducts(id, "Promotion Special");
            return Ok(new { PageDetail = page, PageProducts = products });
        }

        [HttpGet]
        [Route("Customer")]
        public IActionResult ClientPage()
        {
            return Ok(Shared.GetCustomerPromotionalSpecialPage());
        }

        [HttpGet]
        [Route("Reseller")]
        public IActionResult ResellerPage()
        {
            return Ok(Shared.GetResellerPromotionalSpecialPage());
        }      

        [HttpGet]
        [Route("ByMetaTitle")]
        public IActionResult ByMetaTitle(string meta_title)
        {
            PromotionSpecialPage page = Shared.GetPromotionalSpecialPageDetailByMetaTitle(meta_title);
            if (page == null)
            {
                return StatusCode(404, new { error = "Promotional Special Page doesn't exist anymore." });
            }
            List<SpecialPageProduct> products = Shared.GetSpecialPageProducts(page.Id, "Promotion Special");
            List<SubCategory> SubCategories = new();
            List<Brand> Brands = new();
            if (products.Any())
            {
                string subcategories = string.Join(',', products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
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
            return Ok(new { PageDetail = page, PageProducts = products, Brands, SubCategories });
        }

        [HttpGet]
        [Route("GetProducts")]
        public IActionResult PromotionalSpecialPageProducts(long id)
        {
            List<SpecialPageProduct> pageProducts = Shared.GetSpecialPageProducts(id, "Promotion Special");
            return Ok(pageProducts);
        }

        [HttpGet]
        [Route("ProductDetail/{id}")]
        public IActionResult PromotionPageProductDetail(long id)
        {
            SpecialPageProduct oldProduct = Shared.GetSpecialPageProduct(id);
            if (oldProduct == null)
            {
                return StatusCode(404, new { error = "Normal Special Page product doesn't exist anymore." });
            }

            SpecialPageProductDetail pageProducts = Shared.GetSpecialPageProductDetail(id);
            if ((pageProducts.StockQty < 1))
            {
                return NotFound(new { error = "Special Page Product doesn't exist." });
            }
            if (pageProducts == null)
            {
                return NotFound(new { error = "Special Page Product doesn't exist." });
            }
            
            var Brand = Shared.GetBrand(Convert.ToInt64(pageProducts.ManufID));
            long categoryId = Shared.GetCategoryId(oldProduct.ProdID.Value);
            var Category = Shared.GetCategory(categoryId);
            var ProductSpecs = Shared.GetProductSpecifications(pageProducts.ProdID);
            var features = Shared.GetProductFeatures(pageProducts.ProdID);
            var reviews = Shared.GetProductReviews(pageProducts.ProdID);
            var images = Shared.GetProductImages(pageProducts.ProdID);
            List<CategoryFAQ> faqs = Shared.GetFAQByCategory(categoryId);
            return Ok(new { Product = pageProducts, Specs = ProductSpecs, Features = features, Reviews = reviews, Images = images, CategoryId = categoryId, Category, Brand, FAQs = faqs });
        }

       

        [HttpGet]
        [Route("PriceByProductCode/{id}")]
        public IActionResult GetPriceByProductCode(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (id.ToString().ToUpper().Contains("INSERT") || id.ToString().ToUpper().Contains("DELETE") || id.ToString().ToUpper().Contains("UPDATE") || id.ToString().ToUpper().Contains("DROP") || id.ToString().ToUpper().Contains("ALTER"))
                {
                    return StatusCode(500, new { error = "Bogus Query" });
                }
                double price = Shared.GetPriceByProductCode(id);
                return Ok(new { Price = price });
            }
            else
            {
                return StatusCode(400, "Invalid product code.");
            }

        }
    }
}
