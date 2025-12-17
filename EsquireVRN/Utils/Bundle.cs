using System.Data;
using Microsoft.Data.SqlClient;

namespace EsquireVRN
{
    public class Bundle
    {
        private string connectionString;
        public struct ProductDetails
        {
            public ProductDetails(long id, double qty)
            {
                this.Id = id;
                this.Qty = qty;
            }
            public long Id;
            public double Qty;
        }

        public Bundle(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public bool IsBundle(long prodId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT     Status
                    FROM         Products
                    WHERE     (ProdID = @ProdID)", conn))
                {
                    cmd.Parameters.AddWithValue("ProdID", prodId);
                    conn.Open();
                    object value = cmd.ExecuteScalar();
                    return (value != null && value != DBNull.Value && (short)value == 4);
                }
            }
        }

        private string GetBundleIdQuery(long prodId)
        {
            return @"SELECT     Products_1.ProdID AS ProductID
                    FROM         ProductLink INNER JOIN
                          Products ON ProductLink.ProdID = Products.ProdID INNER JOIN
                        Products AS Products_1 ON ProductLink.LinkToProdID = Products_1.ProdID
                    WHERE     (Products.ProdID = " + prodId + ")";
        }

        public long[] GetBundleContentIds(long prodId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(GetBundleIdQuery(prodId), conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    List<long> toReturn = new List<long>(dt.Rows.Count);
                    foreach (DataRow r in dt.Rows)
                    {
                        toReturn.Add((long)r["ProductID"]);
                    }
                    return toReturn.ToArray();
                }
            }
        }

        public ProductDetails[] GetBundleContents(long prodId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"SELECT     Products_1.ProdID AS ProductID, ProductLink.Qty
                    FROM         ProductLink INNER JOIN
                          Products ON ProductLink.ProdID = Products.ProdID INNER JOIN
                          Products AS Products_1 ON ProductLink.LinkToProdID = Products_1.ProdID
                    WHERE     (Products.ProdID = @ProdID)", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.SelectCommand.Parameters.AddWithValue("ProdID", prodId);
                    adapter.Fill(dt);
                    List<ProductDetails> toReturn = new List<ProductDetails>(dt.Rows.Count);
                    foreach (DataRow r in dt.Rows)
                    {
                        ProductDetails pd = new ProductDetails();
                        pd.Id = (long)r["ProductID"];
                        pd.Qty = (double)r["Qty"];
                        toReturn.Add(pd);
                    }
                    return toReturn.ToArray();
                }
            }
        }

        public long[] CheckInvalidBundles(ProductDetails[] details)
        {
            List<long> toReturn = new List<long>();

            for (int i = 0; i < details.Length; i++)
            {
                if (IsBundle(details[i].Id))
                {
                    ProductDetails[] contents = GetBundleContents(details[i].Id);
                    int qty = (int)(details[i].Qty + 0.5);
                    for (int k = 0; k < qty; k++)
                    {
                        foreach (ProductDetails content in contents)
                        {
                            bool invalid = true;
                            for (int j = 0; j < details.Length; j++)
                            {
                                if (details[j].Id == content.Id && details[j].Qty >= content.Qty)
                                {
                                    details[j].Qty -= content.Qty;
                                    invalid = false;
                                    break;
                                }
                            }
                            if (invalid)
                            {
                                toReturn.Add(details[i].Id);
                                break;
                            }
                        }
                    }
                }
            }

            return toReturn.ToArray();
        }

        private DataRow GetProductDetails(long prodId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"SELECT ProdID, PurchasePrice, PriceExclVat1, PriceExclVat2, PriceExclVat3, PriceExclVat4, PriceExclVat5, PriceExclVat6
                    FROM Products WHERE (ProdID = @ProdID)", conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("ProdID", prodId);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows[0];
                }
            }
        }

        public void GetBundleDiscount(long prodId, byte defaultPrice, out decimal priceDiscount, out decimal purchaseDiscount)
        {
            ProductDetails[] contents = GetBundleContents(prodId);
            DataRow rBundle = GetProductDetails(prodId);
            decimal totalPrice = 0m;
            decimal totalPurchasePrice = 0m;
            string priceColumn;
            if (defaultPrice == 0)
                priceColumn = "PurchasePrice";
            else
                priceColumn = "PriceExclVat" + defaultPrice;
            foreach (ProductDetails content in contents)
            {
                DataRow rContent = GetProductDetails(content.Id);
                totalPrice += (decimal)rContent[priceColumn] * (decimal)content.Qty;
                totalPurchasePrice += (decimal)rContent["PurchasePrice"] * (decimal)content.Qty;
            }
            priceDiscount = totalPrice - (decimal)rBundle[priceColumn];
            purchaseDiscount = totalPurchasePrice - (decimal)rBundle["PurchasePrice"];
        }

        public long[] GetProductBundleIds(long productId)
        {
            List<long> ids = new List<long>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"SELECT     ProductLink.LinkToProdID, ProductLink.ProdID
FROM         Products INNER JOIN
                      ProductLink ON Products.ProdID = ProductLink.ProdID
WHERE     (ProductLink.LinkToProdID = @LinkToProdID) AND (Products.Status = 4)", conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("LinkToProdID", productId);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataRow r in dt.Rows)
                    {
                        ids.Add((long)r["ProdID"]);
                    }
                }
            }

            return ids.ToArray();
        }

        public ProductDetails[] RemoveAllBundles(ProductDetails[] products)
        {
            List<ProductDetails> toReturn = new List<ProductDetails>(products);
            for (int i = 0; i < toReturn.Count; i++)
            {
                if (IsBundle(toReturn[i].Id))
                {
                    ProductDetails[] contents = GetBundleContents(toReturn[i].Id);
                    for (int j = 0; j < toReturn.Count; j++)
                    {
                        for (int k = 0; k < contents.Length; k++)
                        {
                            if (contents[k].Id == toReturn[j].Id)
                            {
                                ProductDetails newDetails = toReturn[j];
                                newDetails.Qty -= contents[k].Qty * toReturn[i].Qty;
                                toReturn[j] = newDetails;
                            }
                        }
                    }
                }
            }
            return toReturn.ToArray();
        }
    }
}
