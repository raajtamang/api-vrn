using System.Data;
using Microsoft.Data.SqlClient;


namespace EsquireVRN
{
    public class WebOrder
    {
        private string connectionString;

        public struct WebOrderDetails
        {
            public long ProdId;
            public string ProdCode;
            public string Description;
            public double Qty;
            public decimal PurchasePrice;
            public double Price;
            public long BasketId;
            public bool IsBundle;
        }

        public WebOrder(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public WebOrderDetails[] ProcessBasketForWebOrder(DataTable dtBasket, long orgId)
        {
            WebOrderDetails[] poDetails = new WebOrderDetails[dtBasket.Rows.Count];
            for (int i = 0; i < dtBasket.Rows.Count; i++)
            {
                poDetails[i] = new WebOrderDetails();
                DataRow r = dtBasket.Rows[i];
                poDetails[i].Description = r["ProdDesc"].ToString();
                poDetails[i].PurchasePrice = (decimal)r["PurchasePrice"];
                poDetails[i].ProdCode = r["ProdCode"].ToString();
                poDetails[i].ProdId = (long)r["ProdID"];
                poDetails[i].Qty = (double)(int)r["ProdQty"];
                poDetails[i].BasketId = (long)r["BasketID"];
                poDetails[i].Price = (double)r["Price"];
            }
            return GenerateWebOrder(poDetails, orgId);
        }

        /// <summary>
        /// Checks a purchase order for bundles and updates (discounts) product cost prices accordingly
        /// </summary>
        /// <param name="input">The purchase order to check</param>
        /// <param name="quotationId">The ID of the quotation used to generate the purchase order</param>
        /// <returns>The processed purchase order</returns>
        public WebOrderDetails[] GenerateWebOrder(WebOrderDetails[] input, long orgId)
        {
            List<WebOrderDetails> output = new List<WebOrderDetails>();
            Bundle bundle = new Bundle(connectionString);

            //First add all bundle contents
            for (int i = 0; i < input.Length; i++)
            {
                if (bundle.IsBundle(input[i].ProdId))
                {
                    Bundle.ProductDetails[] contents = bundle.GetBundleContents(input[i].ProdId);
                    decimal totalContentPrice = GetTotalContentPrice(contents);
                    decimal bundleDiscount = 0;
                    
                    decimal dummy;
                    bundle.GetBundleDiscount(input[i].ProdId, 1, out dummy, out bundleDiscount);

                    for (int k = 0; k < input[i].Qty; k++)
                    {
                        foreach (Bundle.ProductDetails content in contents)
                        {

                            for (int j = 0; j < input.Length; j++)
                            {
                                if (input[j].ProdId == content.Id)
                                {
                                    input[j].Qty -= content.Qty;
                                    WebOrderDetails newDetails = input[j];
                                    newDetails.PurchasePrice = (input[j].PurchasePrice) - (((input[j].PurchasePrice / totalContentPrice)) * bundleDiscount);
                                    newDetails.Qty = content.Qty;
                                    newDetails.IsBundle = true;
                                    AddToWebOrderDetailsList(output, newDetails);
                                }
                            }
                        }
                    }
                }
            }
            //Now add the rest
            for (int i = 0; i < input.Length; i++)
            {
                if (!bundle.IsBundle(input[i].ProdId) && (int)(input[i].Qty + 0.5f) > 0)
                {
                    WebOrderDetails newDetails = input[i];
                    newDetails.IsBundle = false;
                    output.Add(newDetails);
                }
            }
            return output.ToArray();
        }

        private bool IsOwnProduct(long prodId, long orgId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(@"SELECT     SourceList.SourceOrgID
                    FROM         Products INNER JOIN
                      OrganisationSource ON Products.OrgSourceID = OrganisationSource.OrgSourceID INNER JOIN
                      SourceList ON OrganisationSource.SourceID = SourceList.SourceID
                    WHERE     (Products.ProdID = @ProdID)", conn))
                {
                    cmd.Parameters.AddWithValue("ProdID", prodId);
                    conn.Open();
                    long sourceOrgId = (long)cmd.ExecuteScalar();
                    return (sourceOrgId == orgId || sourceOrgId == 0);
                }
            }
        }

        /// <summary>
        /// Internal function used to add a purchase order to a purchase order list. If a purchase order with matching ProdId and Price is already in the list,
        /// its qty is increased. Otherwise the item is added to the list
        /// </summary>
        /// <param name="list">The list to add the item to</param>
        /// <param name="item">The item to add</param>
        private void AddToWebOrderDetailsList(List<WebOrderDetails> list, WebOrderDetails item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ProdId == item.ProdId && list[i].PurchasePrice == item.PurchasePrice)
                {
                    WebOrderDetails temp = list[i];
                    temp.Qty += item.Qty;
                    list[i] = temp;
                    return;
                }
            }
            list.Add(item);
        }

        /// <summary>
        /// Calculates the total purchase price of a list, using PurchasePriceExcl and Qty
        /// </summary>
        /// <param name="contents">An array of ProductDetails to calculate total purchase price of</param>
        /// <param name="quotationId">The quotation from which to look up the prices</param>
        /// <returns>The total purchase price of the list</returns>
        private decimal GetTotalContentPrice(Bundle.ProductDetails[] contents)
        {
            decimal total = 0m;
            foreach (Bundle.ProductDetails pd in contents)
            {
                DataRow r = GetProductDetails(pd.Id);
                total += (decimal)r["PurchasePriceExcl"] * (decimal)pd.Qty;
            }
            return total;
        }

        private DataRow GetProductDetails(long prodId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"SELECT     ProdID, PurchasePrice AS PurchasePriceExcl
                    FROM         Products
                    WHERE     (ProdID = @ProdID)", conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("ProdID", prodId);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows[0];
                }
            }
        }
    }
}
