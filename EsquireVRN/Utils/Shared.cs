using EsquireVRN.Models;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SelectPdf;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace EsquireVRN.Utils
{
    public static class Shared
    {
        public static IConfiguration StaticConfig { get; private set; }
        public static IWebHostEnvironment StaticEnv { get; private set; }
        public static string connString { get; private set; }
        public static void SetConfiguration(IConfiguration _configuration, IWebHostEnvironment env)
        {
            StaticConfig = _configuration;
            StaticEnv = env;
            connString = StaticConfig.GetConnectionString("DefaultConnection");
        }

        public const string IE_DESC_ID = "5";
        public const string CD_DESC_ID = "7";
        public const int PAY_ID_DEPOSIT = 1;
        public const int PAY_ID_ELECTRONIC_TRANSFER = 2;
        public const int COLLECT_AND_PAY_AT_SHOP = 12;
        public const int PAY_ID_IMPROPAY = 7;
        public const int PAY_ID_PAYGATE = 11;
        public const int PAY_ID_CREDIT_CARD_INSTANT_EFT_MOBI_CREDIT = 3;
        public const long AwaitingProofOfPaymentId = 2;
        public const byte DistOrderStatusPassed = 4;
        public const int OWN_COURIER_TO_COLLECT = 8;

        public const string CD_WAYBILL_PREFIX = "CDT001";

        public const string INVALID_LOGIN = "invalid";
        public const string ACCOUNT_INACTIVE = "inactive";
        public const string ACCOUNT_FRAUDULENT = "fraudulent";
        public const string LOGOUT = "logout";
        public const string FUNCTION_ERROR = "Error";

        public static string CurrencyFormat
        {
            get
            {
                return GetWebConfigKeyValue("CurrencyFormat");
            }
        }

        public struct Pricing
        {
            public byte NormalPriceNo;
            public byte DiscountPriceNo;
            public bool isDiscount;
            public byte UsePriceNumber;
        }

        public struct VatData
        {
            public string strVAT;
            public string strOneDotVAT;
            public double VAT;
            public double OneDotVAT;
        }

        public struct ProductActiveCheck
        {
            public string? ProductCode { get; set; }
            public string? ErrorMessage { get; set; }
            public bool Active { get; set; }
        }

        public enum FinType
        {
            ImproWeb = 0,
            Fincon = 1,
            FinconBranches = 2,
            Manual = 3
        }


        public struct DeliveryDetails
        {
            public string DeliveryDesc;
            public double Cost;
            public int DeliveryDescID;
            public string DeliveryID;
            public string Area;
        }

        public struct BranchDetail
        {
            public string BranchName;
            public string BranchEMail;
            public string OrgBraShort;
        }

        public struct FinconResult
        {
            public string? FinconId { get; set; }
            public string? ErrorMessage { get; set; }
            public bool Error { get; set; }
        }

        public struct BillingDetail
        {
            public string Terms { get; set; }
            public string CreditAvailable { get; set; }
        }

        public struct PaymentMethod
        {
            public int PayID { get; set; }
            public string? Method { get; set; }
        }
        public struct OrderStatus
        {
            public int StatusID { get; set; }
            public string? Status { get; set; }
        }

        //Brands Section

        public static List<Brand> GetBrands()
        {
            List<Brand> brands = new();
            using (var db = new SqlConnection(connString))
            {
                string queryStr = "Select x.Id,x.Name,x.Logo,x.Link,x.MetaTitle,x.MetaDescription,x.Description,x.Featured,x.Position from (Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description],[Featured],[Position],(SELECT Count(p.ProdID) from Products p  where dbo.GetProductStockCount(p.ProdID, p.Status, N'A')>0 and p.ManufID=m.ManufID and p.OutputMe=1 and p.Active=1 and p.OrgID IN (94,380,932,546) and p.ImgURL IS NOT NULL and p.ImgURL!='') as ItemCount from [dbo].[Manufacturers]  m) as x where x.ItemCount>0 ORDER BY x.Name";
                brands = db.Query<Brand>(queryStr).ToList();
            }
            return brands;
        }

        public static List<Brand> GetAllBrands()
        {
            List<Brand> brands = new();
            using (var db = new SqlConnection(connString))
            {
                string queryStr = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description],[Featured],[Position] from Manufacturers ORDER BY [ManufID] DESC";
                brands = db.Query<Brand>(queryStr).ToList();
            }
            return brands;
        }

        public static List<Brand> GetFeaturedBrands(int? pNum, int? pSize)
        {
            if (pNum == null || pNum < 0)
            {
                pNum = 1;
            }
            if (pSize == null || pSize < 1)
            {
                pSize = 12;
            }
            List<Brand> brands = new();
            using (var db = new SqlConnection(connString))
            {
                string queryStr = @"Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] Order By Position";
                brands = db.Query<Brand>(queryStr).ToList();
            }
            return brands;
        }
        public static List<Brand> GetPopularBrands(int? pNum, int? pSize)
        {
            if (pNum == null || pNum < 0)
            {
                pNum = 1;
            }
            if (pSize == null || pSize < 1)
            {
                pSize = 12;
            }
            List<Brand> brands = new();
            using (var db = new SqlConnection(connString))
            {
                string queryStr = @"WITH ProductCounts AS (SELECT ManufID, COUNT(1) AS ItemCount FROM Products p WHERE dbo.GetProductStockCount(p.ProdID, p.Status, N'A') > 0 AND p.OutputMe = 1 AND p.Active = 1 AND p.OrgID IN (94, 380, 932, 546) GROUP BY ManufID) SELECT m.ManufID AS Id, m.ManufacturerName AS Name, m.Logo, m.ManufURL AS Link,m.MetaTitle, m.MetaDescription, m.Description FROM dbo.Manufacturers m JOIN ProductCounts pc ON m.ManufID = pc.ManufID WHERE pc.ItemCount > 0 ORDER BY NEWID() OFFSET " + (pSize * (pNum - 1)) + " ROWS FETCH NEXT " + pSize + " ROWS ONLY";
                brands = db.Query<Brand>(queryStr).ToList();
            }
            return brands;
        }
        public static Brand GetBrand(long? id)
        {
            Brand brands = new();
            using (var db = new SqlConnection(connString))
            {
                string queryStr = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description],[Featured],[Position] from [dbo].[Manufacturers] where [ManufID]=@MfdId";
                var values = new { MfdId = id };
                brands = db.Query<Brand>(queryStr, values).FirstOrDefault();
            }
            return brands;
        }

        internal static List<Product_View> GetBrandProducts(long id)
        {
            string searchWhere = " AND (Products.ManufID=" + id + ")";
            List<Product_View> Products = GetProducts(searchWhere);

            return Products;
        }

        public static List<Product_View> GetProducts(string strWhere)
        {
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            double margin = GetMargin();
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID,Products.ProductName, Products.ProductCode, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE ((Products.Active = 1) AND (Products.OutputMe = 1) AND Products.OrgID IN (94,380,932,546))" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + "))" +
                                "" + strWhere + " ORDER BY Products.CreateDate Desc";

                products = db.Query<Product_View>(strQuery, commandTimeout: 60).ToList();
            }
            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    //item.BrancStocks = GetProductStockCount(item.ProdID);
                    item.Special_Price = GetSpecialPrice(item.ProductCode);
                    returnProducts.Add(item);
                }
            }
            return returnProducts;
        }

        public static OrgWebDetail GetOrgWebDetail()
        {
            string strOrgID = "" + GetOrgID();
            OrgWebDetail detail = new();
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strSQL = @"SELECT TOP (1) Organisation.OrgName, Organisation.WEBEMailInfo, Organisation.WEBEMailOrders, Organisation.WEBOrgURL, Organisation.WEBPriceUsed,Organisation.WEBCustPriceUsed, 
                      Organisation.WEBStockOnly, Organisation.isFranchise, Organisation.WEBMinStock, Organisation.WEBNoImg, Organisation.WEBUseGroup, 
                      Organisation.WEBAutoOrder, Organisation.WEBProdOrderBy, Organisation.OrgRegNo, Organisation.OrgVATNo, Organisation.OrgTel1, Organisation.OrgFax, 
                      Organisation.OrgStreet1, Organisation.OrgStreet2, Organisation.OrgStreet3, Organisation.OrgStreet4, Organisation.OrgStreet5, Organisation.VATRegistered, 
                      Organisation.FromDoorID,Organisation.FinType, Users.UserID,Organisation.OrgLength,Organisation.OrgWidth,Organisation.OrgHeight,Organisation.OrgMass
                FROM Organisation INNER JOIN
                      Users ON Organisation.OrgID = Users.OrgID
                WHERE (Organisation.OrgID = " + strOrgID + ") AND (Users.Menu <> 10);";
                detail = db.Query<OrgWebDetail>(strSQL).FirstOrDefault();
            }
            return detail;
        }

        public static long GetOrgID()
        {

            return StaticConfig.GetValue<long>("OrgID");
        }

        private static double? GetSpecialPrice(string productCode)
        {

            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            double specialprice = 0;
            double margin = GetMargin();
            string sql = "Select TOP 1 Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + "  as SpecialPrice From SpecialPageProduct spp join PromotionSpecialPage psp on psp.Id=spp.PageId inner Join Products on spp.ProductCode=Products.ProductCode where psp.Expired!=1 and psp.Demo!=1 and psp.Active=1 AND spp.StartDate<=GETDATE() and spp.EndDate>=GETDATE() and psp.PageType=N'Reseller Page' and Products.ProductCode='" + productCode + "' AND Products.Active=1 AND Products.OutputMe=1 AND Products.OrgID IN (94,380,932,546)";
            using (var db = new SqlConnection(connString))
            {
                specialprice = db.Query<double>(sql).FirstOrDefault();
                if (specialprice == 0)
                {
                    sql = "Select TOP 1 Products.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + "  From SpecialPageProduct spp join NormalSpecialPage psp on psp.Id=spp.PageId JOIN Products on spp.ProductCode=Products.ProductCode where spp.StartDate<=GETDATE() and spp.EndDate>=GETDATE() and psp.PageType=N'Reseller Page' and Products.Active=1 AND Products.OutputMe=1 AND Products.OrgID in (94,380,932,546) AND spp.ProductCode='" + productCode + "'";
                    specialprice = db.Query<double>(sql).FirstOrDefault();

                }
            }
            return specialprice;
        }

        public static Pricing GetPriceUsed(object custId)
        {
            Pricing pOut = new Pricing();
            pOut.isDiscount = false;
            pOut.NormalPriceNo = 1;
            pOut.DiscountPriceNo = 1;
            pOut.UsePriceNumber = 1;
            string strSQL = "";
            try
            {
                long lngCustID = 0;
                if (!string.IsNullOrEmpty((string)custId))
                    lngCustID = long.Parse(Val(custId.ToString()));
                using (IDbConnection db = new SqlConnection(connString))
                {
                    strSQL = @"SELECT WEBPriceUsed FROM Organisation WHERE (OrgID = @OrgID);
                    SELECT Accounts.UsePrice FROM Accounts INNER JOIN
                    WEBCustomer ON Accounts.AccountID = WEBCustomer.AccountID 
                    WHERE (WEBCustomer.CustID = @CustID) AND (WEBCustomer.Active = 1) AND (Accounts.OrgID = @OrgID)";

                    var values = new { OrgID = GetOrgID(), CustID = lngCustID };
                    var result = db.QueryMultiple(strSQL, values);
                    var uPrice = result.Read<string>().First();
                    if (!string.IsNullOrWhiteSpace(uPrice))
                    {
                        pOut.UsePriceNumber = byte.Parse(Val(uPrice));
                        pOut.NormalPriceNo = pOut.UsePriceNumber;
                    }
                    var dPrice = result.Read<string>().First();
                    if (result.Read<string>().Last() != null)
                    {
                        pOut.DiscountPriceNo = byte.Parse(Val(result.Read<string>().Last()));
                        pOut.UsePriceNumber = pOut.DiscountPriceNo;
                    }
                    else
                    {
                        pOut.DiscountPriceNo = pOut.NormalPriceNo;

                    }
                    if (pOut.NormalPriceNo != pOut.DiscountPriceNo)
                        pOut.isDiscount = true;
                }
            }
            catch
            {

            }
            return pOut;
        }

        public static string Val(string strIn)
        {
            strIn = strIn == null ? "" : strIn;
            char[] chrsIn = strIn.ToCharArray();
            string strOut = "";
            foreach (char charIn in chrsIn)
            {
                if (Char.IsNumber(charIn) || charIn.ToString() == "." || charIn.ToString() == "-")
                {
                    strOut += charIn.ToString();
                }
            }
            if (strOut == "")
            {
                strOut = "0";
            }
            return strOut;
        }

        //Categories Section
        public static IEnumerable<Category> GetCategories()
        {
            List<Category> categories = [];
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "Select x.* from (Select  pgH.GroupHeadID  as Id,pgH.HeadName as Title,pgH.MetaTitle,pgH.MetaDescription,pgH.ImageUrl,pgH.[Description],pgH.[Featured],pgH.[Position],'0' as ItemCount from ProductGroupHead pgH where pgH.OrgID IN (94,380,932,546)) as x ORDER BY x.Title";
                categories = [.. db.Query<Category>(strQuery)];
            }
            return categories;
        }

        public static List<Category> GetFeaturedCategories()
        {
            List<Category> categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "Select pgH.GroupHeadID  as Id,pgH.HeadName as Title,pgH.MetaTitle,pgH.MetaDescription,pgH.ImageUrl,pgH.[Description],pgH.[Featured],pgH.[Position] from ProductGroupHead pgH where pgH.OrgID IN (94,380,932,546) Order By pgH.Position";
                categories = db.Query<Category>(strQuery).ToList();
            }
            return categories;
        }
        public static List<Category> GetPopularCategories(int? pNum, int? pSize)
        {
            if (pNum == null || pNum < 0)
            {
                pNum = 1;
            }
            if (pSize == null || pSize < 1)
            {
                pSize = 12;
            }
            List<Category> categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "Select Y.* From  (SELECT X.* FROM (Select PGH.GroupHeadID as Id,PGH.HeadName as Title,PGH.MetaTitle,PGH.ImageURL,(Select Count (1) FROM PRODUCTS P Where P.ORGID=94 AND P.Active=1 AND P.OutputMe=1 AND P.CategoryID=PGH.GroupHeadID AND dbo.GetProductStockCount(P.ProdID,P.Status,N'A')>0) as ItemCount From ProductGroupHead PGH Where OrgID=94) AS X Where x.ItemCount>0 ORDER BY NEWID() OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY) AS Y UNION Select Y.* From  (SELECT X.* FROM (Select PGH.GroupHeadID as Id,PGH.HeadName as Title,PGH.MetaTitle,PGH.ImageURL,(Select Count (1) FROM PRODUCTS P Where P.ORGID=380 AND P.Active=1 AND P.OutputMe=1 AND P.CategoryID=PGH.GroupHeadID AND dbo.GetProductStockCount(P.ProdID,P.Status,N'A')>0) as ItemCount From ProductGroupHead PGH Where OrgID=380) AS X Where x.ItemCount>0 ORDER BY NEWID() OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY) AS Y UNION Select Y.* From  (SELECT X.* FROM (Select PGH.GroupHeadID as Id,PGH.HeadName as Title,PGH.MetaTitle,PGH.ImageURL,(Select Count (1) FROM PRODUCTS P Where P.ORGID=932 AND P.Active=1 AND P.OutputMe=1 AND P.CategoryID=PGH.GroupHeadID AND dbo.GetProductStockCount(P.ProdID,P.Status,N'A')>0) as ItemCount From ProductGroupHead PGH Where OrgID=932) AS X Where x.ItemCount>0 ORDER BY NEWID() OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY) AS Y UNION Select Y.* From  (SELECT X.* FROM (Select PGH.GroupHeadID as Id,PGH.HeadName as Title,PGH.MetaTitle,PGH.ImageURL,(Select Count (1) FROM PRODUCTS P Where P.ORGID=546 AND P.Active=1 AND P.OutputMe=1 AND P.CategoryID=PGH.GroupHeadID AND dbo.GetProductStockCount(P.ProdID,P.Status,N'A')>0) as ItemCount From ProductGroupHead PGH Where OrgID=546) AS X Where x.ItemCount>0 ORDER BY NEWID() OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY) AS Y";
                categories = db.Query<Category>(strQuery).ToList();
            }
            return categories;
        }

        private static long? GetItemCount(long id)
        {
            string query = "SELECT Count(Distinct p.ProdID) from Products p Join ProductGroups pg on p.GroupName=pg.GroupName Join ProdGroupLink pgl on pgl.ProdGroupName=pg.GroupName where pgl.GroupHeadID=" + id + " and p.OutputMe=1 and p.Active = 1 and p.OrgID IN (94,380,932,546) AND (dbo.GetProductStockCount(p.ProdID, p.Status, N'A')) >=1";
            using (var db = new SqlConnection(connString))
            {
                long count = db.Query<long>(query).FirstOrDefault();
                return count;
            }
        }

        public static Category GetCategory(long? id)
        {
            Category categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "SELECT GroupHeadID  as Id,HeadName as Title,MetaTitle,MetaDescription,ImageUrl,Description,Featured,Position FROM [dbo].[ProductGroupHead] where GroupHeadID=@Id";
                var values = new { Id = id };
                categories = db.Query<Category>(strQuery, values).FirstOrDefault();
            }
            return categories;

        }

        public static long GetCategoryId(long id)
        {
            long CatId = 0;
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "Select Top 1 pG.GroupHeadID From ProductGroupHead pG join ProdGroupLink pGL on pG.GroupHeadID=pGL.GroupHeadID Join Products p on pGL.ProdGroupName=p.GroupName Where pG.OrgID In (94,380,932,546) AND p.Active=1 AND p.OutputMe=1 AND p.ProdID=@Id Order by pG.GroupHeadID";
                var values = new { Id = id };
                CatId = db.Query<long>(strQuery, values).FirstOrDefault();
            }
            return CatId;

        }

        public static List<Product_View> GetCategoryProducts(long CategoryId)
        {
            string subcategories = GetSubCategoriesForCategory("" + CategoryId);
            string searchWhere = " AND (Products.GroupName IN (" + subcategories + "))";
            List<Product_View> Products = new();
            if (!string.IsNullOrWhiteSpace(subcategories))
            {
                Products = GetProducts(searchWhere);
            }
            return Products;

        }

        private static string GetSubCategoriesForCategory(string categories)
        {
            string query = "Select pg.GroupName From ProductGroups pg Join ProdGroupLink pgl on pg.GroupName=pgl.ProdGroupName join ProductGroupHead pgH on pgl.GroupHeadID=pgH.GroupHeadID WHERE pgH.GroupHeadID IN(" + categories + ")";

            List<string> subcategories = new List<string>();
            using (var db = new SqlConnection(connString))
            {
                subcategories = db.Query<string>(query).ToList();
            }

            return string.Join(',', subcategories.Distinct().Select(x => new { x = "'" + x.Replace("'", "''") + "'" }).Select(x => x.x));
        }

        //Sub Categories Section
        public static List<SubCategory> GetSubCategories()
        {
            List<SubCategory> categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546)";
                categories = db.Query<SubCategory>(strQuery).ToList();
            }
            return categories.DistinctBy(x => x.Title).ToList();
        }
        public static List<SubCategory> GetSubGetCategoriesByCategory(long? category_id)
        {
            List<SubCategory> categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.GroupHeadID=" + category_id;
                var values = new { Id = category_id };
                categories = db.Query<SubCategory>(strQuery, category_id).ToList();
            }
            return categories;

        }

        internal static SubCategory GetSubCategory(long? id)
        {
            SubCategory categories = new();
            using (var db = new SqlConnection(connString))
            {
                string strQuery = "SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where ProdGroupID=@Id";
                var values = new { Id = id };
                categories = db.Query<SubCategory>(strQuery, values).FirstOrDefault();
            }
            return categories;
        }

        public static List<Product_View> GetSubCategoryProducts(string id)
        {
            string searchWhere = " AND (GroupName='" + id.Replace("'", "''") + "')";
            List<Product_View> Products = GetProducts(searchWhere);
            return Products;
        }

        //Menu Section
        internal static List<Menu> GetMenu()
        {
            string sqlQuery = "Select * From FrontMenu Where OrgID=" + GetOrgID() + " ORDER BY Position";
            List<Menu> menu = [];
            using (var db = new SqlConnection(connString))
            {
                menu = [.. db.Query<Menu>(sqlQuery)];
            }
            return menu;
        }

        internal static IEnumerable<News> GetNews()
        {
            string strQuery = "SELECT [Id],[Title],[ShortDescription],[Description],[MetaTitle],[CreatedDate],[ImageUrl],[CreatedBy],[LastUpdateDate],[LastUpdatedBy] FROM [dbo].[News]";
            List<News> newsList = new();
            using (var db = new SqlConnection(connString))
            {
                newsList = db.Query<News>(strQuery).ToList();
            }
            return newsList;
        }

        internal static News GetNewsDetail(long id)
        {
            string strQuery = "SELECT [Id],[Title],[ShortDescription],[Description],[MetaTitle],[CreatedDate],[ImageUrl],[CreatedBy],[LastUpdateDate],[LastUpdatedBy] FROM [dbo].[News] WHERE [Id]=" + id;
            News news = new();
            using (var db = new SqlConnection(connString))
            {
                news = db.Query<News>(strQuery).FirstOrDefault();
            }
            return news;
        }

        internal static News GetNewsDetailByMetaTitle(string id)
        {
            string strQuery = "SELECT [Id],[Title],[ShortDescription],[Description],[MetaTitle],[CreatedDate],[ImageUrl],[CreatedBy],[LastUpdateDate],[LastUpdatedBy] FROM [dbo].[News] WHERE [MetaTitle]='" + id + "'";
            News news = new();
            using (var db = new SqlConnection(connString))
            {
                news = db.Query<News>(strQuery).FirstOrDefault();
            }
            return news;
        }

        internal static IEnumerable<NormalSpecialPage> GetNormalSpecialPage()
        {
            string query = "Select * From NormalSpecialPage Order By Date Desc";
            List<NormalSpecialPage> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<NormalSpecialPage>(query).ToList();
            }
            return pageList;
        }

        internal static NormalSpecialPage GetFirstNormalSpecialPage(string pType)
        {
            string query = "Select TOP 1 * From NormalSpecialPage Where PageType like'%" + pType + "%' Order By Date Desc";
            NormalSpecialPage pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<NormalSpecialPage>(query).FirstOrDefault();
            }
            return pageList;
        }

        internal static IEnumerable<NormalSpecialPage> GetCustomerNormalSpecialPage()
        {
            string query = "Select * From NormalSpecialPage WHERE PageType='Customer Page' Order By Date Desc";
            List<NormalSpecialPage> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<NormalSpecialPage>(query).ToList();
            }
            return pageList;
        }

        internal static IEnumerable<NormalSpecialPage> GetResellerNormalSpecialPage()
        {
            string query = "Select * From NormalSpecialPage WHERE PageType='Reseller Page' Order By Date Desc";
            List<NormalSpecialPage> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<NormalSpecialPage>(query).ToList();
            }
            return pageList;
        }

        internal static NormalSpecialPage GetNormalSpecialPageDetail(long id)
        {
            string query = "Select * From NormalSpecialPage where Id=" + id;
            NormalSpecialPage pageList = new NormalSpecialPage();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<NormalSpecialPage>(query).FirstOrDefault();
            }
            return pageList;
        }

        internal static NormalSpecialPage GetNormalSpecialPageDetailByMetaTitle(string id)
        {
            string query = "Select * From NormalSpecialPage where MetaTitle=@MetaTitle";
            NormalSpecialPage pageList = new NormalSpecialPage();
            using (var db = new SqlConnection(connString))
            {
                var values = new { MetaTitle = id };
                pageList = db.Query<NormalSpecialPage>(query, values).FirstOrDefault();
            }
            return pageList;
        }

        internal static List<SpecialPageProduct> GetSpecialPageProducts(long id, string PageType)
        {
            DateTime today = DateTime.UtcNow.AddHours(2);
            Pricing prices = GetPriceUsed(null);
            double margin = GetMargin();
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string query = "";
            query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,p.Active,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as Stock From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) And spp.PageId=" + id + " and spp.PageType='" + PageType + "' and spp.StartDate<='" + today + "' and spp.EndDate>='" + today + "'";

            List<SpecialPageProduct> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = [.. db.Query<SpecialPageProduct>(query)];
            }
            return [.. pageList.Where(x => x.Stock > 0)];
        }

        internal static List<SpecialPageProduct> GetExpiredSpecialPageProducts()
        {
            DateTime today = DateTime.UtcNow.AddHours(2);
            Pricing prices = GetPriceUsed(null);
            double margin = GetMargin();
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " ) as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as Stock From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and spp.PageType='Promotion Special' and spp.EndDate<'" + today + "'";
            List<SpecialPageProduct> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<SpecialPageProduct>(query).ToList();
            }
            return pageList;
        }
        public static List<SpecialPageProductDetail> GetNormalProducts(long id, string pType)
        {
            DateTime today = DateTime.UtcNow.AddHours(2);
            string strWEBPriceUsed = "";
            if (pType == "Reseller Page")
            {
                Pricing prices = GetPriceUsed(null);
                strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            }
            else
            {
                Pricing prices = GetCustomerPriceUsed(null);
                strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            }
            string nQuery = "Select Id From PromotionSpecialPage Where PageType like '%" + pType + "%' AND Expired=0 AND Demo=0";
            List<int> pages = new();
            using (var db = new SqlConnection(connString))
            {
                pages = db.Query<int>(nQuery).ToList();
            }

            double margin = GetMargin();

            string whereQuery = string.Join(',', pages);
            string query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,p.Active,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as StockQty From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and spp.StartDate<='" + today + "' and spp.EndDate>='" + today + "' And spp.PageType='Normal Special' And PageId=" + id;
            if (pages.Count > 0)
            {
                query += @"UNION Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,p.Active,([dbo].[GetProductStockCount](p.ProdID, p.Status, N'A')) as StockQty From SpecialPageProduct spp left Join Products p on spp.ProductCode = p.ProductCode  join Manufacturers m on p.ManufID = m.ManufID Where p.Active = 1 and p.OutputMe = 1 and p.OrgID In(94,380,932,546) and spp.StartDate <= '" + today + "' and spp.EndDate >= '" + today + "' And spp.PageType = 'Promotion Special' and spp.PageId In(" + whereQuery + ")";
            }
            List<SpecialPageProductDetail> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<SpecialPageProductDetail>(query).ToList();
            }
            return pageList.OrderBy(x => x.SpecialPrice).DistinctBy(x => x.ProductCode).Where(x => x.StockQty > 0).ToList();
        }

        internal static SpecialPageProductDetail GetSpecialPageProductDetail(long id)
        {
            OrgWebDetail detail = GetOrgWebDetail();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            SpecialPageProductDetail pDetail = new();
            double margin = GetMargin();
            string query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.[ProdID],p.[OrgID],p.[ProductCode],m.[ManufacturerName] as Brand,p.[ManufId],p.[Description],p.[LongDescription],p.[PurchasePrice]*1.15*" + margin + ",p.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + " AS Price," + "p.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, p.PriceExclVat" + strWEBPublicPriceUsed + " *1.15*" + margin + " AS PublicPrice,p.[GroupName] as SubCategory,p.[UsualAvailability],p.[Notes],p.[URL],p.[ImgURL],p.[Status],p.[Warranty],p.[OrgSourceID],p.[OutputMe],p.[Active],dbo.GetProductStockCount(p.ProdID, p.Status, N'A') AS StockQty,p.[DiscQty],p.[Unit],p.[CreateDate],p.[Length],p.[Width],p.[Height],p.[Mass],p.[DebitOrderFormId],p.[DeliveryID],p.[MasterProdID],p.[AdwordExclude],p.[DataSource],p.[ProductName],p.[CategoryID],p.[Trending],p.[Featured],p.[BestSeller],p.[MostViewed] from SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and spp.Id=" + id;
            using (var db = new SqlConnection(connString))
            {
                pDetail = db.Query<SpecialPageProductDetail>(query).FirstOrDefault();
            }
            List<BranchStock> brancStock = GetProductStockCount(pDetail.ProdID);
            pDetail.BrancStocks = brancStock;
            return pDetail;
        }

        internal static SpecialPageProduct GetSpecialPageProduct(long id)
        {
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            double margin = GetMargin();
            string query = "Select spp.Id,p.ProductCode,spp.PageId,p.ProdID,p.ImgURL,p.Description,spp.Margin,p.Notes,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " AS OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.Active From SpecialPageProduct spp JOIN Products p on spp.ProductCode=p.ProductCode WHERE p.Active=1 AND p.OutputMe=1 and spp.Id=" + id + " AND p.OrgID IN (94,380,932,546)";

            SpecialPageProduct pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<SpecialPageProduct>(query).FirstOrDefault();
            }
            return pageList;
        }

        public static List<BranchStock> GetProductStockCount(long prodId)
        {

            List<BranchStock> toReturn = [];
            try
            {
                using (var db = new SqlConnection(connString))
                {
                    string strSql = @"Select OrganisationBranch.OrgBraID AS Id,OrganisationBranch.OrgBraShort AS ShortName,BranchStock.StockCount From BranchStock Join OrganisationBranch ON BranchStock.OrgBraID=OrganisationBranch.OrgBraID WHERE  BranchStock.ProdId=@ProductId ORDER BY OrganisationBranch.OrgBraShort";
                    var values = new { ProductId = prodId };

                    var result = db.Query<BranchStock>(strSql, values).ToList();
                    List<string> branches = result.Select(x => x.ShortName).Distinct().ToList();
                    foreach (var item in branches)
                    {
                        BranchStock bStock = new()
                        {
                            ShortName = item,
                            StockCount = result.Where(x => x.ShortName == item).Sum(x => x.StockCount),
                            Id = result.Where(x => x.ShortName == item).OrderBy(x => x.Id).Select(x => x.Id).FirstOrDefault()
                        };
                        toReturn.Add(bStock);
                    }
                }
            }
            catch
            {

            }

            return toReturn;
        }

        public static List<ProductFeature> GetProductFeatures(long prodId)
        {
            prodId = GetDistFromResellerProductId(prodId);
            List<ProductFeature> features = new();
            using (var db = new SqlConnection(connString))
            {
                string strSql = @"SELECT FeatureID,ProdID, Description, FeatureValue FROM Features WHERE (ProdID = @ProdID) ORDER BY FeatureOrder";
                features = db.Query<ProductFeature>(strSql, new { ProdID = prodId }).ToList();

            }
            return features;
        }

        public static List<ProductSpecification> GetProductSpecifications(long prodId)
        {
            prodId = GetDistFromResellerProductId(prodId);
            List<ProductSpecification> specifications = new();
            using (var db = new SqlConnection(connString))
            {
                string strSql = @"SELECT SpecificationID as Id, Description, SpecificationValue, SpecificationGroup
                FROM         Specifications
                WHERE     (ProdID = @ProdID)
                ORDER BY GroupOrder, SpecificationGroup, DescriptionOrder";
                specifications = db.Query<ProductSpecification>(strSql, new { ProdID = prodId }).ToList();

            }

            return specifications;
        }

        public static long GetDistFromResellerProductId(long prodId)
        {
            SqlConnection conn = null;
            SqlDataReader reader = null;
            long returnValue = -1;

            try
            {
                string strSql = @"SELECT SourceList.SourceOrgID, Products.ProductCode, Products.MasterProdID
                    FROM OrganisationSource INNER JOIN Products ON OrganisationSource.OrgSourceID = Products.OrgSourceID 
                        INNER JOIN SourceList ON OrganisationSource.SourceID = SourceList.SourceID 
                    WHERE (Products.ProdID = " + prodId.ToString() + ")";

                conn = new SqlConnection(connString);
                conn.Open();
                SqlCommand cmd = new SqlCommand(strSql, conn);

                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    if (Val(reader["MasterProdID"].ToString()) != "0")
                        returnValue = long.Parse(Val(reader["MasterProdID"].ToString()));
                    else
                    {
                        object result = GetDistProductId((long)reader["SourceOrgID"], reader["ProductCode"].ToString());
                        if (result == null)
                            returnValue = prodId;
                        else
                            returnValue = (long)result;
                    }
                }
                else
                {
                    returnValue = prodId;
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (conn != null)
                    conn.Close();
            }
            return returnValue;
        }

        private static long? GetDistProductId(long sourceOrgId, string productCode)
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(@"SELECT ProdID FROM Products 
                    WHERE (OrgID = @OrgID) AND (ProductCode = @ProductCode) AND (Active = 1) AND (OutputMe = 1)", conn);
                cmd.Parameters.AddWithValue("OrgID", sourceOrgId);
                cmd.Parameters.AddWithValue("ProductCode", productCode);
                conn.Open();
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return null;
                else
                    return (long)value;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }

        }

        public static Pricing GetCustomerPriceUsed(object custId)
        {
            Pricing pOut = new Pricing();
            pOut.isDiscount = false;
            pOut.NormalPriceNo = 1;
            pOut.DiscountPriceNo = 1;
            pOut.UsePriceNumber = 1;
            string strSQL = "";
            try
            {
                long lngCustID = 0;
                if (!string.IsNullOrEmpty((string)custId))
                    lngCustID = long.Parse(Val(custId.ToString()));
                using (IDbConnection db = new SqlConnection(connString))
                {
                    strSQL = @"SELECT WEBCustPriceUsed FROM Organisation WHERE (OrgID = @OrgID);
                    SELECT Accounts.UsePrice FROM Accounts INNER JOIN
                    WEBCustomer ON Accounts.AccountID = WEBCustomer.AccountID 
                    WHERE (WEBCustomer.CustID = @CustID) AND (WEBCustomer.Active = 1) AND (Accounts.OrgID = @OrgID)";

                    var values = new { OrgID = GetOrgID(), CustID = lngCustID };
                    var result = db.QueryMultiple(strSQL, values);
                    var uPrice = result.Read<string>().First();
                    if (!string.IsNullOrWhiteSpace(uPrice))
                    {
                        pOut.UsePriceNumber = byte.Parse(Val(uPrice));
                        pOut.NormalPriceNo = pOut.UsePriceNumber;
                    }
                    var dPrice = result.Read<string>().First();
                    if (result.Read<string>().Last() != null)
                    {
                        pOut.DiscountPriceNo = byte.Parse(Val(result.Read<string>().Last()));
                        pOut.UsePriceNumber = pOut.DiscountPriceNo;
                    }
                    else
                    {
                        pOut.DiscountPriceNo = pOut.NormalPriceNo;

                    }
                    if (pOut.NormalPriceNo != pOut.DiscountPriceNo)
                        pOut.isDiscount = true;
                }
            }
            catch
            {

            }
            return pOut;
        }

        public static string GetBigProductImage(string imgUrl)
        {
            if (string.IsNullOrEmpty(imgUrl))
            {
                return imgUrl;
            }
            string strOut = "";
            string strNoPic = StaticConfig.GetValue<string>("NoPic");
            if (imgUrl.Length > 4)
            {
                try
                {
                    int iSplit = imgUrl.LastIndexOf("/");
                    if (iSplit > 0)
                    {
                        string url = imgUrl.Substring(0, iSplit + 1);
                        string file = imgUrl.Substring(iSplit + 1, imgUrl.Length - iSplit - 1);
                        strOut = url + "Big_" + file;
                    }
                }
                catch
                {
                }
            }
            else
                strOut = strNoPic;
            return strOut;
        }

        public static List<ProductImage> GetProductImages(long strProdID)
        {
            List<ProductImage> images = new();
            List<ProductImage> productImages = new();
            try
            {
                long lngDistProdID = GetDistFromResellerProductId(strProdID);
                using (var db = new SqlConnection(connString))
                {
                    string strSQL = @"SELECT [ProdImgID],[ProdID],ImageURL FROM ProductImages WHERE (ProdID = @ProdID) OR (ProdID = @DistiProdID)";
                    var values = new { ProdID = strProdID, DistiProdID = lngDistProdID };
                    productImages = db.Query<ProductImage>(strSQL, values).ToList();
                }
                foreach (var item in productImages)
                {
                    ProductImage pImage = new ProductImage()
                    {
                        ProdImgID = item.ProdImgID,
                        ProdID = item.ProdID,
                        ImageURL = item.ImageURL,
                        BigImageURL = GetBigProductImage(item.ImageURL)
                    };
                    images.Add(pImage);
                }
            }
            catch
            {

            }

            return images;
        }

        public static List<ProductReview> GetProductReviews(long prodId)
        {
            long distProdId = GetDistFromResellerProductId(prodId);

            List<ProductReview> reviews = new();
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strSQL = "SELECT ProdRevID, ProdID, ProdCode, wc.CustID, ProdRevHeading, ProdRevRating, ProdRevPros, ProdRevCons, ProdRevText, ProdRevDate, RefID, ReviewStatusID,FirstName+' '+Surname as Reviewer FROM ReviewProduct rp left Outer join WEBCustomer wc on rp.CustID=wc.CustID WHERE(ProdID = @ProdID) AND ReviewStatusID=2";
                reviews = db.Query<ProductReview>(strSQL, new { ProdID = distProdId }).ToList();
            }
            return reviews;
        }

        internal static List<CategoryFAQ> GetFAQByCategory(long catId)
        {
            List<CategoryFAQ> faqs = new();
            string Query = "Select * From CategoryFAQ WHERE CategoryId=" + catId;

            using (var db = new SqlConnection(connString))
            {
                faqs = db.Query<CategoryFAQ>(Query).ToList();
                return faqs;
            }
        }

        //pages section
        public static List<Pages> GetPages()
        {
            List<Pages> page = new();
            List<Page_Product> products = new();
            string Query = "Select * From Page;Select * From Page_Products;";
            using (var db = new SqlConnection(connString))
            {
                var result = db.QueryMultiple(Query); if (result != null)
                {
                    page = result.Read<Pages>().ToList();
                    products = result.Read<Page_Product>().ToList();
                }
            }
            List<Pages> rPage = new();
            if (page != null && page.Count > 0)
            {
                foreach (var item in page)
                {
                    item.Product_Code = products.Where(x => x.Page_Id == item.Id).Select(x => x.Product_Code).ToArray();
                    rPage.Add(item);
                }
            }
            return rPage;
        }

        public static Pages GetPage(int id)
        {
            Pages page = new();
            string Query = "Select * From Page Where Id=@id;Select Product_Code from Page_Products where Page_Id=@id";
            List<string> productCodes = new();
            using (var db = new SqlConnection(connString))
            {
                var values = new { Id = id };
                var result = db.QueryMultiple(Query, values);
                if (result != null)
                {
                    page = result.Read<Pages>().FirstOrDefault();
                    productCodes = result.Read<string>().ToList();
                }
            }
            if (productCodes != null && productCodes.Count() > 0)
            {
                page.Product_Code = productCodes.ToArray();
            }
            return page;
        }

        public static Pages GetPageByMetaTitle(string id)
        {
            Pages page = new();
            string Query = "Select * From Page Where MetaTitle=@id;";
            List<string> productCodes = new();
            using (var db = new SqlConnection(connString))
            {
                var values = new { Id = id };
                page = db.Query<Pages>(Query, values).FirstOrDefault();
                if (page != null)
                {
                    string secondQuery = "Select Product_Code from Page_Products where Page_Id=@id";
                    var newValues = new { Id = page.Id };
                    productCodes = db.Query<string>(secondQuery, newValues).ToList();
                }
            }
            if (productCodes != null && productCodes.Count() > 0)
            {
                page.Product_Code = productCodes.ToArray();
            }
            return page;
        }

        internal static List<Product_View> GetPageProducts(int pageId)
        {
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            List<string> product_codeList = new();
            string product_codes = "";
            double margin = GetMargin();
            using (var db = new SqlConnection(connString))
            {
                product_codeList = db.Query<string>("Select Product_Code from Page_Products where Page_Id=" + pageId).ToList();
                product_codes = string.Join(",", product_codeList.Select(x => new { data = "'" + x + "'" }).Select(x => x.data).Distinct());
            }
            if (!string.IsNullOrWhiteSpace(product_codes))
            {
                using (var db = new SqlConnection(connString))
                {
                    string strQuery = "SELECT Top 12 Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                     "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + " AS Price," +
                                     "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15*" + margin + " AS PublicPrice," +
                                     "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                     "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                     " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                     " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                     " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID JOIN [Page_Products] pProducts on Products.ProductCode=pProducts.Product_Code WHERE ((Products.Active = 1) AND (Products.OutputMe = 1) AND Products.OrgID IN (94,380,932,546))" +
                                     " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + "))" +
                                    " AND Products.ProductCode IN (" + product_codes + ")";
                    products = db.Query<Product_View>(strQuery).ToList();
                }
            }

            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    item.BrancStocks = GetProductStockCount(item.ProdID);
                    returnProducts.Add(item);
                }
            }
            return returnProducts;
        }

        public static PagedPageImage GetProductImages(string SearchText, int? pageSize, int? pageNum)
        {
            string query = "";
            long ImageCount = 0;
            if (string.IsNullOrWhiteSpace(SearchText))
            {

                query = "SELECT * FROM PageImage ORDER BY CreatedDate Desc OFFSET " + (pageSize * (pageNum - 1)) + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY;Select Count(*) from PageImage;";
            }
            else
            {
                SearchText = SearchText.Replace("'", "''");
                query = "SELECT * FROM PageImage where Title Like '%" + SearchText + "%' ORDER BY CreatedDate Desc OFFSET " + (pageSize * (pageNum - 1)) + " ROWS FETCH NEXT " + pageSize + " ROWS ONLY;Select Count(*) from PageImage  where Title Like '%" + SearchText + "%';";

            }
            List<PageImage> images = new();
            using (var db = new SqlConnection(connString))
            {
                var result = db.QueryMultiple(query);
                if (result != null)
                {
                    images = result.Read<PageImage>().ToList();
                    ImageCount = result.Read<long>().FirstOrDefault();
                }

            }
            long pageCount = Convert.ToInt32(ImageCount / pageSize);
            decimal pageDivision = Convert.ToDecimal(ImageCount) / Convert.ToDecimal(pageSize);
            if ((pageDivision - pageCount) > 0)
            {
                pageCount += 1;
            }
            PagedPageImage pImage = new()
            {
                Images = images,
                PageCount = pageCount
            };

            return pImage;
        }

        public static List<PageImage> SearchProductImages(string SearchText)
        {
            string query = "";
            if (string.IsNullOrWhiteSpace(SearchText))
            {

                query = "SELECT * FROM PageImage ORDER BY CreatedDate Desc";
            }
            else
            {
                SearchText = SearchText.Replace("'", "''");
                query = "SELECT * FROM PageImage where Title Like '%" + SearchText + "%' ORDER BY CreatedDate Desc;";

            }
            List<PageImage> images = new();
            using (var db = new SqlConnection(connString))
            {
                var result = db.QueryMultiple(query);
                if (result != null)
                {
                    images = result.Read<PageImage>().ToList();
                }

            }
            return images;
        }

        public static string GetWebConfigKeyValue(string key)
        {
            try
            {

                return StaticConfig.GetValue<string>(key);
            }
            catch
            {
            }
            return "";
        }

        internal static void AddReview(ProductReview review)
        {
            string query = "INSERT INTO [dbo].[ReviewProduct] ([ProdID],[ProdCode],[OrgID],[CustID],[ProdRevHeading],[ProdRevRating],[ProdRevPros],[ProdRevCons],[ProdRevText],[ProdRevDate],[RefID],[ReviewStatusID]) VALUES (@ProdID,@ProdCode,@OrgID,@CustID,@ProdRevHeading,@ProdRevRating,@ProdRevPros,@ProdRevCons,@ProdRevText,@ProdRevDate,@RefID,@ReviewStatusID)";
            using (var db = new SqlConnection(connString))
            {
                db.Execute(query, review);
            }
        }

        internal static Product_View GetResellerProductByProductCode(string productCode)
        {
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            productCode = WebUtility.UrlDecode(productCode);
            double margin = GetMargin();
            string strQuery = "SELECT Products.ProdID,Products.ProductName, Products.ProductCode, Products.LongDescription,Products.URL," +
                                "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + " AS Price," +
                                "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15*" + margin + " AS PublicPrice," +
                                "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE ((Products.Active = 1) AND (Products.OutputMe = 1) AND Products.OrgID IN (94,380,932,546))" +
                                " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + "))" +
                                " AND ProductCode=N'" + productCode + "' ORDER BY Products.CreateDate Desc";

            using (var db = new SqlConnection(connString))
            {
                Product_View product = db.Query<Product_View>(strQuery).FirstOrDefault();
                return product;
            }
        }

        public static PagedProduct GetPagedProducts(string strWhere, int pNum, int pSize)
        {
            if (pNum == 0)
            {
                pNum = 1;
            }
            if (pSize < 1)
            {
                pSize = 12;
            }
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Shared.Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Shared.Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = @"SELECT Products.ProdID,Products.ProductName, Products.ProductCode, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15*" + margin + " AS PublicPrice," +
                                 "(Select TOP 1 ManufacturerName From Manufacturers Where Manufacturers.ManufID=Products.ManufID) as ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Products INNER JOIN Organisation ON Products.OrgID= Organisation.OrgID WHERE (Products.OrgID IN (94,380,932,546) AND dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A')>0 AND Products.Active=1) " + strWhere + " ORDER BY Price Desc" +
                                 " OFFSET " + (pSize * (pNum - 1)) + @" ROWS FETCH NEXT " + pSize + @" ROWS ONLY;";
                products = db.Query<Product_View>(strQuery).ToList();
            }
            long pCount = GetProductCount(pSize, strWhere);
            List<Product_View> returnProducts = new();
            foreach (var item in products)
            {
                item.Special_Price = GetSpecialPrice(item.ProductCode);
                returnProducts.Add(item);
            }
            PagedProduct pagedProduct = new()
            {
                Products = returnProducts,
                PageCount = pCount
            };
            return pagedProduct;
        }

        public static long GetProductCount(int pageSize, string strWhere)
        {
            int productCount = 0;
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Shared.Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Shared.Val(prices.DiscountPriceNo.ToString());
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Count(*)" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1) AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + "))" +
                                "" + strWhere;
                productCount = db.Query<int>(strQuery).FirstOrDefault();
            }
            long pageCount = Convert.ToInt32(productCount / pageSize);
            decimal pageDivision = Convert.ToDecimal(productCount) / Convert.ToDecimal(pageSize);
            if ((pageDivision - pageCount) > 0)
            {
                pageCount += 1;
            }
            return pageCount;
        }

        public static Product GetProduct(long prodId)
        {
            OrgWebDetail detail = GetOrgWebDetail();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            Product productDetail = new();
            double margin = GetMargin();
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = @"SELECT [ProdID],[OrgID],[ProductCode],[ManufID],[ManufCode],[Description],[LongDescription],[PurchasePrice]*1.15*" + margin + " AS PurchasePrice,Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," + "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice,[GroupName],[UsualAvailability],[Notes],[URL],[ImgURL],[Status],[Warranty],[OrgSourceID],[OutputMe],[Active],dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty,[DiscQty],[Unit],[CreateDate],[Length],[Width],[Height],[Mass],[DebitOrderFormId],[DeliveryID],[MasterProdID],[AdwordExclude],[DataSource],[ProductName],[CategoryID],[Trending],[Featured],[BestSeller],[MostViewed],(Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating FROM [dbo].[Products] WHERE (ProdID = @ProdID);";
                var values = new { ProdID = prodId };
                productDetail = db.Query<Product>(strQuery, values).FirstOrDefault();

                if (productDetail != null)
                {
                    if (productDetail.Height == null || productDetail.Height == 0)
                    {
                        productDetail.Height = detail.OrgHeight;
                    }

                    if (productDetail.Width == null || productDetail.Width == 0)
                    {
                        productDetail.Width = detail.OrgWidth;
                    }

                    if (productDetail.Length == null || productDetail.Length == 0)
                    {
                        productDetail.Length = detail.OrgLength;
                    }
                    productDetail.Special_Price = GetSpecialPrice(productDetail.ProductCode);
                }
            }
            if (productDetail != null)
            {
                productDetail.BrancStocks = GetProductStockCount(prodId);
            }
            return productDetail;
        }

        internal static List<Product_View> GetYouMayLike(string? groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                groupName = groupName.Replace("'", "''");
            }
            string searchWhere = " AND (GroupName='" + groupName + "')";
            PagedProduct Products = GetPagedProducts(searchWhere, 1, 25);
            return Products.Products;
        }

        public static SearchProductResult SearchProduct(string searchText)
        {
            string strWhere = "";
            List<string> keyword = new();
            if (!string.IsNullOrWhiteSpace(searchText))
            {

                //keyword = searchText.Trim().Split(" ").ToList();
                //strWhere = " AND ((Products.ProductCode Like '%" + keyword[0].Replace("'", "''") + "%') OR (Products.ProductCode='" + keyword[0] + "') OR (Products.ProductName LIKE N'%" + keyword[0].Replace("'", "''") + "%') OR (Products.GroupName LIKE N'%" + keyword[0].Replace("'", "''") + "%') OR (Products.Description LIKE N'%" + keyword[0].Replace("'", "''") + "%'))";
                strWhere = " AND ((Products.Tag like N'%" + searchText.Replace("\'", "\'\'") + "%') OR (Products.ProductName like N'%" + searchText.Replace("\'", "\'\'") + "%') OR (Products.ProductCode Like '%" + searchText.Replace("\'", "\'\'") + "%') OR (Products.ProductCode='" + searchText.Replace("\'", "\'\'") + "'))";
            }

            List<Product_View> products = new();
            List<SubCategory> sub_categories = new();
            List<Brand> brands = new();
            products = GetProductsWithZeroStock(strWhere);

            products = products.DistinctBy(x => x.ProdID).ToList();

            if (products != null && products.Count > 0)
            {
                using (var db = new SqlConnection(connString))
                {
                    string subcategories = string.Join(',', products.Where(x => x.GroupName != null).Select(y => new { x = "N'" + y.GroupName.Replace("'", "''") + "'" }).Select(x => x.x).Distinct());
                    string brandIds = string.Join(',', products.Where(x => x.ManufacturerName != null).Select(x => new { y = "N'" + x.ManufacturerName.Replace("'", "''") + "'" }).Select(x => x.y).Distinct());
                    if (!string.IsNullOrEmpty(brandIds))
                    {
                        string strBrandQuery = "Select [ManufID] as Id,[ManufacturerName] as [Name],[Logo],[ManufURL] as Link,[MetaTitle],[MetaDescription],[Description] from [dbo].[Manufacturers] WHERE [ManufacturerName] IN (" + brandIds + ");";
                        brands = db.Query<Brand>(strBrandQuery, commandTimeout: 60).ToList();

                    }
                    if (!string.IsNullOrEmpty(subcategories))
                    {
                        string categoryQuery = "SELECT sCategory.ProdGroupID as Id,sCategory.GroupName as Title,link.GroupHeadID as Category_Id,sCategory.MetaTitle,sCategory.MetaDescription,sCategory.ImageUrl,sCategory.[Description] from ProductGroups sCategory  Join ProdGroupLInk link on sCategory.GroupName=link.ProdGroupName join ProductGroupHead Category on link.GroupHeadID=Category.GroupHeadID Where Category.OrgID IN (94,380,932,546) AND sCategory.GroupName IN (" + subcategories + ");";
                        sub_categories = db.Query<SubCategory>(categoryQuery, commandTimeout: 60).ToList();
                    }
                }
            }
            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    item.Special_Price = GetSpecialPrice(item.ProductCode);
                    returnProducts.Add(item);
                }
            }
            SearchProductResult spr = new()
            {
                Products = returnProducts,
                Brands = [.. brands.DistinctBy(x => x.Id)],
                SubCategories = [.. sub_categories.DistinctBy(x => x.Id)]
            };
            return spr;
        }

        public static List<Product_View> GetProductsWithZeroStock(string strWhere)
        {
            List<Product_View> products = [];
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID,Products.ProductName, Products.ProductCode, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15*" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "(Select TOP 1 ManufacturerName From Manufacturers Where Manufacturers.ManufID=Products.ManufID) as ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Products INNER JOIN Organisation ON Products.OrgID= Organisation.OrgID WHERE (Products.OrgID IN (94,380,932,546) AND dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A')>0 AND Products.Active=1) " + strWhere + " ORDER BY Products.CreateDate Desc";

                products = [.. db.Query<Product_View>(strQuery, commandTimeout: 60)];
            }

            return products;
        }

        public static MissedSearch GetMissedSearch(long msID)
        {
            string QueryStr = "Select * from MissedSearch Where Id=" + msID;
            using (var db = new SqlConnection(connString))
            {
                MissedSearch mSearch = db.Query<MissedSearch>(QueryStr).FirstOrDefault();
                return mSearch;
            }
        }

        internal static MissedSearch SaveMissedSearch(MissedSearch mSearch)
        {
            string QueryStr = "Insert Into MissedSearch ([IP],[SearchString],[Date],[CustID]) OUTPUT Inserted.Id Values (@IP,@SearchString,@Date,@CustID)";
            using (var db = new SqlConnection(connString))
            {
                long msID = db.Query<long>(QueryStr, mSearch).FirstOrDefault();
                return GetMissedSearch(msID);
            }
        }

        public static PagedProduct SearchPagedProduct(string searchText, int pageNum, int pageSize)
        {
            string strWhere = "";
            List<string> keyword = new();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = WebUtility.UrlDecode(searchText);
                strWhere = " AND ((Products.Tag Like '%" + searchText.Replace("'", "''") + "%') OR (Products.ProductName LIKE N'%" + searchText.Replace("'", "''") + "%')  OR (Products.ProductCode Like '%" + searchText.Replace("\'", "\'\'") + "%') OR (Products.ProductCode='" + searchText.Replace("\'", "\'\'") + "'))";
            }

            PagedProduct PagedProducts = new();
            PagedProducts = GetPagedProducts(strWhere, pageNum, pageSize);
            PagedProducts.Products = PagedProducts.Products.DistinctBy(x => x.ProdID).ToList();
            List<Product_View> products = PagedProducts.Products;

            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    item.Special_Price = GetSpecialPrice(item.ProductCode);
                    returnProducts.Add(item);
                }
            }
            PagedProducts.Products = returnProducts.OrderByDescending(x => x.PublicPrice).ToList();
            return PagedProducts;
        }

        public static List<Product_View> GetLastThirtyDayProducts()
        {
            var oneMonthAgo = DateTime.UtcNow.AddHours(2).AddMonths(-1).ToString("yyyy/MM/dd");
            string strWhere = " AND (Products.CreateDate>='" + oneMonthAgo + "')";
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());
            double margin = GetMargin();
            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT TOP 30 Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') " + strWhere + " ORDER BY Products.CreateDate";
                products = db.Query<Product_View>(strQuery).ToList();
            }

            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    item.Special_Price = GetSpecialPrice(item.ProductCode);
                    returnProducts.Add(item);
                }
            }
            return returnProducts;
        }

        internal static List<Product_View> GetLatestProducts()
        {
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Shared.Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Shared.Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Shared.Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT TOP 30 Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') ORDER BY NEWID()";
                products = db.Query<Product_View>(strQuery).ToList();
            }

            List<Product_View> returnProducts = new();
            if (products != null && products.Any())
            {
                foreach (var item in products)
                {
                    item.Special_Price = GetSpecialPrice(item.ProductCode);
                    returnProducts.Add(item);
                }
            }
            return returnProducts;
        }

        internal static List<Product_View> GetMostViewedProducts()
        {

            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetCustomerPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') AND (Products.MostViewed=1) ORDER BY NEWID();";
                products = db.Query<Product_View>(strQuery).ToList();
            }
            List<Product_View> returnProducts = new();
            foreach (var item in products)
            {
                item.Special_Price = GetSpecialPrice(item.ProductCode);
                returnProducts.Add(item);
            }
            return returnProducts;
        }

        internal static List<Product_View> GetTrendingProducts()
        {

            List<Product_View> products = [];
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetCustomerPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15 *" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') AND (Products.Trending=1) ORDER BY NEWID();";
                products = db.Query<Product_View>(strQuery).ToList();
            }
            List<Product_View> returnProducts = new();
            foreach (var item in products)
            {
                item.Special_Price = GetSpecialPrice(item.ProductCode);
                returnProducts.Add(item);
            }
            return returnProducts;
        }

        internal static List<Product_View> GetBestSellerProducts()
        {
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetCustomerPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + "  AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') AND (Products.BestSeller=1) ORDER BY NEWID();";
                products = db.Query<Product_View>(strQuery).ToList();
            }
            List<Product_View> returnProducts = new();
            foreach (var item in products)
            {
                item.Special_Price = GetSpecialPrice(item.ProductCode);
                returnProducts.Add(item);
            }
            return returnProducts;
        }

        public static List<Product_View> GetFeaturedProducts()
        {
            List<Product_View> products = new();
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBProdOrderBy = detail.WEBProdOrderBy.ToString();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string strWEBMinStock = detail.WEBMinStock.ToString();
            Pricing prices = GetCustomerPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string strWEBPublicPriceUsed = Val(prices.NormalPriceNo.ToString());
            string strWEBDiscountPriceUsed = Val(prices.DiscountPriceNo.ToString());

            double margin = GetMargin();

            using (IDbConnection db = new SqlConnection(connString))
            {
                string strQuery = "SELECT Products.ProdID, Products.ProductCode,Products.ProductName, Products.LongDescription,Products.URL," +
                                 "Products.ImgURL, Products.Description, Products.PriceExclVat" + strWEBPriceUsed + " *1.15*" + margin + " AS Price," +
                                 "Products.PriceExclVat" + strWEBDiscountPriceUsed + " *1.15 *" + margin + " AS DiscountPrice, Products.PriceExclVat" + strWEBPublicPriceUsed + " *1.15 *" + margin + " AS PublicPrice," +
                                 "Manufacturers.ManufacturerName,Products.GroupName, Organisation.OrgName, Products.Status, Products.CreateDate," +
                                 "dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') AS StockQty, (Select ROUND(AVG(CAST(rp.ProdRevRating AS FLOAT)), 2) From ReviewProduct rp where rp.ProdID=Products.ProdID) as Rating" +
                                 " FROM Manufacturers RIGHT OUTER JOIN SourceList INNER JOIN OrganisationSource" +
                                 " ON SourceList.SourceID = OrganisationSource.SourceID INNER JOIN Organisation ON SourceList.SourceOrgID = Organisation.OrgID RIGHT OUTER JOIN Products" +
                                 " ON OrganisationSource.OrgSourceID = Products.OrgSourceID ON Manufacturers.ManufID = Products.ManufID WHERE (Products.Active = 1) AND (Products.OutputMe = 1)  AND Products.OrgID IN (94,380,932,546)" +
                                 " AND ((dbo.GetProductStockCount(Products.ProdID, Products.Status, N'A') >= " + strWEBMinStock + ")) AND (Products.ImgURL IS NOT NULL AND Products.ImgURL!='') AND (Products.Featured=1) ORDER BY NEWID();";
                products = db.Query<Product_View>(strQuery).ToList();
            }
            List<Product_View> returnProducts = new();
            foreach (var item in products)
            {
                item.Special_Price = GetSpecialPrice(item.ProductCode);
                returnProducts.Add(item);
            }
            return returnProducts;
        }

        internal static List<SpecialPageProduct> GetSpecialFeaturedProducts()
        {
            DateTime today = DateTime.UtcNow.AddHours(2);
            Pricing prices = GetPriceUsed(null);
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string query = "";

            double margin = GetMargin();

            query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15 *" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as Stock From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and ([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A'))>1 and spp.StartDate<='" + today + "' and spp.EndDate>='" + today + "' Order By NEWID() OFFSET 0 ROWS FETCH NEXT 40 ROWS ONLY";
            List<SpecialPageProduct> pageList = new();
            using (var db = new SqlConnection(connString))
            {
                pageList = db.Query<SpecialPageProduct>(query).ToList();
            }
            return pageList.Where(x => x.Stock > 0).ToList();
        }

        //Slider Section
        internal static IEnumerable<Slider> GetSlider()
        {
            string query = "Select * From Slider";
            List<Slider> sliders = [];
            using (var db = new SqlConnection(connString))
            {
                sliders = [.. db.Query<Slider>(query)];
            }
            return sliders;
        }
        internal static IEnumerable<Slider> GetClientSlider()
        {
            string query = "Select * From Slider WHERE SliderType='Customer'";
            List<Slider> sliders = [];
            using (var db = new SqlConnection(connString))
            {
                sliders = [.. db.Query<Slider>(query)];
            }
            return sliders;
        }

        internal static IEnumerable<Slider> GetResellerSlider()
        {
            string query = "Select * From Slider WHERE SliderType='Reseller'";
            List<Slider> sliders = [];
            using (var db = new SqlConnection(connString))
            {
                sliders = [.. db.Query<Slider>(query)];
            }
            return sliders;
        }

        internal static Slider GetSliderDetail(int id)
        {
            string query = "Select * From Slider Where Id=" + id;
            Slider slider = new();
            using (var db = new SqlConnection(connString))
            {
                slider = db.Query<Slider>(query).FirstOrDefault();
            }
            return slider;

        }

        public static List<CartItem> GetCartItems(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                OrgWebDetail detail = GetOrgWebDetail();
                string strWEBStockOnly = detail.WEBStockOnly.ToString();
                string query = @"SELECT wb.BasketID,wb.SessionID,wb.custID,wb.OrgID, wb.ProdID, wb.ProdQty, wb.ProdDesc, wb.Price, wb.ProdCode,p.ImgURL,dbo.GetProductStockCount(p.ProdID, p.Status, N'A') AS StockQuantity FROM WEBBasket wb join Products p on wb.ProdID=p.ProdID where SessionID=@Id";
                using (var db = new SqlConnection(connString))
                {
                    var values = new { Id = sessionId };
                    List<CartItem> items = db.Query<CartItem>(query, values).ToList();
                    List<CartItem> returnList = new();
                    foreach (var item in items)
                    {
                        Product product = GetProduct(item.ProdID);
                        product.BrancStocks = GetProductStockCount(item.ProdID);
                        item.Product = product;
                        returnList.Add(item);
                    }
                    return returnList;
                }
            }
            else
            {
                return new List<CartItem>();
            }
        }

        public static List<CartItem> GetCartItemsByCustId(long CustId)
        {

            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string query = @"SELECT wb.BasketID,wb.SessionID,wb.custID,wb.OrgID, wb.ProdID, wb.ProdQty, wb.ProdDesc, wb.Price, wb.ProdCode,p.ImgURL,dbo.GetProductStockCount(p.ProdID, p.Status, N'A') AS StockQuantity FROM WEBBasket wb join Products p on wb.ProdID=p.ProdID where CustId=@CustomerId and wb.OrgId=" + GetOrgID();
            using (var db = new SqlConnection(connString))
            {
                var values = new { CustomerId = CustId };
                List<CartItem> items = db.Query<CartItem>(query, values).ToList();
                List<CartItem> returnList = new();
                foreach (var item in items)
                {
                    Product product = GetProduct(item.ProdID);
                    product.BrancStocks = GetProductStockCount(item.ProdID);
                    item.Product = product;
                    returnList.Add(item);
                }
                return returnList;
            }

        }
        public static long AddToCart(CartItem cartItem)
        {
            string query = @"INSERT INTO WEBBasket (OrgID, SessionID, ProdID, ProdQty, ProdDesc, Price, ProdCode, CustID) OUTPUT INSERTED.BasketID VALUES (@OrgID,@SessionID,@ProdID,@ProdQty,@ProdDesc,@Price,@ProdCode,@CustID)";
            using (var db = new SqlConnection(connString))
            {
                return db.Query<long>(query, cartItem).FirstOrDefault();
            }
        }

        public static bool UpdateCart(CartItem cartItem)
        {
            try
            {

                string query = @"UPDATE WEBBasket SET OrgID=@OrgID, ProdQty=@ProdQty, ProdDesc=@ProdDesc, Price=@Price, ProdCode=@ProdCode Where CustID=" + cartItem.CustID + " AND ProdID=" + cartItem.ProdID + " AND OrgID=" + cartItem.OrgID;
                using (var db = new SqlConnection(connString))
                {
                    db.Execute(query, cartItem);
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        public static bool UpdateCartItems(List<CartItem> cartItems)
        {
            try
            {

                string query = @"UPDATE WEBBasket SET OrgID=@OrgID, ProdQty=@ProdQty, ProdDesc=@ProdDesc, Price=@Price, ProdCode=@ProdCode,CustID=@CustID";
                using (var db = new SqlConnection(connString))
                {
                    db.Execute(query, cartItems);
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        internal static bool RemoveCartItem(long id, string sessionId)
        {
            try
            {
                string query = @"DELETE FROM WEBBasket Where SessionID='" + sessionId + "' and BasketID=" + id;
                using (var db = new SqlConnection(connString))
                {
                    db.Execute(query);
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        internal static bool RemoveCartItemWithCustId(long id, long CustId)
        {
            try
            {
                string query = @"DELETE FROM WEBBasket Where CustId=" + CustId + " and BasketID=" + id;
                using (var db = new SqlConnection(connString))
                {
                    db.Execute(query);
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        public static async Task<string> GetConnectID(string FinconUrl, string FinconServerUsername, string FinconServerPassword)
        {
            string FinconUsername = Shared.GetWebConfigKeyValue("FinconUsername");
            string FinconPassword = Shared.GetWebConfigKeyValue("FinconPassword");
            string UseAltExt = Shared.GetWebConfigKeyValue("UseAltExt");
            string DataID = Shared.GetWebConfigKeyValue("DataID");
            string ConnectID = "";
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(FinconServerUsername + ":" + FinconServerPassword)));

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(FinconUrl + "\"Login\"/" + DataID + "/" + FinconUsername + "/" + FinconPassword + "/" + UseAltExt),
                    };

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (responseBody.Contains("ConnectID"))
                    {
                        ConnectID = responseBody.Split(",")[1].Split(":")[1].Replace("\"", "");
                    }
                }
            }
            catch
            {
            }
            return ConnectID;
        }

        public static async Task<ProductActiveCheck> CheckProductStatus(string ConnectId, string productCode)
        {
            productCode = UrlEncoder.Default.Encode(productCode);
            ProductActiveCheck pStatus = new ProductActiveCheck() { Active = true, ErrorMessage = null, ProductCode = productCode };
            string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
            string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
            string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
            string SalesOrderLink = FinconUrl + "\"GetStock\"" + "/" + ConnectId + "/" + productCode + "/" + productCode + "/0,1,2,3,4,5,7,8/false";
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.ConnectionClose = true;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(FinconServerUsername + ":" + FinconServerPassword)));

                var response = await client.GetAsync(SalesOrderLink).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                dynamic obj = JObject.Parse(responseBody);
                int count = (int)obj.result[0].Count;

                if (count == 0)
                {
                    pStatus.ErrorMessage = "Server didn't return any products.";
                    pStatus.Active = false;
                }
                JArray stockList = obj.result[0].Stock;
                var stocks = stockList.ToObject<List<StockUpdate>>();
                if (stocks == null)
                {
                    pStatus.ErrorMessage = "Server didn't return any products.";
                    pStatus.Active = false;
                }
                foreach (var item in stocks)
                {
                    if (item.Active == "Y")
                    {
                        pStatus.Active = true;
                    }
                    else
                    {
                        pStatus.Active = false;
                    }
                }
                return pStatus;
            }
        }

        public static async Task<FinconResult> UpdateFincon(long id, string ConnectID, string FinconServerUsername, string FinconServerPassword)
        {
            FinconResult fResult = new()
            {
                FinconId = null,
                Error = true,
                ErrorMessage = "Something went wrong."
            };
            try
            {

                Order order = Shared.GetOrder(id);
                if (order == null)
                {
                    return fResult;
                }
                List<OrderItem> items = Shared.GetOrderItems(id);
                List<FinconSalesOrderDetail> orderdetais = new();
                int itemCount = 1;
                string AccountNumber = Shared.GetAccountNumber(order.CustID);
                foreach (var item in items)
                {
                    double exclTax = Math.Round(((Math.Round(item.Price, 2) / 1.15) * item.ProdQty), 2);
                    double tax = Math.Round((Math.Round(item.Price, 2) * item.ProdQty - exclTax), 2);
                    FinconSalesOrderDetail oDetail = new()
                    {
                        Description = item.ProdDesc,
                        Quantity = item.ProdQty,
                        UnitCost = Math.Round(item.Price, 2),
                        ItemNo = item.ProdCode,
                        LineTotalExcl = exclTax,
                        LineTotalTax = tax
                    };
                    itemCount++;
                    orderdetais.Add(oDetail);
                }



                char DeliveryMethod = 'C';
                if (order.DeliveryMethod == "Collect from shop")
                {
                    DeliveryMethod = 'C';
                }
                else if (order.DeliveryMethod == "Courier Direct")
                {
                    DeliveryMethod = 'R';

                    double exclTax = Math.Round((Convert.ToDouble(order.DeliveryCost) / 1.15), 2);
                    double tax = Math.Round((Convert.ToDouble(order.DeliveryCost) - exclTax), 2);
                    FinconSalesOrderDetail oDetail = new()
                    {
                        Description = "Delivery Cost",
                        Quantity = 1,
                        UnitCost = Math.Round(Convert.ToDouble(order.DeliveryCost), 2),
                        ItemNo = "CDT001",
                        LineTotalExcl = exclTax,
                        LineTotalTax = tax
                    };
                    itemCount++;
                    orderdetais.Add(oDetail);
                }
                string location = Shared.GetLocation(order.OrgBranchID);

                FinconSalesOrder salesOrder = new()
                {
                    AccNo = AccountNumber,
                    RepCode = "003",
                    TotalExcl = Math.Round(orderdetais.Sum(x => x.LineTotalExcl), 2),
                    TotalTax = Math.Round(orderdetais.Sum(x => x.LineTotalTax), 2),
                    DeliveryMethod = DeliveryMethod,
                    NumberOfItems = orderdetais.Count,
                    Status = "" + order.StatusID,
                    SalesOrderDetail = orderdetais,
                    LocNo = location
                };
                double totalAmount = salesOrder.TotalExcl + salesOrder.TotalTax;
                Serilog.Log.Error("Order No. : " + order.OrderID + " : Fincon Total Price : " + totalAmount);
                string bodyContent = Newtonsoft.Json.JsonConvert.SerializeObject(salesOrder, Formatting.Indented);
                string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");

                string SalesOrderLink = FinconUrl + "\"CreateSalesOrder\"" + "/" + ConnectID + "/false";
                var buffer = System.Text.Encoding.UTF8.GetBytes(bodyContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.ConnectionClose = true;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(FinconServerUsername + ":" + FinconServerPassword)));

                    var response = await client.PostAsync(SalesOrderLink, byteContent).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    dynamic obj = JObject.Parse(responseBody);
                    if (!string.IsNullOrWhiteSpace(Convert.ToString(obj.result[0].ErrorInfo)))
                    {
                        fResult.Error = true;
                        fResult.ErrorMessage = Convert.ToString(obj.result[0].ErrorInfo);
                        //"Server is too busy. Please try again in a few minutes.";
                        fResult.FinconId = null;
                        return fResult;
                    }
                    string SalesOrder = (string)obj.result[0].SalesOrderInfo.OrderNo;

                    fResult.Error = false;
                    fResult.ErrorMessage = "";
                    fResult.FinconId = SalesOrder;
                    return fResult;
                }
            }
            catch (Exception ex)
            {
                fResult.Error = true;
                if (ex.Message.Contains("A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond."))
                {
                    fResult.ErrorMessage = "Connection Error";
                }
                else
                {
                    fResult.ErrorMessage = ex.Message;
                }
                //"Server is too busy. Please try again in a few minutes.";
                fResult.FinconId = null;
                return fResult;
            }
        }

        internal static string GetLocation(long orgBranchID)
        {
            long orgId = GetOrgID();
            string strSQL = @"SELECT OrgBraCodeExt FROM OrganisationBranch WHERE (OrgID = @OrgID) AND (OrgBraID = @BranchID);";
            string location = "00";
            using (var db = new SqlConnection(connString))
            {
                var values = new { OrgID = orgId, BranchID = orgBranchID };
                string fLocation = db.Query<string>(strSQL, values).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fLocation))
                {
                    location = fLocation;
                }
            }
            return location;
        }


        internal static bool ClearQuotationCartWithCustId(string custID)
        {
            try
            {
                string query = @"DELETE FROM QuotationCart WHERE CustID=" + custID;
                using (var db = new SqlConnection(connString))
                {
                    int output = db.Execute(query);
                    if (output > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        internal static int GetProductTotalStockCount(long prodID)
        {
            var bStocks = GetProductStockCount(prodID);
            int productCount = 0;
            foreach (var item in bStocks)
            {
                productCount += Convert.ToInt32(item.StockCount);
            }
            return productCount;
        }
        internal static bool ClearCartWithCustId(long CustId)
        {
            try
            {
                string query = @"DELETE FROM WEBBasket Where CustId=" + CustId;
                using (var db = new SqlConnection(connString))
                {
                    int output = db.Execute(query);
                    if (output > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool ClearCart(string sessionId)
        {
            try
            {
                string query = @"DELETE FROM WEBBasket Where SessionID='" + sessionId + "'";
                using (var db = new SqlConnection(connString))
                {
                    int output = db.Execute(query);
                    if (output > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        internal static List<WebDeliveryMethods> GetDeliveryMethods()
        {
            List<WebDeliveryMethods> dMethods = new();
            using (var db = new SqlConnection(connString))
            {
                string query = "Select WEBDelivery.DeliveryID as DeliveryID,WEBDeliveryDesc.DeliveryDesc as Area From WEBDelivery JOin WEBDeliveryDesc on WebDelivery.DeliveryDescId=WEBDeliveryDesc.DeliveryDescId where WEBDelivery.OrgID =" + Shared.GetOrgID();
                dMethods = db.Query<WebDeliveryMethods>(query).ToList();
            }
            return dMethods;
        }

        internal static string GetShippingId(long customerID)
        {
            string query = "Select top 1 ShippingID From WEBCustShipping Where CustID=" + customerID;
            string shippingId = "0";
            using (var db = new SqlConnection(connString))
            {
                shippingId = db.Query<string>(query).FirstOrDefault();
            }
            if (string.IsNullOrEmpty(shippingId))
            {
                shippingId = "0";
            }
            return shippingId;
        }

        internal static bool CheckUserIsActive(long custId)
        {
            string strSql = @"SELECT Top 1 Accounts.Active FROM WEBCustomer INNER JOIN Accounts ON WEBCustomer.AccountID = Accounts.AccountID WHERE (WEBCustomer.CustID = " + custId + " AND Accounts.OrgID IN (94,380,932,546,473)) Order By WEBCustomer.CustID";
            using (var connection = new SqlConnection(connString))
            {
                bool result = connection.Query<bool>(strSql).FirstOrDefault();
                return result;

            }
        }

        public static void UpdateCartPrice(long CustID)
        {
            string query = "Select * From WebBasket where CustID=" + CustID + " AND ORGID=94";
            List<CartItem> cartItems = new();
            using (var db = new SqlConnection(connString))
            {
                cartItems = db.Query<CartItem>(query).ToList();
            }
            if (cartItems != null && cartItems.Count > 0)
            {
                List<CartItem> updatedCartItems = new();
                foreach (var item in cartItems)
                {
                    Product product = GetProduct(item.ProdID);
                    if (product != null)
                    {
                        if (product.Special_Price > 0)
                        {
                            if (item.Price != Convert.ToDecimal(product.Special_Price))
                            {
                                item.Price = Math.Round(Convert.ToDecimal(product.Special_Price), 2);
                                updatedCartItems.Add(item);
                            }
                        }
                        else if (item.Price != Convert.ToDecimal(product.Price))
                        {
                            item.Price = Math.Round(Convert.ToDecimal(product.Price), 0);
                            updatedCartItems.Add(item);
                        }
                    }
                }
                if (updatedCartItems.Count > 0)
                {
                    string nQuery = "Update WebBasket set Price=@Price where CustID=" + CustID + " AND ProdID=@ProdID AND ORGID=94";
                    using (var db = new SqlConnection(connString))
                    {
                        db.Execute(nQuery, updatedCartItems);
                    }
                }
            }
        }

        public static Customer GetCustomer(long id)
        {
            string query = "SELECT [CustID],[OrgID],[AccountID],[FirstName],[Surname],[Tel],[Tel2],[Fax],[Email],[Company],[PostalAdd],[PostalCode],[DateCreated],[Title],[CellNo],[Notes],[PostalCountry],[PostalAddressIEID],[IdNo],[VatNo],[SendEmails],[ReferenceCode],[IsCommissionActive],[TimesToUseCommission],[FraudulentUserID],[Password],[UserType] FROM [dbo].[WEBCustomer] where [CustID]=@CustomerId";
            Customer customer = new();
            using (var db = new SqlConnection(connString))
            {
                var values = new { CustomerId = id };
                customer = db.Query<Customer>(query, values).FirstOrDefault();
            }
            return customer;
        }

        internal static List<BankDetails> GetBankDetails()
        {
            string query = "SELECT [BankID],[BankName],[BranchName],[BranchCode],[AccountNo],[OrgID],[OrgBranchID],[AccountName],[BankNameId],[BankAccountTypeId] FROM [dbo].[WEBBank] WHERE OrgId=" + GetOrgID();
            List<BankDetails> bDetails = new();
            using (var db = new SqlConnection(connString))
            {
                bDetails = db.Query<BankDetails>(query).ToList();
            }
            return bDetails;
        }

        internal static long getDeliveryID(string deliveryType)
        {
            string query = "Select WEBDelivery.DeliveryID From WEBDelivery JOin WEBDeliveryDesc on WebDelivery.DeliveryDescId = WEBDeliveryDesc.DeliveryDescId where WEBDelivery.OrgID = " + Shared.GetOrgID() + " AND WEBDeliveryDesc.DeliveryDesc=N'" + deliveryType + "'";

            long DeliveryId = 0;
            using (var db = new SqlConnection(connString))
            {
                DeliveryId = db.Query<long>(query).FirstOrDefault();
                return DeliveryId;
            }
        }

        internal static long GetProductStock(long prodID)
        {
            string query = "Select [dbo].GetProductStockCount(Products.ProdID,Products.Status,N'A') as Stock from Products Where ProdID=" + prodID;
            long stockCount = 0;
            using (var db = new SqlConnection(connString))
            {
                stockCount = db.Query<long>(query).FirstOrDefault();
            }
            return stockCount;
        }

        public static DeliveryDetails getDeliveryDescID(long deliveryID)
        {
            DeliveryDetails details = new DeliveryDetails();
            try
            {
                using (var db = new SqlConnection(connString))
                {
                    string strSQL = "SELECT WEBDeliveryDesc.DeliveryDesc, WEBDelivery.Area, WEBDelivery.Cost, " +
                   "WEBDeliveryDesc.DeliveryDescID,WebDelivery.DeliveryID FROM WEBDelivery INNER JOIN " +
                   "WEBDeliveryDesc ON WEBDelivery.DeliveryDescID = WEBDeliveryDesc.DeliveryDescID " +
                   "WHERE (WEBDelivery.DeliveryID = " + deliveryID.ToString() + ");";
                    details = db.Query<DeliveryDetails>(strSQL).FirstOrDefault();

                    details.Cost = details.Cost;
                    if (details.DeliveryDesc.ToString().ToLower().Trim() == details.Area.ToString().ToLower().Trim())
                        details.DeliveryDesc = details.DeliveryDesc.ToString();
                    else if (details.Area.ToString().ToLower().Trim().Contains(details.DeliveryDesc.ToString().ToLower().Trim()))
                        details.DeliveryDesc = details.Area.ToString();
                    else
                    {
                        if (!string.IsNullOrEmpty(details.Area.ToString()))
                            details.DeliveryDesc = details.DeliveryDesc.ToString() + " - " + details.Area.ToString();
                        else
                            details.DeliveryDesc = details.DeliveryDesc.ToString();
                    }
                    details.DeliveryID = details.DeliveryID;
                    details.DeliveryDescID = (int)details.DeliveryDescID;

                }
            }
            catch
            {

            }

            return details;
        }

        public static bool UpdateOrderStatus(long id, int status, string changeBy)
        {
            try
            {
                string q1 = @"Select StatusID from WEBOrders where OrderID=" + id;
                using (var db = new SqlConnection(connString))
                {
                    long OLD_STATUS_ID = db.Query<long>(q1).FirstOrDefault();
                    string q2 = @"INSERT INTO [dbo].[WEBOrderStatusChange] ([OrderID],[NewStatusID],[ChangeDateTime],[ChangeBy]) OUTPUT Inserted.WebOrderStatusChangeID VALUES (@OrderID,@NewStatusID,@ChangeDateTime,@ChangeBy)";
                    var values = new { OrderID = id, NewStatusID = status, ChangeDateTime = DateTime.UtcNow.AddHours(2), ChangeBy = changeBy };
                    var changeId = db.Query(q2, values).FirstOrDefault();
                    if (changeId != null)
                    {
                        var q3 = @"Update [dbo].[WEBOrders] SET StatusID=@StatusId where OrderID=@OrderId";
                        var qValues = new { OrderId = id, StatusId = status };
                        db.Execute(q3, qValues);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static BranchDetail getBranchName(string strBranchID)
        {
            BranchDetail bOut = new();
            bOut.BranchName = "";
            bOut.BranchEMail = "";
            using (SqlConnection Conn = new(connString))
            {
                Conn.Open();
                string strSQL = @"SELECT OrgBraName, OrgBraEMailTo,OrgBraShort FROM OrganisationBranch WHERE (OrgID = @OrgID) AND (OrgBraID = @BranchID);";
                using (SqlCommand cmd = new SqlCommand(strSQL, Conn))
                {
                    cmd.Parameters.AddWithValue("OrgID", Shared.GetOrgID());
                    cmd.Parameters.AddWithValue("BranchID", int.Parse(strBranchID));
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        bOut.BranchName = reader["OrgBraName"].ToString();
                        bOut.BranchEMail = reader["OrgBraEMailTo"].ToString();
                        bOut.OrgBraShort = reader["OrgBraShort"].ToString();
                    }
                    cmd.Dispose();
                }
                Conn.Dispose();
            }
            return bOut;
        }

        public static void SendMailHangFire(string strSubject, string strHTMLBody, List<string> to, string from, bool includeSignature)
        {
            SendMail(strSubject, strHTMLBody, to.ToArray(), from, null, includeSignature);
        }

        public static void SendMailHangFire(string strSubject, string strHTMLBody, List<string> to, List<string> cc, string from, bool includeSignature)
        {
            SendMail(strSubject, strHTMLBody, to.ToArray(), from, cc, includeSignature);
        }

        public static void SendMail(string strSubject, string strHTMLBody, string[] to, string from, List<string> bcc, bool includeSignature)
        {
            try
            {
                //string[] strAdminEmails = getAdminEMail().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                string strSite = GetWebConfigKeyValue("SiteName");
                MailMessage myMail = new MailMessage();
                myMail.Subject = strSubject;
                foreach (string m in to)
                    myMail.To.Add(new MailAddress(m));
                if (from != "productquestions@improweb.com")
                    myMail.CC.Add(new MailAddress(from));

                //foreach (string adminEmail in strAdminEmails)
                //    myMail.Bcc.Add(new MailAddress(adminEmail));

                if (bcc != null)
                {
                    foreach (string m in bcc)
                        myMail.Bcc.Add(new MailAddress(m));
                }
                string strTitle = "";
                if (includeSignature)
                    strTitle = "Sent From " + strSite;

                myMail.From = new MailAddress(from);
                myMail.IsBodyHtml = true;

                myMail.Body = "<html><head><TITLE>" + strTitle + "</TITLE>" +
                    "<STYLE>BODY {FONT-SIZE: 12px; FONT-FAMILY: Arial, helvetica, sans-serif; " +
                    "SCROLLBAR-BASE-COLOR:#2D5281;}</STYLE></head><BODY>" +
                    "<table WIDTH=100% BORDER=0 cellspacing=0 cellpadding=0><tr><td>" + strHTMLBody +
                    "</td></tr><tr><td><HR></td></tr></table></BODY></html>";
                SmtpClient client = MailSetup();
                client.Send(myMail);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex.Message);

            }
        }

        public static void sendMail(string strSubject, string strHTMLBody, MailAddress[] to, MailAddress from, MailAddress[] bcc, bool includeSignature)
        {
            sendMail(strSubject, strHTMLBody, to, from, bcc, null, includeSignature);
        }

        public static void SendMail(string strSubject, string strHTMLBody, string[] to, string from, List<string> bcc, bool includeSignature, string attachment, string orderId, string type)
        {
            try
            {
                //string[] strAdminEmails = getAdminEMail().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                string strSite = GetWebConfigKeyValue("SiteName");
                MailMessage myMail = new MailMessage();
                myMail.Subject = strSubject;
                foreach (string m in to)
                    myMail.To.Add(new MailAddress(m));
                if (from != "productquestions@improweb.com")
                    myMail.CC.Add(new MailAddress(from));

                //foreach (string adminEmail in strAdminEmails)
                //    myMail.Bcc.Add(new MailAddress(adminEmail));

                if (bcc != null)
                {
                    foreach (string m in bcc)
                        myMail.Bcc.Add(new MailAddress(m));
                }
                string strTitle = "";
                if (includeSignature)
                    strTitle = "Sent From " + strSite;

                myMail.From = new MailAddress(from);
                myMail.IsBodyHtml = true;

                myMail.Body = "<html><head><TITLE>" + strTitle + "</TITLE>" +
                    "<STYLE>BODY {FONT-SIZE: 12px; FONT-FAMILY: Arial, helvetica, sans-serif; " +
                    "SCROLLBAR-BASE-COLOR:#2D5281;}</STYLE></head><BODY>" +
                    "<table WIDTH=100% BORDER=0 cellspacing=0 cellpadding=0><tr><td>" + strHTMLBody +
                    "</td></tr><tr><td><HR></td></tr></table></BODY></html>";
                PdfDocument doc = GetPdf(attachment);
                MemoryStream pdfStream = new MemoryStream();

                doc.Save(pdfStream);
                pdfStream.Position = 0;
                if (type == "Order")
                {
                    myMail.Attachments.Add(new Attachment(pdfStream, "OrderReceipt-" + orderId + ".pdf"));
                }
                else
                {
                    myMail.Attachments.Add(new Attachment(pdfStream, "QuoteNo-" + orderId + ".pdf"));
                }
                SmtpClient client = MailSetup();
                client.Send(myMail);
                pdfStream.Close();
                doc.Close();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex.Message);

            }
        }

        public static void sendMail(string strSubject, string strHTMLBody, MailAddress[] to, MailAddress from, MailAddress[] bcc, Attachment[] attachments, bool includeSignature)
        {
            StringBuilder sbErr = new StringBuilder();
            try
            {

                string strMailLogo = StaticConfig.GetValue<string>("MailLogo");
                OrgWebDetail arrOrg = GetOrgWebDetail();
                string orgURL = arrOrg.WEBOrgURL.ToString();

                MailMessage myMail = new MailMessage();
                myMail.Subject = strSubject;

#if DEBUG
                foreach (MailAddress a in to)
                    myMail.To.Add(new MailAddress("4me.suren@gmail.com", a.DisplayName));
#else
                foreach (MailAddress a in to)
                    myMail.To.Add(a);

                
                if (bcc != null)
                    foreach (MailAddress a in bcc)
                        myMail.Bcc.Add(a);
#endif
                if (attachments != null)
                    foreach (Attachment a in attachments)
                        myMail.Attachments.Add(a);

                string css = "body { margin: 0px; } " +
                   "td { font-size: 10px; vertical-align: top; font-family: Arial, Serif; text-align: left; } " +
                   ".style1 { font-weight: bold; font-size: 12px; font-family: Arial, Serif; } " +
                   ".style2 { font-size: 10px; font-family: Arial, Serif; } .style3 { font-size: 10px; font-family: Arial, Serif; } " +
                   ".style4 { font-weight: bold; font-size: 10px; font-family: Arial, Serif; } " +
                   ".style5 { font-weight: bold; font-size: 11px; } .style6 { font-size: 12px; font-family: Arial, Serif; };";

                string signature = "";
                if (includeSignature)
                {
                    signature = "<tr><td><br/><br/>Thank you for visiting <a class=style3 href='http://esquire.co.za' target=\"_new\">www.esquire.co.za</a>" +
                        "</td></tr><tr><td><img src='" + strMailLogo + "'></td></tr>";
                }

                myMail.From = from;
                myMail.IsBodyHtml = true;
                sbErr.Append("Build Body of EMail\r\n");
                myMail.Body = string.Format(@"<html><head><TITLE></TITLE><style>{0}</style>
                    </head><BODY><div style=""PADDING-LEFT: 20px; PADDING-TOP: 20px"">
                    <table WIDTH=""100%"" BORDER=""0"" cellspacing=""0"" cellpadding=0><tr><td>{1}
                    </td></tr>{2}</table></div></BODY></html>",
                    css, strHTMLBody, signature);
                sbErr.Append("Setup Mail Server\r\n");
                SmtpClient client = MailSetup();
                sbErr.Append("Send Mail\r\n");
                client.Send(myMail);
                return;
            }
            catch
            {
                //DebugMe(1, "Error in sendMail on Shared at " + sbErr.ToString() + " \r\nstrSubject = " + strSubject + 
                //"\r\nstrHTMLBody = " + strHTMLBody + "\r\nto = " + to.Length + " is " + to[to.Length-1].ToString() + 
                //"\r\nfrom = " + from.ToString() , Excp);
            }
        }


        public static PdfDocument GetPdf(string html)
        {
            HtmlToPdf converter = new HtmlToPdf();
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
            PdfDocument doc = converter.ConvertHtmlString(html);
            return doc;


        }

        public static SmtpClient MailSetup()
        {

            SmtpClient client = new()
            {
                Port = Convert.ToInt32(StaticConfig["SMTPSetting:Port"]),
                Host = StaticConfig["SMTPSetting:Host"],
                EnableSsl = Convert.ToBoolean(StaticConfig["SMTPSetting:EnableSsl"]),
                Timeout = Convert.ToInt32(StaticConfig["SMTPSetting:Timeout"]),
                Credentials = new System.Net.NetworkCredential(StaticConfig["SMTPSetting:Email"], StaticConfig["SMTPSetting:Password"]),
                UseDefaultCredentials = Convert.ToBoolean(StaticConfig["SMTPSetting:UseDefaultCredentials"])
            };
            return client;
        }

        internal static string GetCustomerAccountNo(long customerID)
        {
            string Query = "Select Accounts.AccountNo from WEBCustomer JOIN Accounts on WebCustomer.AccountID=Accounts.AccountID Where WEBCustomer.CustID=" + customerID;
            string AccountId = "";
            using (var db = new SqlConnection(connString))
            {
                AccountId = db.Query<string>(Query).FirstOrDefault();
            }
            return AccountId;
        }

        public static async Task<BillingDetail> GetBillingDetail(string ConnectID, long CustID, string FinconServerUsername, string FinconServerPassword)
        {
            BillingDetail bDetail = new BillingDetail();
            try
            {
                string AccountNumber = GetAccountNumber(CustID);
                string FinconUrl = GetWebConfigKeyValue("FinconUrl");

                string SalesOrderLink = FinconUrl + "\"GetDebAccounts\"" + "/" + ConnectID + "/" + AccountNumber + "/" + AccountNumber + "/0/1";
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(FinconServerUsername + ":" + FinconServerPassword)));

                    var response = await client.GetAsync(SalesOrderLink).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    dynamic obj = JObject.Parse(responseBody);
                    string Terms = (string)obj.result[0].Accounts[0].TermCode;
                    //string CreditAvailable = (string)obj.result[0].Accounts[0].CurrentBalance;
                    string CreditAvailable = "Not entered";
                    bDetail.Terms = Shared.GetTerms(Terms);
                    bDetail.CreditAvailable = CreditAvailable;
                }
            }
            catch (Exception ex)
            {

            }
            return bDetail;
        }

        public static string GetAccountNumber(long? custId)
        {
            string sqlQuery = "Select a.AccountNo From Accounts a Join WEBCustomer c on a.AccountID=c.AccountID Where c.CustID=@CustId and a.OrgId IN (94,380,932,546,473)";
            string accountNo = "";
            long orgId = GetOrgID();
            using (var db = new SqlConnection(connString))
            {
                var values = new { CustId = custId, OrgId = orgId };
                accountNo = db.Query<string>(sqlQuery, values).FirstOrDefault();
            }
            return accountNo;
        }

        internal static string GetTerms(string terms)
        {
            string query = "Select Code+' - '+Description from Terms Where Code='" + terms + "'";
            string returnTerm = "";
            using (var db = new SqlConnection(connString))
            {
                returnTerm = db.Query<string>(query).FirstOrDefault();
            }
            return returnTerm;
        }

        public static Order GetOrder(long id)
        {
            string query = "SELECT [OrderID],[CustID],[OrderDate],[DeliveryMethod],[DeliveryDescID],[DeliveryCost],[PayID],[StatusID],[Notes],[ShippingID],[OrgID],[OrgBranchID],[Insurance],[DeliveryQuoteID],[DistOrdStatus],[ReviewEmailSent],[DeliveryWaybillID],[CustRef],[DiscountRefCode],[Discount],[DeliveryID],[FinconId],[ShippingInstruction] FROM [dbo].[WEBOrders] WHERE [OrderID]=@OrderId";
            Order order = new();
            using (var db = new SqlConnection(connString))
            {
                var values = new { OrderId = id };
                order = db.Query<Order>(query, values).FirstOrDefault();
            }
            return order;
        }

        public static List<OrderItem> GetOrderItems(long id)
        {
            OrgWebDetail detail = GetOrgWebDetail();
            string strWEBStockOnly = detail.WEBStockOnly.ToString();
            string query = "SELECT wO.[ItemID],wO.[OrderID],wO.[ProdID],wO.[ProdQty],wO.[Price],wO.[ProdDesc],wO.[ProdCode],dbo.GetProductStockCount(p.ProdID, p.Status, 'A') as StockCount,p.ImgURL as Image FROM [dbo].[WEBOrderItems] wO JOIN [dbo].[Products] p on wO.ProdID=p.ProdID WHERE OrderID=@OrderId";
            List<OrderItem> items = new();
            using (var db = new SqlConnection(connString))
            {
                var values = new { OrderId = id };
                items = db.Query<OrderItem>(query, values).ToList();
            }
            return items;
        }

        public static DeliveryAddress GetDeliveryAddress(long ShippingID)
        {
            string query = "SELECT [ShippingID],[CustID],[ShippingDesc],[ShippingAddress],[ShippingCountry],[ShippingType],[ShippingAddressIEID] as ShippingddressIEID,[CourierDirectKey],[Town],[Phone],[PostalCode],[Name],[Email] FROM [dbo].[WEBCustShipping] where ShippingID=@ShippingID";
            DeliveryAddress addresses = new();
            using (var db = new SqlConnection(connString))
            {
                addresses = db.Query<DeliveryAddress>(query, new { ShippingID }).FirstOrDefault();
            }
            return addresses;
        }

        public static string GetOrderDetails(long orderId, string? accountNo)
        {
            string strOut = "";
            if (!string.IsNullOrEmpty(accountNo))
            {
                Order orderDetail = GetOrder(orderId);
                if (orderDetail == null)
                {
                    strOut = "There is a security problem with this page! Your IP address has been saved!";
                    return strOut;
                }
                if (orderDetail.DeliveryMethod == "Collect from shop")
                {
                    strOut = "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Document</title><style>*{box-sizing:border-box;padding:0;margin:0;font-style:normal;scroll-behavior:smooth}body{background:#ebf0f9;padding:1rem;display:flex;flex-direction:column;gap:1rem;width:100%}.image{display:flex;justify-content:flex-end}.details{display:flex;flex-direction:column;gap:1rem}.head{font-weight:600;width:fit-content;font-size:1.2rem;line-height:1.25rem;color:#077ea2;padding-bottom:.8rem;position:relative}.head::before{content:'';position:absolute;width:50%;height:1px;bottom:0;left:0;border-bottom:4px solid #077ea2;border-radius:15px}.detailflex{display:flex;gap:.5rem}.detailflex .label{font-size:1rem;font-weight:600;color:#077ea2}.detailflex .value{font-size:1rem;font-weight:600;color:#000}.table-container{overflow-x:auto;width:100%}table td,table th{padding:10px;border-bottom:2px solid #077ea2}table tr:last-child>td{border-bottom:none}table th:last-child{border-top-right-radius:.8rem}table th:first-child{border-top-left-radius:.8rem}table th{padding:.5rem;text-align:left;background-color:#077ea2;color:#fff;font-size:.9rem;white-space:nowrap}table{border:2px solid #077ea2;width:100%;border-radius:1rem;border-spacing:0}.total{width:fit-content;border-radius:2rem;display:flex;gap:.5rem;align-items:center;background:#077ea2;padding:.6rem .9rem;color:#fff}.total span{font-size:.8rem}.total h2{font-size:1rem}</style></head><body>";
                }
                else
                {
                    strOut = "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Document</title><style>*{box-sizing:border-box;padding:0;margin:0;font-style:normal;scroll-behavior:smooth}body{background:#ebf0f9;padding:1rem;display:flex;flex-direction:column;gap:1rem;width:100%}.image{display:flex;justify-content:flex-end}.details{display:flex;flex-direction:column;gap:1rem}.head{font-weight:600;width:fit-content;font-size:1.2rem;line-height:1.25rem;color:#077ea2;padding-bottom:.8rem;position:relative}.head::before{content:'';position:absolute;width:50%;height:1px;bottom:0;left:0;border-bottom:4px solid #077ea2;border-radius:15px}.detailflex{display:flex;gap:.5rem}.detailflex .label{font-size:1rem;font-weight:600;color:#077ea2}.detailflex .value{font-size:1rem;font-weight:600;color:#000}.table-container{overflow-x:auto;width:100%}table td,table th{padding:10px;border-bottom:2px solid #077ea2}table tr:last-child>td{border-bottom:none}table th:last-child{border-top-right-radius:.8rem}table th:first-child{border-top-left-radius:.8rem}table th{padding:.5rem;text-align:left;background-color:#077ea2;color:#fff;font-size:.9rem;white-space:nowrap}table{border:2px solid #077ea2;width:100%;border-radius:1rem;border-spacing:0}.total{width:fit-content;border-radius:2rem;display:flex;gap:.5rem;align-items:center;background:#077ea2;padding:.6rem .9rem;color:#fff}.total span{font-size:.8rem}.total h2{font-size:1rem}</style></head><body><div class='image'><img src='https://api.esquire.co.za/Resources/Images/PageImage/courierdirect.png' alt='' style='object-fit:contain'></div>";
                }
                if (orderDetail.DeliveryMethod == "Collect from shop")
                {
                    using (SqlConnection Conn = new SqlConnection(connString))
                    {
                        Conn.Open();
                        string strSQL = @"SELECT WEBCustomer.CustID, WEBCustomer.Title, WEBCustomer.FirstName, WEBCustomer.Surname, 
                        WEBCustomer.Tel, WEBCustomer.Email FROM WEBOrders LEFT OUTER JOIN Accounts INNER JOIN WEBCustomer ON Accounts.AccountID = WEBCustomer.AccountID ON WEBOrders.CustID = WEBCustomer.CustID
                        WHERE (WEBOrders.OrderID = @OrderID);";
                        using (SqlCommand Cmd = new SqlCommand(strSQL, Conn))
                        {
                            Cmd.Parameters.AddWithValue("OrderID", orderId);
                            SqlDataReader reader = Cmd.ExecuteReader();
                            if (reader.HasRows)
                            {
                                OrgWebDetail detail = GetOrgWebDetail();
                                reader.Read();
                                long lngCustID = long.Parse(reader["CustID"].ToString());

                                strOut += string.Format(@"<div class='detailflex' style='border:1px solid #077ea2;padding:10px'><div class='label'>PACKING SLIP FOR REF NO:</div><div class='value'>{0}</div></div><div class='details'><h1 class='head'>Customer Details</h1><div class='detailflex'><div class='label'>Account No :</div><div class='value'>{1}</div></div><div class='detailflex'><div class='label'>Name :</div><div class='value'>{2}</div></div><div class='detailflex'><div class='label'>Contact No :</div><div class='value'>{3}</div></div><div class='detailflex'><div class='label'>Email :</div><div class='value'>{4}</div></div><div class='detailflex'><div class='label'>Shipping :</div><div class='value'>Collect From Shop</div></div></div>",
                                   orderId, accountNo,
                                    reader["Title"].ToString() + " " + reader["FirstName"].ToString() + " " + reader["Surname"].ToString(),
                                    reader["Tel"].ToString(), reader["Email"].ToString());
                                string strShowVat = GetWebConfigKeyValue("ShowVat");
                                bool bHideVat = false;
                                if (strShowVat.ToLower() == "false")
                                    bHideVat = true;
                                strOut += GetOrderDetails(orderId, lngCustID, bHideVat);
                                string strQuery = "Select Products.ProductCode,Products.Length,Products.Width,Products.Height,Products.Mass,WebOrderItems.ProdQty from Products JOIN WebOrderItems on Products.ProdID=WebOrderItems.ProdID Where OrderId=" + orderId;
                                string packaging_details = "<div class='details'><h1 class='head'>Dimensions</h1></div><table><thead><tr><th>PIECES</th><th>DESCRIPTION</th><th>LENGTH (CM)</th><th>BREADTH (CM)</th><th>HEIGHT (CM)</th><th>MASS (KG)</th><th>VOLUME</th></tr></thead><tbody>{packaging_details}</tbody></table></div>";
                                string packaging_details_body = "";
                                using (var db = new SqlConnection(connString))
                                {
                                    List<PackageDetails> details = db.Query<PackageDetails>(strQuery).ToList();
                                    foreach (var item in details)
                                    {
                                        if (item.Length == null || item.Length == 0)
                                        {
                                            item.Length = Convert.ToDecimal(detail.OrgLength);
                                        }
                                        if (item.Width == null || item.Width == 0)
                                        {
                                            item.Width = Convert.ToDecimal(detail.OrgWidth);
                                        }
                                        if (item.Height == null || item.Height == 0)
                                        {
                                            item.Height = Convert.ToDecimal(detail.OrgHeight);
                                        }
                                        double volume = Math.Round(((Convert.ToDouble(item.ProdQty) * Convert.ToDouble(item.Length) * Convert.ToDouble(item.Height) * Convert.ToDouble(item.Width)) / 5000), 2);
                                        packaging_details_body += "<tr><td>" + item.ProdQty + "</td><td>Code = " + item.ProductCode + "</td><td>" + item.Length + "</td><td>" + item.Width + "</td><td>" + item.Height + "</td><td>" + item.Mass + "</td><td>" + volume + "</td></tr>";
                                    }
                                }
                                var bottom_contents = packaging_details.Replace("{packaging_details}", packaging_details_body);
                                strOut += bottom_contents + "</body></html>";
                            }
                            else
                            {
                                strOut = "There is a security problem with this page! Your IP address has been saved!";
                            }
                            Cmd.Dispose();
                        }
                        Conn.Dispose();
                    }
                }
                else
                {
                    using (SqlConnection Conn = new SqlConnection(connString))
                    {
                        Conn.Open();
                        string strSQL = @"SELECT WEBCustomer.CustID, WEBCustomer.Title, WEBCustomer.FirstName, WEBCustomer.Surname, 
                        WEBCustShipping.ShippingAddress, WEBCustShipping.ShippingCountry, WEBCustomer.Tel, WEBCustomer.Email
                        FROM WEBCustShipping INNER JOIN
                            WEBOrders ON WEBCustShipping.ShippingID = WEBOrders.ShippingID LEFT OUTER JOIN Accounts INNER JOIN
                            WEBCustomer ON Accounts.AccountID = WEBCustomer.AccountID ON WEBOrders.CustID = WEBCustomer.CustID
                        WHERE (WEBOrders.OrderID = @OrderID);";
                        using (SqlCommand Cmd = new SqlCommand(strSQL, Conn))
                        {
                            Cmd.Parameters.AddWithValue("OrderID", orderId);
                            SqlDataReader reader = Cmd.ExecuteReader();
                            if (reader.HasRows)
                            {
                                reader.Read();
                                long lngCustID = long.Parse(reader["CustID"].ToString());

                                strOut += string.Format(@"<div class='detailflex' style='border:1px solid #077ea2;padding:10px'><div class='label'>PACKING SLIP FOR REF NO:</div><div class='value'>{0}</div></div><div class='details'><h1 class='head'>Customer Details</h1><div class='detailflex'><div class='label'>Account No :</div><div class='value'>{1}</div></div><div class='detailflex'><div class='label'>Name :</div><div class='value'>{2}</div></div><div class='detailflex'><div class='label'>Contact No :</div><div class='value'>{3}</div></div><div class='detailflex'><div class='label'>Email :</div><div class='value'>{4}</div></div><div class='detailflex'><div class='label'>Shipping :</div><div class='value'>{5}</div></div></div>",
                                    orderId.ToString(), accountNo,
                                    reader["Title"].ToString() + " " + reader["FirstName"].ToString() + " " + reader["Surname"].ToString(),
                                    reader["Tel"].ToString(), reader["Email"].ToString(),
                                    reader["ShippingAddress"].ToString() + "<br/>" + reader["ShippingCountry"].ToString());
                                string strShowVat = GetWebConfigKeyValue("ShowVat");
                                bool bHideVat = false;
                                if (strShowVat.ToLower() == "false")
                                    bHideVat = true;
                                strOut += GetOrderDetails(orderId, lngCustID, bHideVat);
                                string strQuery = "Select Products.ProductCode,Products.Length,Products.Width,Products.Height,Products.Mass,WebOrderItems.ProdQty from Products JOIN WebOrderItems on Products.ProdID=WebOrderItems.ProdID Where OrderId=" + orderId;
                                string packaging_details = "<div class='details'><h1 class='head'>Dimensions</h1></div><table><thead><tr><th>PIECES</th><th>DESCRIPTION</th><th>LENGTH (CM)</th><th>BREADTH (CM)</th><th>HEIGHT (CM)</th><th>MASS (KG)</th><th>VOLUME</th></tr></thead><tbody>{packaging_details}</tbody></table></div>";
                                string packaging_details_body = "";
                                using (var db = new SqlConnection(connString))
                                {
                                    List<PackageDetails> details = db.Query<PackageDetails>(strQuery).ToList();
                                    foreach (var item in details)
                                    {
                                        double volume = Math.Round(((Convert.ToDouble(item.ProdQty) * Convert.ToDouble(item.Length) * Convert.ToDouble(item.Height) * Convert.ToDouble(item.Width)) / 5000), 2);
                                        packaging_details_body += "<tr><td>" + item.ProdQty + "</td><td>Code = " + item.ProductCode + "</td><td>" + item.Length + "</td><td>" + item.Width + "</td><td>" + item.Height + "</td><td>" + item.Mass + "</td><td>" + volume + "</td></tr>";
                                    }
                                }
                                var bottom_contents = packaging_details.Replace("{packaging_details}", packaging_details_body);
                                strOut += bottom_contents + "</body></html>";
                            }
                            else
                            {
                                strOut = "There is a security problem with this page! Your IP address has been saved!";
                            }
                            Cmd.Dispose();
                        }
                        Conn.Dispose();
                    }
                }
            }
            return strOut;
        }

        public static string GetOrderDetails(long orderId, long custId, bool incVAT)
        {
            Order orders = GetOrder(orderId);

            List<OrderItem> orderItems = GetOrderItems(orderId);
            VatData vd = VAT;
            OrgWebDetail companyDetail = GetOrgWebDetail();
            StringBuilder sb = new StringBuilder();

            sb.Append("<div class='table-container'><table>");
            if (orderItems.Count > 0)
            {
                if (custId == orders.CustID)
                {
                    sb.Append("<tr><th>STK#</th><th>DESCRIPTION</th><th>PRICE EXCL.</th><th>QTY</th><th>TOTAL EXCL.</th><th>TOTAL INCL.</th></tr>");

                    double total = 0.0;
                    string strShowVat = Shared.GetWebConfigKeyValue("ShowVat").ToLower();
                    foreach (var r in orderItems)
                    {
                        double cost = r.Price * (double)r.ProdQty;
                        double price = r.Price;
                        double totalexclusive = (r.Price * (double)r.ProdQty) / 1.15;
                        double rate = r.Price / 1.15;
                        if (strShowVat == "true")
                        {
                            cost = cost * vd.OneDotVAT;
                            price = price * vd.OneDotVAT;
                        }
                        total += cost;
                        sb.Append("<tr><td style='white-space:nowrap'>" + r.ProdCode + "</td>");
                        sb.Append("<td>" + r.ProdDesc + "</td>");
                        sb.Append(string.Format(@"<td style='white-space:nowrap'>{0}</td><td style='white-space:nowrap'>{1}</td><td style='white-space:nowrap'>{2}</td><td style='white-space:nowrap'>{3}</td></tr>",
                        rate.ToString(CurrencyFormat),
                        r.ProdQty.ToString(),
                        totalexclusive.ToString(CurrencyFormat),
                        cost.ToString(CurrencyFormat)));
                    }

                    double dblTotalWithShipping = total + (Convert.ToDouble(orders.DeliveryCost));
                    if (orders.DeliveryDescID.ToString() == CD_DESC_ID)
                    {
                        orders.DeliveryMethod = orders.DeliveryMethod + " Waybill No: " + Shared.CD_WAYBILL_PREFIX + orders.OrderID;
                        sb.Append("<tr><td colspan='5'><span style='font-weight:600'>Total (Excl. delivery)</span></td><td><span style='font-weight:600'>" + total.ToString(CurrencyFormat) + "</span></td></tr>");

                        sb.Append("<tr><td colspan='5'><span style='font-weight:600'>Courier Direct</span></td><td><span style='font-weight:600'>R " + String.Format(Convert.ToString(orders.DeliveryCost), CurrencyFormat) + "</span></td></tr>");
                        sb.Append("<tr><td colspan='5'><span style='font-weight:600'>Total (Incl. delivery)</span></td><td><span style='font-weight:600'>" + dblTotalWithShipping.ToString(CurrencyFormat) + "</span></td></tr>");
                        sb.Append("<tr><td colspan='6'><div style='display:flex;justify-content:flex-end'><div class='total'><span>Grand Total :</span><h2>" + (dblTotalWithShipping).ToString(CurrencyFormat) + "</h2><span>(Incl. 15% VAT)</span></div></div></td></tr>");
                    }
                    else
                    {
                        sb.Append("<tr><td colspan='6'><div style='display:flex;justify-content:flex-end'><div class='total'><span>Grand Total :</span><h2>" + (total).ToString(CurrencyFormat) + "</h2><span>(Incl. 15% VAT)</span></div></div></td></tr>");
                    }
                }
                else
                {
                    sb.Append("<tr><td>Unauthorized access</td></tr>");
                }
            }
            else
                sb.Append("<tr><td>No details for this order were found</td></tr>");
            sb.Append("</table></div>");

            return sb.ToString();
        }

        internal static LoginDetails AdminLogin(string username, string password)
        {
            LoginDetails returnValue = new()
            {
                CustID = INVALID_LOGIN,
                Email = username,
                IsLoggedIn = false
            };
            try
            {
                string strSql = @"SELECT OrgID, UserID, AccountID='NULL', Password, 
                    FirstName, Surname, Active,UsePrice=1,DefaultBranch=null FROM Users WHERE (EMailAddress = N'" +
                        username.Replace("\'", "\'\'") + "') AND (Password='" + password.Replace("\'", "\'\'") + "') AND OrgID IN (94,380,932,546)";
                using (var connection = new SqlConnection(connString))
                {
                    var result = connection.QueryMultiple(strSql);
                    var uDetail = result.Read().FirstOrDefault();
                    if (uDetail != null)
                    {
                        returnValue.CustID = Convert.ToString(uDetail.UserID);
                        returnValue.UserName = Convert.ToString(uDetail.FirstName) + " " + Convert.ToString(uDetail.Surname);
                        returnValue.UsePrice = Convert.ToInt32(uDetail.UsePrice);
                        returnValue.IsLoggedIn = true;
                        returnValue.FirstName = Convert.ToString(uDetail.FirstName);
                        returnValue.Surname = Convert.ToString(uDetail.Surname);
                        returnValue.AccountID = Convert.ToString(uDetail.AccountID);
                        returnValue.DefaultBranch = Convert.ToString(uDetail.DefaultBranch);
                    }
                }
            }
            catch (Exception ex)
            {
                returnValue.CustID = INVALID_LOGIN;
            }
            return returnValue;
        }

        public static VatData VAT
        {
            get
            {
                VatData vd = new VatData();
                SqlConnection Conn = new SqlConnection(connString);
                Conn.Open();

                string strSQL = "SELECT OrgVATPercentage FROM Organisation WHERE (OrgID = " + GetOrgID() + ");";
                SqlCommand Cmd = new SqlCommand(strSQL, Conn);
                SqlDataReader Reader = Cmd.ExecuteReader();
                if (!Reader.HasRows)
                {
                    vd.OneDotVAT = 1.14;
                    vd.VAT = 0.14;
                    vd.strOneDotVAT = "1.14";
                    vd.strVAT = "0.14";
                }
                else
                {
                    Reader.Read();
                    double dblVat = double.Parse(Reader["OrgVATPercentage"].ToString());
                    double dblOneDotVat = 1.00 + dblVat;
                    vd.VAT = dblVat;
                    vd.OneDotVAT = dblOneDotVat;
                    vd.strVAT = dblVat.ToString();
                    vd.strOneDotVAT = dblOneDotVat.ToString();
                }
                Reader.Dispose();
                Cmd.Dispose();
                Conn.Dispose();
                return vd;
            }
        }

        public static string GetOrgName()
        {
            string returnValue = "";
            try
            {

                returnValue = StaticConfig.GetValue<string>("OrgName");
            }
            catch
            {
            }
            return returnValue;
        }

        public static LoginDetails Login(string username, string password)
        {
            LoginDetails returnValue = new()
            {
                CustID = INVALID_LOGIN,
                Email = username,
                IsLoggedIn = false
            };

            try
            {
                using (var db = new SqlConnection(connString))
                {
                    string strSQL = @"SELECT Top 1 WEBCustomer.OrgID, WEBCustomer.CustID, WEBCustomer.AccountID, WEBCustomer.Password, 
                    WEBCustomer.FirstName, WEBCustomer.Surname, Accounts.Active, Accounts.UsePrice,Accounts.DefaultBranch FROM WEBCustomer 
                    INNER JOIN Accounts ON WEBCustomer.AccountID = Accounts.AccountID WHERE (WEBCustomer.Email = N'" +
                        username.Replace("\'", "\'\'") + "') AND (WEbCustomer.Password='" + password.Replace("\'", "\'\'") + "') AND Accounts.OrgID IN (94,380,932,546) Order By WebCustomer.CustID" + @";
                    SELECT WEBCustomer.Email, WEBCustomer.FraudulentUserID, Users.FirstName, Users.Surname, Organisation.WEBOrgURL
                    FROM WEBCustomer INNER JOIN Users ON WEBCustomer.FraudulentUserID = Users.UserID INNER JOIN
                        Organisation ON Users.OrgID = Organisation.OrgID
                    WHERE (WEBCustomer.Email = N'" + username.Replace("'", "''") + "') AND (WEbCustomer.Password='" + password + "') AND (WEBCustomer.Active = 1)  AND Users.OrgID IN (94,380,932,546);";

                    var result = db.QueryMultiple(strSQL);
                    var uDetail = result.Read<LoginUDetail>().FirstOrDefault();
                    if (uDetail != null)
                    {
                        if (uDetail.Password == password)
                        {
                            returnValue.CustID = Convert.ToString(uDetail.CustID);
                            returnValue.UserName = Convert.ToString(uDetail.FirstName) + " " + Convert.ToString(uDetail.Surname);
                            returnValue.UsePrice = Convert.ToInt32(uDetail.UsePrice);
                            returnValue.IsLoggedIn = true;
                            returnValue.FirstName = Convert.ToString(uDetail.FirstName);
                            returnValue.Surname = Convert.ToString(uDetail.Surname);
                            returnValue.AccountID = Convert.ToString(uDetail.AccountID);
                            returnValue.DefaultBranch = Convert.ToString(uDetail.DefaultBranch);
                        }
                        else
                        {
                            returnValue.IsLoggedIn = false;
                            returnValue.CustID = "Password_Mismatch";
                        }
                    }

                    var fradulent_check = result.Read().FirstOrDefault();
                    if (fradulent_check != null)
                    {
                        returnValue.CustID = ACCOUNT_FRAUDULENT;
                        returnValue.IsLoggedIn = false;
                    }
                }

            }
            catch
            {
                returnValue.CustID = INVALID_LOGIN;
            }

            return returnValue;
        }

        public static string getAdminEMail()
        {

            return (string)StaticConfig.GetValue<string>("AdminEmail");
        }

        public static void sendLoginMail(string strSubject, string strHTMLBody, string strTo, string strFrom, string strBcc, bool bIncludeFooter)
        {
            try
            {

                MailMessage myMail = new MailMessage();
                myMail.Subject = strSubject;
                if (strTo.IndexOf(";") > 2)
                {
                    string[] arrTo = strTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string email in arrTo)
                        myMail.To.Add(new MailAddress(email));
                }
                else
                {
                    myMail.To.Add(new MailAddress(strTo));
                }

                if (!string.IsNullOrEmpty(strBcc) && isEmailValid(strBcc))
                {
                    if (strBcc.IndexOf(";") > 2)
                    {
                        string[] arrBcc = strBcc.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string email in arrBcc)
                            myMail.Bcc.Add(new MailAddress(email));
                    }
                    else
                    {
                        myMail.Bcc.Add(new MailAddress(strBcc));
                    }
                    //todo: Remove
                    myMail.Bcc.Add(new MailAddress("jana@esquire.co.za"));
                }
                else
                {
                    //todo: Remove
                    myMail.Bcc.Add(new MailAddress("jana@esquire.co.za"));
                }
                myMail.From = new MailAddress(strFrom);
                myMail.IsBodyHtml = true;
                myMail.Body = "<HTML><HEAD><TITLE>Sent From " + GetOrgName() + "</TITLE>" +
                    "<STYLE>BODY { FONT-FAMILY: 'Arial, Helvetica, sans-serif' } TD	{" +
                    "font-size:xx-small; vertical-align:top;}</STYLE></HEAD><BODY>" +
                    "<TABLE WIDTH=100% BORDER=0 CELLSPACING=0 CELLPADDING=0><TR><TD>" + strHTMLBody +
                    "</TD></TR><TR><TD><HR><FONT size=-2>This response e-mail is generated by the " + GetOrgName() + " " +
                    "software and is protected by the laws of the Republic of South Africa. This " +
                    "response e-mail may contain confidential information. If you are not the intended" +
                    " recipient hereof, kindly delete this document and immediately inform " + GetOrgName() + ", it " +
                    "would be much appreciated. Please note that copying, disseminating or taking any " +
                    "action based on the information contained in this e-mail by any person or persons " +
                    "other than the intended recipient is unlawful and/or wrongful. " + GetOrgName() + ", it's service " +
                    "providers or clients will not be liable for any damages whatsoever arising out of the use or " +
                    "inability to use the " + GetOrgName() + " software or services including, but not limited to: " +
                    "damages for loss of business, interruption in business, loss of business information " +
                    "and/or any other direct, indirect or consequential loss.</FONT></TD></TR></TABLE></BODY></HTML>";
                SmtpClient client = MailSetup();
                client.Send(myMail);
                return;
            }
            catch (Exception Excp)
            {
                //DebugMe("Error in sendMail strTo: " + strTo + " \r\nstrFrom: " + strFrom + " \r\nSubject: " + strSubject, Excp, 1);
            }
        }
        public static bool isEmailValid(string strEMail)
        {
            return Regex.IsMatch(strEMail, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public static void RecordLogin(string CustId)
        {
            using (var db = new SqlConnection(connString))
            {
                string strSQL = @"INSERT INTO WEBDistLogin
                        (CustID, LoginTime)
                        VALUES     (@CustID,@LoginTime)";
                var values = new { CustID = CustId, LoginTime = DateTime.Now };
                db.Execute(strSQL, values);
            }
        }

        public static List<DeliveryAddress> GetDeliveryAddresses(long CustID)
        {
            string query = "SELECT [ShippingID],[CustID],[ShippingDesc],[ShippingAddress],[ShippingCountry],[ShippingType],[ShippingAddressIEID] as ShippingddressIEID,[CourierDirectKey],[Town],[Phone],[PostalCode],[Name],[Email] FROM [dbo].[WEBCustShipping] where CustID=@CustID";
            List<DeliveryAddress> addresses = new();
            using (var db = new SqlConnection(connString))
            {
                addresses = db.Query<DeliveryAddress>(query, new { CustID }).ToList();
            }
            return addresses;
        }

        public static string GenerateToken(LoginDetails user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(StaticConfig["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim(ClaimTypes.Role,role),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", user.UserName),
                new Claim("UsePrice", user.UsePrice.ToString())
            };
            if (!string.IsNullOrEmpty(user.CustID))
            {
                var custId = new Claim("CustomerID", Convert.ToString(user.CustID));
                claims.Add(custId);
            }
            if (!string.IsNullOrEmpty(user.AccountID))
            {
                var custId = new Claim("AccountID", Convert.ToString(user.AccountID));
                claims.Add(custId);
            }
            if (!string.IsNullOrWhiteSpace(user.DefaultBranch))
            {
                var branchId = new Claim("DefaultBranch", user.DefaultBranch);
                claims.Add(branchId);
            }
            var token = new JwtSecurityToken(StaticConfig["Jwt:Issuer"],
                StaticConfig["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        public static string GetOrgEmail()
        {
            string returnValue = "";
            try
            {
                returnValue = StaticConfig.GetValue<string>("AdminEmail");
            }
            catch
            {
            }
            return returnValue;
        }

        public static MailAddress splitEMailFrom(string strAddress, string strName)
        {
            MailAddress mOut;
            try
            {
                if (strAddress.IndexOf(";") > 2)
                {
                    string[] arrEMail = strAddress.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    mOut = new MailAddress(arrEMail[0], strName);
                }
                else
                {
                    mOut = new MailAddress(strAddress, strName);
                }
            }
            catch (Exception ex)
            {
                mOut = new MailAddress(strAddress, strName);
                //DebugMe(1, "Error splitting E-Mail in splitEMailFrom " + strAddress + " was!", ex);
            }
            return mOut;
        }

        public static void CheckFraudulent(string email, string idNo, string custName)
        {
            string adminEmail = getAdminEMail();
            if (idNo.Trim() == "" || idNo.ToLower() == "undefined")
                idNo = "NoId";
            string sqlQuery = @"SELECT CustID, FirstName, Surname, Email, IdNo, DateCreated
                        FROM WEBCustomer
                        WHERE(Email = @Email) AND(FraudulentUserID IS NOT NULL) OR
                            (IdNo = @IdNo) AND(FraudulentUserID IS NOT NULL)
                        ORDER BY DateCreated;";
            using (var db = new SqlConnection(connString))
            {
                var values = new { Email = email, IdNo = idNo };
                var result = db.Query(sqlQuery, values).FirstOrDefault();
                if (result != null)
                {
                    string subject = "Possible fraudulent user registration";
                    string body = string.Format("A user, {0}, has just registered on your website. The ID/passport no. and/or email address given have possibly been involved " +
                        "in fraudulent transactions in the past.<br />" +
                        "Email address: {1}<br />", custName, email);
                    if (idNo.Trim() != "NoId")
                        body += string.Format("ID no: {0}<br />", idNo);
                    MailAddress from = new(adminEmail);
                    List<MailAddress> to = new();
                    string[] addresses = GetOrgWebDetail().WEBEMailInfo.Split([";"], StringSplitOptions.RemoveEmptyEntries);
                    foreach (string address in addresses)
                        to.Add(new MailAddress(address));
                    sendMail(subject, body, to.ToArray(), from, null, false);
                }
            }
        }

        public static void CheckFraudulentPassword(string strPassword, string custName)
        {

            var sqlQuery = @"SELECT WEBCustomer.DateCreated, WEBCustomer.Password, Users.FirstName, Users.Surname, Organisation.WEBOrgURL
                        FROM WEBCustomer INNER JOIN Users ON WEBCustomer.FraudulentUserID = Users.UserID INNER JOIN Organisation ON Users.OrgID = Organisation.OrgID
                        WHERE (WEBCustomer.Password = @Password) ORDER BY WEBCustomer.DateCreated DESC;";
            using (var db = new SqlConnection(connString))
            {
                var values = new { Password = strPassword };
                var result = db.Query(sqlQuery, values).FirstOrDefault();
                if (result != null)
                {
                    string strBy = result.FirstName.ToString() + " " + result.Surname.ToString() + " from " + result.WEBOrgURL.ToString();
                    string subject = "Possible fraudulent user registration from password!";
                    string body = string.Format("A user, {0}, has just registered on your website. The password given has possibly been involved " +
                        "in fraudulent transactions in the past.<br />" +
                        "On our systems we have noted that fraudsters keep the same passwords on many sites even though they change " +
                        "e-mail address, telephone numbers, physical address details, id numbers and everything else.<br />" +
                        "I guess it gets tricky to remember all these details all the time.<br />The password used was <strong>{1}</strong>" +
                        "<br />It was reported by {2}<br /><br />Kind Regards, Henno", custName, strPassword, strBy);
                    MailAddress from = new(getAdminEMail());
                    List<MailAddress> to = [];
                    string[] addresses = GetOrgWebDetail().WEBEMailInfo.Split([";"], StringSplitOptions.RemoveEmptyEntries);
                    foreach (string address in addresses)
                    {
                        to.Add(new MailAddress(address));
                    }
                    sendMail(subject, body, to.ToArray(), from, null, false);

                }
            }
        }

        internal static DeliveryAddress SaveDeliveryAddress(DeliveryAddress deliveryaddress)
        {
            string query = "INSERT INTO [dbo].[WEBCustShipping] ([CustID],[ShippingDesc],[ShippingAddress],[ShippingCountry],[ShippingType],[ShippingAddressIEID],[CourierDirectKey],[Town],[Phone],[PostalCode],[Name],[Email]) OUTPUT Inserted.ShippingID VALUES (@CustID,@ShippingDesc,@ShippingAddress,@ShippingCountry,@ShippingType,@ShippingAddressIEID,@CourierDirectKey,@Town,@Phone,@PostalCode,@Name,@Email)";
            DeliveryAddress dAddress = new();
            using (var db = new SqlConnection(connString))
            {
                var dAddressId = db.Query<long>(query, new { deliveryaddress.CustID, deliveryaddress.ShippingDesc, deliveryaddress.ShippingAddress, deliveryaddress.ShippingCountry, deliveryaddress.ShippingType, ShippingAddressIEID = deliveryaddress.ShippingddressIEID, deliveryaddress.CourierDirectKey, deliveryaddress.Town, deliveryaddress.Phone, deliveryaddress.PostalCode, deliveryaddress.Name, deliveryaddress.Email }).FirstOrDefault();
                dAddress = GetDeliveryAddress(dAddressId);
            }
            return dAddress;
        }

        public static MailAddress[] splitEMailTo(string strAddressesToSplit, string strName)
        {
            List<MailAddress> mList = [];
            try
            {
                if (strAddressesToSplit.IndexOf(";") > 1)
                {
                    string[] arrEMail = strAddressesToSplit.Split([';'], StringSplitOptions.RemoveEmptyEntries);
                    foreach (string email in arrEMail)
                        mList.Add(new MailAddress(email, strName));
                }
                else
                {
                    mList.Add(new MailAddress(strAddressesToSplit, strName));
                }
            }
            catch
            {
            }
            return [.. mList];
        }

        internal static List<WishList> GetCustomerWishList(long id)
        {
            string strQuery = "SELECT wL.*,p.ProductName,p.ProductCode,p.ImgURL,p.GroupName AS SubCategory,(Select ISNULL(avg(ProdRevRating),0) From ReviewProduct rp where rp.ProdID=p.ProdID) as Rating FROM [dbo].[WishList] wL join Products p on wL.ProdID=p.ProdID WHERE wL.CustID=" + id;
            List<WishList> wishes = [];
            using (var db = new SqlConnection(connString))
            {
                wishes = [.. db.Query<WishList>(strQuery)];
            }
            List<WishList> returnList = [];
            foreach (var item in wishes)
            {
                Product product = GetProduct(item.ProdID);
                product.BrancStocks = GetProductStockCount(item.ProdID);
                item.Product = product;
                returnList.Add(item);
            }
            return returnList;
        }

        internal static WishList SaveWishList(WishList wishList)
        {
            string strQuery = "INSERT INTO [dbo].[WishList] ([CustID],[ProdID],[CreationDate],[Price],[Stock]) OUTPUT INSERTED.WishID VALUES (@CustID,@ProdID,@CreationDate,@Price,@Stock)";
            using (var db = new SqlConnection(connString))
            {
                long wId = db.Query<long>(strQuery, wishList).FirstOrDefault();
                wishList.WishID = wId;
            }
            return wishList;
        }

        internal static WishList GetWishListDetail(long id)
        {
            string strQuery = "SELECT * FROM [dbo].[WishList] WHERE WishID=" + id;
            WishList wishes = new();
            using (var db = new SqlConnection(connString))
            {
                wishes = db.Query<WishList>(strQuery).FirstOrDefault();
            }
            return wishes;
        }

        internal static bool DeleteWishList(long id)
        {
            string strQuery = "DELETE FROM [dbo].[WishList] WHERE WishID=" + id;
            WishList wishes = new();
            using (var db = new SqlConnection(connString))
            {
                int success = db.Execute(strQuery);
                if (success > 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal static List<HomepageSetup> GetHomepageSetups()
        {
            string query = "Select * From HomePageSetup Where OrgId=" + GetOrgID() + " Order By Position";
            List<HomepageSetup> homepageSetups = [];
            using (var db = new SqlConnection(connString))
            {
                homepageSetups = [.. db.Query<HomepageSetup>(query)];
            }
            return homepageSetups;
        }

        internal static List<Banner> GetBanners(List<long> list)
        {
            string query = "Select * from Banners Where Id In (" + string.Join(',', list) + ")";
            List<Banner> banners = [];
            using (var db = new SqlConnection(connString))
            {
                banners = [.. db.Query<Banner>(query)];
            }
            return banners;
        }

        //Order Section
        public static List<Order> GetCustomerOrders(long CustID)
        {
            string query = "SELECT [OrderID],[CustID],[OrderDate],[DeliveryMethod],[DeliveryDescID],[DeliveryCost],[PayID],[StatusID],[Notes],[ShippingID],[OrgID],[OrgBranchID],[Insurance],[DeliveryQuoteID],[DistOrdStatus],[ReviewEmailSent],[DeliveryWaybillID],[CustRef],[DiscountRefCode],[Discount],[DeliveryID],[FinconId],[ShippingInstruction] FROM [dbo].[WEBOrders] Where CustID=@CustID and OrgID IN (94,380,932,546) ORDER BY OrderDate";
            List<Order> orders = [];
            using (var db = new SqlConnection(connString))
            {
                orders = [.. db.Query<Order>(query, new { CustID })];
            }
            return orders;
        }

        internal static List<OrderTracking> GetOrderTracking(long orderId)
        {
            List<OrderTracking> trakings = new();
            string QueryStr = "Select wosc.NewStatusID as Id,woS.Status,wosc.ChangeDateTime From WEBOrderStatusChange woSC join WEBOrderStatus woS on woSC.NewStatusID=wos.StatusID Where wosc.OrderID=@OrderId order by woSC.ChangeDateTime Desc";
            using (var db = new SqlConnection(connString))
            {
                var values = new { OrderId = orderId };
                trakings = db.Query<OrderTracking>(QueryStr, values).ToList();
            }
            return trakings;
        }

        internal static List<PaymentMethod> GetPaymentMethod()
        {
            string query = "Select * From WEBPayMethod";
            List<PaymentMethod> paymentMethods = new();
            using (var db = new SqlConnection(connString))
            {
                paymentMethods = db.Query<PaymentMethod>(query).ToList();
            }

            return paymentMethods;
        }

        internal static List<OrderStatus> GetOrderStaus()
        {

            string query = "Select StatusID,Status From WebOrderStatus";
            List<OrderStatus> orderStatus = new();
            using (var db = new SqlConnection(connString))
            {
                orderStatus = db.Query<OrderStatus>(query).ToList();
            }

            return orderStatus;
        }

        //popup
        internal static IEnumerable<PopupMessage> GetPopup()
        {
            string query = "Select * From PopupMessage";
            List<PopupMessage> pMessage = new();
            using (var db = new SqlConnection(connString))
            {
                pMessage = db.Query<PopupMessage>(query).ToList();
            }
            return pMessage;
        }

        internal static PopupMessage GetPopupDetail(int id)
        {
            string query = "Select Top 1 * From PopupMessage Where Id=" + id;
            PopupMessage pMessage = new();
            using (var db = new SqlConnection(connString))
            {
                pMessage = db.Query<PopupMessage>(query).FirstOrDefault();
            }
            return pMessage;
        }

        internal static PopupMessage GetPopupFor(string pFor)
        {
            string query = "Select Top 1 * From PopupMessage Where PopupFor='" + pFor + "'";
            PopupMessage pMessage = new();
            using (var db = new SqlConnection(connString))
            {
                pMessage = db.Query<PopupMessage>(query).FirstOrDefault();
            }
            return pMessage;
        }

        internal static double GetMargin()
        {
            string query = "Select Margin From Organisation  Where OrgId=" + GetOrgID();
            double margin = 0;
            using (var db = new SqlConnection(connString))
            {
                margin = (db.Query<double?>(query).FirstOrDefault() ?? 0);
            }
            margin = 1 + (margin / 100);
            return margin;
        }

        internal static List<Product_View> GetDealsOfTheDay()
        {
            Pricing prices = GetPriceUsed(null);
            double margin = GetMargin();
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string query = "Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + ") as SpecialPrice,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,p.Active,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as Stock From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and spp.StartDate<=GETDATE() and spp.EndDate>=(SELECT DATEADD(day, 1, GETDATE()))";
            List<Product_View> products = [];
            using (var db = new SqlConnection(connString))
            {
                products = [.. db.Query<Product_View>(query)];
            }
            return products;
        }

        internal static List<Product_View> GetDealsOfTheDayHomepage()
        {
            Pricing prices = GetPriceUsed(null);
            double margin = GetMargin();
            string strWEBPriceUsed = Val(prices.UsePriceNumber.ToString());
            string query = "With  x as (Select spp.Id,spp.ProductCode,p.PriceExclVat" + strWEBPriceUsed + "*1.15*" + margin + " as OldPrice,(spp.SpecialPrice*1.15*" + margin + ") as SpecialPrice,((p.PriceExclVat" + strWEBPriceUsed + "-spp.SpecialPrice)*1.15*" + margin + ") as Discount,spp.Margin, spp.Date,spp.StartDate,spp.EndDate,spp.PageType,p.ProdID,p.ProductName,p.GroupName as SubCategory,m.ManufacturerName as Brand,p.ImgURL,p.Description,p.Active,([dbo].[GetProductStockCount](p.ProdID,p.Status,N'A')) as Stock From SpecialPageProduct spp left Join Products p on spp.ProductCode=p.ProductCode  join Manufacturers m on p.ManufID=m.ManufID Where p.Active=1 and p.OutputMe=1 and p.OrgID In (94,380,932,546) and spp.StartDate<=GETDATE() and spp.EndDate>=(SELECT DATEADD(day, 1, GETDATE()))) SELECT TOP 5 * FROM X  Order by Discount Desc";
            List<Product_View> products = [];
            using (var db = new SqlConnection(connString))
            {
                products = [.. db.Query<Product_View>(query)];
            }
            return products;
        }
    }
}
