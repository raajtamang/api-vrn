using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NormalSpecialPageController : ControllerBase
    {
        // GET: api/<NormaSpecialPageController>
        [HttpGet]
        public IEnumerable<NormalSpecialPage> Get()
        {
            return Shared.GetNormalSpecialPage();
        }

        [HttpGet]
        [Route("Customer")]
        public IEnumerable<NormalSpecialPage> CustomerPage()
        {
            return Shared.GetCustomerNormalSpecialPage();
        }

        [HttpGet]
        [Route("Reseller")]
        public IEnumerable<NormalSpecialPage> ResellerPage()
        {
            return Shared.GetResellerNormalSpecialPage();
        }

        // GET api/<NormaSpecialPageController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            NormalSpecialPage page = Shared.GetNormalSpecialPageDetail(id);
            List<SpecialPageProduct> products = Shared.GetSpecialPageProducts(id, "Normal Special");
            return Ok(new { PageDetail = page, PageProducts = products });
        }

        [HttpGet]
        [Route("ByMetaTitle")]
        public IActionResult ByMetaTitle(string meta_title)
        {
            NormalSpecialPage page = Shared.GetNormalSpecialPageDetailByMetaTitle(meta_title);
            if (page == null)
            {
                return StatusCode(404, new { error = "Normal Special Page doesn't exist anymore." });
            }
            List<SpecialPageProduct> products = Shared.GetSpecialPageProducts(page.Id, "Normal Special");
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
        public IActionResult NormalSpecialPageProducts(long id)
        {
            List<SpecialPageProduct> pageProducts = Shared.GetSpecialPageProducts(id, "Normal Special");
            return Ok(pageProducts);
        }
        [HttpGet]
        [Route("ProductDetail/{id}")]
        public IActionResult NormalSpecialPageProductDetail(long id)
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
        [Route("GetResellerNormalSpecialPageProducts")]
        public IActionResult GetNormalSpecialPageProducts()
        {
            NormalSpecialPage page = Shared.GetFirstNormalSpecialPage("Reseller");
            if (page == null)
            {
                return NotFound(new { error = "Reseller normal special page doesn't exist" });

            }
            List<SpecialPageProductDetail> products = Shared.GetNormalProducts(page.Id, "Reseller Page");
            List<SubCategory> SubCategories = new();
            List<Brand> Brands = new();
            if (products.Any())
            {
                string subcategories = string.Join(',', products.Where(x => x.SubCategory != null).Select(y => new { x = "N'" + y.SubCategory.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', products.Where(x => x.Brand != null).Select(x => new { y = "N'" + x.Brand.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
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
        [Route("GetCustomerNormalSpecialPageProducts")]
        public IActionResult GetCNormalSpecialPageProducts()
        {
            NormalSpecialPage page = Shared.GetFirstNormalSpecialPage("Customer");
            if (page == null)
            {
                return NotFound(new { error = "Customer normal special page doesn't exist" });

            }
            List<SpecialPageProductDetail> products = Shared.GetNormalProducts(page.Id, "Customer Page");
            List<SubCategory> SubCategories = new();
            List<Brand> Brands = new();
            if (products.Any())
            {
                string subcategories = string.Join(',', products.Where(x => x.SubCategory != null).Select(y => new { x = "N'" + y.SubCategory.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', products.Where(x => x.Brand != null).Select(x => new { y = "N'" + x.Brand.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
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
    }
}
