using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {             
        // GET: api/<ProductController>
        [HttpGet]
        public IActionResult Get(int page_number, int page_size)
        {
            return Ok(Shared.GetPagedProducts("AND (Products.Status=1 OR Products.Status=3 OR Products.Status=4)", page_number, page_size));
        }

        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            Models.Product p_Detail = Shared.GetProduct(id);
            if (p_Detail == null)
            {
                return NotFound("Product doesn't exist.");
            }

            var Brand = Shared.GetBrand(p_Detail.ManufID);
            long categoryId = Shared.GetCategoryId(p_Detail.ProdID);
            var Category = Shared.GetCategory(categoryId);
            var ProductSpecs = Shared.GetProductSpecifications(id);
            var features = Shared.GetProductFeatures(id);
            var reviews = Shared.GetProductReviews(id);
            var images = Shared.GetProductImages(id);
            List<Product_View> youMayLike = Shared.GetYouMayLike(p_Detail.GroupName);
            List<CategoryFAQ> faqs = Shared.GetFAQByCategory(categoryId);
            return Ok(new { Product = p_Detail, Specs = ProductSpecs, Features = features, Reviews = reviews, Images = images, youMayLike, CategoryId = categoryId, Category, Brand, FAQs = faqs });
        }

        [HttpGet]
        [Route("ProductDetail/{meta_title}/{id}")]
        public IActionResult GetProductDetail(string meta_title, long id)
        {
            Models.Product p_Detail = Shared.GetProduct(id);
            if (p_Detail == null)
            {
                return NotFound("Product doesn't exist.");
            }

            var Brand = Shared.GetBrand(p_Detail.ManufID);
            long categoryId = Shared.GetCategoryId(id);
            var Category = Shared.GetCategory(categoryId);
            var ProductSpecs = Shared.GetProductSpecifications(id);
            var features = Shared.GetProductFeatures(id);
            var reviews = Shared.GetProductReviews(id);
            var images = Shared.GetProductImages(id);
            List<Product_View> youMayLike = Shared.GetYouMayLike(p_Detail.GroupName);
            return Ok(new { Product = p_Detail, Specs = ProductSpecs, Features = features, Reviews = reviews, Images = images, youMayLike, CategoryId = categoryId, Category, Brand });
        }

        [HttpGet]
        [Route("Search")]
        public IActionResult SearchProducts(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return StatusCode(204, new { error = "Please enter something to search." });
            }
            if (search.ToString().ToUpper().Contains("INSERT") || search.ToString().ToUpper().Contains("DELETE") || search.ToString().ToUpper().Contains("UPDATE") || search.ToString().ToUpper().Contains("DROP") || search.ToString().ToUpper().Contains("ALTER"))
            {
                return StatusCode(500, new { error = "Bogus Query" });
            }
            SearchProductResult products = Shared.SearchProduct(search);
            if (products != null && products.Products != null && !products.Products.Any())
            {
                string ipAddress = Convert.ToString(Request.HttpContext.Connection.RemoteIpAddress);
                long? custId = null;
                if (User.Identity.IsAuthenticated)
                {
                    custId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                }
                MissedSearch mSearch = new()
                {
                    CustID = custId,
                    SearchString = search,
                    IP = ipAddress
                };
                Shared.SaveMissedSearch(mSearch);
            }
            return Ok(products);
        }

        [HttpGet]
        [Route("PagedSearch")]
        public IActionResult PagedSearch(string search, int? page_number, int? page_size)
        {

            if (string.IsNullOrWhiteSpace(search))
            {
                return StatusCode(204, new { error = "Please enter something to search." });
            }
            if (search.ToString().ToUpper().Contains("INSERT") || search.ToString().ToUpper().Contains("DELETE") || search.ToString().ToUpper().Contains("UPDATE") || search.ToString().ToUpper().Contains("DROP") || search.ToString().ToUpper().Contains("ALTER"))
            {
                return StatusCode(500, new { error = "Bogus Query" });
            }
            int pageNum = (page_number ?? 1);
            int pageSize = (page_size ?? 12);
            PagedProduct products = Shared.SearchPagedProduct(search, pageNum, pageSize);
            return Ok(products);
        }

        [HttpGet]
        [Route("Latest")]
        public IActionResult GetLatestProducts()
        {

            List<Product_View> Products = Shared.GetLastThirtyDayProducts();
            if (Products.Count > 0)
            {
                Products = Products.OrderBy(_ => Guid.NewGuid()).ToList();
            }
            if (Products.Count() == 0)
            {
                Products = Shared.GetLatestProducts();
            }
            List<SubCategory> SubCategories = new();
            List<Brand> Brands = new();
            if (Products != null && Products.Any())
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpGet]
        [Route("MostViewed")]
        public IActionResult GeMostViewedProducts()
        {

            List<Product_View> Products = Shared.GetMostViewedProducts();
            List<SubCategory> SubCategories = [];
            List<Brand> Brands = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpGet]
        [Route("Trending")]
        public IActionResult GetTrendingProducts()
        {
            List<Product_View> Products = Shared.GetTrendingProducts();
            List<SubCategory> SubCategories = [];
            List<Brand> Brands = [];
            if (Products != null && Products.Count>0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
                string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpGet]
        [Route("BestSelling")]
        public IActionResult GetBestSellingProducts()
        {
            List<Product_View> Products = Shared.GetBestSellerProducts();

            List<SubCategory> SubCategories = [];
            List<Brand> Brands = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
                string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpGet]
        [Route("Featured")]
        public IActionResult GetFeaturedProducts()
        {
            List<Product_View> Products = Shared.GetFeaturedProducts();

            List<SubCategory> SubCategories = [];
            List<Brand> Brands = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
                string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpGet]
        [Route("SpecialFeatured")]
        public IActionResult GetFeaturedSpecialProducts()
        {
            List<SpecialPageProduct> Products = Shared.GetSpecialFeaturedProducts();

            List<SubCategory> SubCategories = [];
            List<Brand> Brands = [];
            if (Products != null && Products.Count > 0)
            {
                string subcategories = string.Join(',', Products.Where(x => x.SubCategory != null).Select(y => new { x = "N'" + y.SubCategory.Replace("\'", "\'\'") + "'" }).Select(x => x.x).Distinct());
                string brandIds = string.Join(',', Products.Where(x => x.Brand != null).Select(x => new { y = "N'" + x.Brand.Replace("\'", "\'\'") + "'" }).Select(x => x.y).Distinct());
                string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
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
            return Ok(new { Products, SubCategories, Brands });
        }

        [HttpPost]
        [Route("AddReview")]
        [Authorize(Roles = "Reseller,Customer")]
        public IActionResult AddReview(ProductReview review)
        {
            try
            {
                long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                review.CustID = CustomerID;
                review.ProdRevDate = DateTime.UtcNow.AddHours(2);
                review.ReviewStatusID = 1;
                review.OrgID = Shared.GetOrgID();
                Shared.AddReview(review);
                return Ok(new { message = "Review added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetProductByProductCode/{id}")]
        public IActionResult GetProductByProductCode(string id)
        {
            Product_View product = Shared.GetResellerProductByProductCode(id);
            return Ok(product);

        }

        [HttpGet]
        [Route("GetDealsOfTheDay")]
        public IActionResult GetDealsOfTheDay()
        {
            var products= Shared.GetDealsOfTheDayHomepage();
            return Ok(products);

        }

        [HttpGet]
        [Route("GetAllDealsOfTheDay")]
        public IActionResult GetAllDealsOfTheDay(int? page_number,int? page_size)
        {
            int pNum = (page_number ?? 1);
            int pSize = (page_size ?? 16);
            var products = Shared.GetDealsOfTheDay(pNum,pSize);
            return Ok(products);

        }

        [HttpGet]
        [Route("TopSales")]
        public IActionResult GetTopSalesProducts()
        {
            List<TopSale> topSales = new();
            string query = "WITH CTE as (Select Products.ProdId,Products.ImgURL,Products.ProductName,Sum(WebOrderItems.ProdQty) as Sales from WEBOrderItems JOin Products on WEBOrderItems.ProdID=Products.ProdID Where Products.OrgID IN (94,380,932,546) AND Products.Active=1 AND Products.OutputMe=1 AND dbo.[GetProductStockCount](Products.ProdId,Products.Status,N'A')>0 Group By Products.ProdID,Products.ImgURL,Products.ProductName) Select Top 10 * from CTE Order By Sales desc;";
            using (var db = new SqlConnection(Shared.connString))
            {
                topSales = db.Query<TopSale>(query).ToList();
            }
            return Ok(topSales);

        }

    }   
}
