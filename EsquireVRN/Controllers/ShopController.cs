using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private struct InactiveProducts
        {
            public string? ItemsToRemove { get; set; }
            public string? Message { get; set; }
        }

        [HttpGet]
        [Route("GetCartItems")]
        public IActionResult GetCartItems(string SessionID)
        {
            return Ok(Shared.GetCartItems(SessionID));
        }

        [HttpPost]
        [Route("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] CartItem cartItem)
        {

            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    cartItem.CustID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                    if (string.IsNullOrWhiteSpace(cartItem.SessionID))
                    {
                        cartItem.SessionID = "" + Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                    }
                }

                cartItem.OrgID = Shared.GetOrgID();

                string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");

                string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
                if (string.IsNullOrEmpty(connectId))
                {
                    return StatusCode(500, new { error = "Something went wrong with the servers. Please try again. If error persists please contact the administrators." });
                }

                Shared.ProductActiveCheck newCheck = await Shared.CheckProductStatus(connectId, cartItem.ProdCode);

                if (newCheck.Active != true && newCheck.ErrorMessage == null)
                {
                    return StatusCode(500, new { error = "Product seems to be invalid. Please try again. If error persists please contact the administrators." });
                }
                else if (newCheck.Active != true && newCheck.ErrorMessage != null && newCheck.ErrorMessage.Length > 0)
                {
                    return StatusCode(500, new { error = "Something went wrong with the servers. Please try again. If error persists please contact the administrators." });
                }
                int p = Shared.GetProductTotalStockCount(cartItem.ProdID);
                if (p < cartItem.ProdQty)
                {
                    return StatusCode(500, new { error = "Cart Item exceeds available quantity. Please check and try again." });
                }
                long cartItemId = Shared.AddToCart(cartItem);

                return Ok(new { message = "Item added to basket successfully.", CartItemId = cartItemId });
            }
            catch (Exception ex)
            {
                return StatusCode(500,new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("UpdateUserId")]
        [Authorize(Roles = "Reseller,Customer")]
        public IActionResult UpdateUserId(string SessionId)
        {
            List<CartItem> cartItems = Shared.GetCartItems(SessionId);
            long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            cartItems.ForEach(x => x.CustID = CustId);
            Shared.UpdateCartItems(cartItems);
            return Ok(cartItems);

        }

        [HttpGet]
        [Route("GetClientCartItems")]
        [Authorize(Roles = "Reseller,Customer")]
        public IActionResult GetClientCartItems()
        {
            long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            List<CartItem> cartItems = Shared.GetCartItemsByCustId(CustId);
            return Ok(cartItems);
        }

        [HttpPut]
        [Route("UpdateCart")]
        [Authorize(Roles = "Reseller,Customer")]
        public IActionResult UpdateCart([FromBody] CartItem cartItem)
        {
            long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            cartItem.OrgID = Shared.GetOrgID();
            cartItem.CustID = CustId;
            if (Shared.UpdateCart(cartItem))
            {
                return Ok(new { message = "Item updated successfully." });

            }
            return StatusCode(500, new { error = "Something went wrong. Please try again." });
        }

        [HttpDelete]
        [Route("RemoveCartItem")]
        public IActionResult RemoveCartItem(long id, string? SessionID)
        {
            long CustId = 0;
            if (User.Identity.IsAuthenticated)
            {
                CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            }
            if (CustId > 0)
            {
                if (Shared.RemoveCartItemWithCustId(id, CustId))
                {
                    return Ok(new { message = "Item removed successfully." });
                }
            }
            else
            {
                if (Shared.RemoveCartItem(id, SessionID))
                {
                    return Ok(new { message = "Item removed successfully." });
                }
            }
            return StatusCode(500, new { error = "Something went wrong. Please try again." });
        }

        [HttpDelete]
        [Route("ClearCart")]
        public IActionResult ClearCart(string? SessionID)
        {
            long CustId = 0;
            if (User.Identity.IsAuthenticated)
            {
                CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            }
            if (CustId > 0)
            {
                if (Shared.ClearCartWithCustId(CustId))
                {
                    return Ok(new { message = "Item removed successfully." });
                }
            }
            else
            {
                if (Shared.ClearCart(SessionID))
                {
                    return Ok(new { message = "Cart cleared successfully." });
                }
            }
            return StatusCode(500, new { error = "Something went wrong. Please try again." });
        }

        [HttpGet]
        [Route("DeliveryMethods")]
        public IActionResult GetDeliveryMethods()
        {
            List<WebDeliveryMethods> deliverymethods = Shared.GetDeliveryMethods();
            return Ok(deliverymethods);
        }

        [HttpPost]
        [Route("Checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutModel confirmModel)
        {

            if (!string.IsNullOrWhiteSpace(confirmModel.BillingAddress) && confirmModel.BillingAddress.Length < 5)
            {
                return BadRequest(new { error = "Invalid Billing Address." });
            }

            try
            {
                long custId = 0;
                if (User.Identity.IsAuthenticated)
                {
                    custId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                }
                else
                {
                    return StatusCode(401, new { error = "Authentication failed. Please login and try again." });
                }
                if (confirmModel.ShippingId == 0)
                {
                    string strShipID = Shared.GetShippingId(custId);
                    if (strShipID == "0")
                    {
                        return StatusCode(500, new { error = "Please add billing address to continue." });
                    }

                }

                if (!Shared.CheckUserIsActive(custId))
                {
                    return StatusCode(400, new { error = "Your account is inactive." });
                }
                string OutOfStockProducts = CheckStock(custId);
                if (!string.IsNullOrEmpty(OutOfStockProducts))
                {
                    return StatusCode(500, new { error = OutOfStockProducts });
                }



                Shared.UpdateCartPrice(custId);
                List<CartItem> cartItems = Shared.GetCartItemsByCustId(custId);

                string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);

                if (string.IsNullOrEmpty(connectId))
                {
                    return StatusCode(500, new { error = "Something went wrong with the servers. Please try again. If error persists please contact the administrators." });
                }

                List<InactiveProducts> iProducts = new();
                foreach (var item in cartItems)
                {
                    var newCheck = await Shared.CheckProductStatus(connectId, item.ProdCode);
                    if (newCheck.Active != true && newCheck.ErrorMessage == null)
                    {
                        InactiveProducts iProduct = new()
                        {
                            ItemsToRemove = item.BasketId + "",
                            Message = "Product with code : " + item.ProdCode + " seems to be invalid. If error persists please contact the administrators."
                        };
                        iProducts.Add(iProduct);
                    }
                    else if (newCheck.Active != true && newCheck.ErrorMessage != null && newCheck.ErrorMessage.Length > 0)
                    {
                        InactiveProducts iProduct = new()
                        {
                            ItemsToRemove = item.BasketId + "",
                            Message = "Something went wrong with the servers. Please try again. If error persists please contact the administrators."
                        };
                        iProducts.Add(iProduct);
                    }
                }

                if (iProducts.Any())
                {
                    return StatusCode(422, new { result = iProducts });
                }

                string BilliingName = "", BilliingEmail = "", ShippingName = "", ShippingEmail = "";


                Customer CustomerDetails = Shared.GetCustomer(custId);
                OrgWebDetail orgDetails = Shared.GetOrgWebDetail();
                List<BankDetails> bandetails = Shared.GetBankDetails();

                Models.DeliveryDetails dDetais = new()
                {
                    PaymentId = confirmModel.PaymentId,
                    DeliveryCharge = confirmModel.DeliveryCharge,
                    DeliveryType = Shared.getDeliveryID(confirmModel.DeliveryDescription),
                    BillingAddress = confirmModel.BillingAddress,
                    BillingCountry = confirmModel.BillingCountry,
                    ShippingId = confirmModel.ShippingId,
                    ShippingCountry = confirmModel.ShippingCountry,
                    ShippingAddress = confirmModel.ShippingAddress,
                    NearestBranchId = confirmModel.NearestBranchId,
                    CustRef = confirmModel.CustRef,
                    DeliveryDescription = confirmModel.DeliveryDescription,
                    DeliveryText = confirmModel.DeliveryText,
                    ShippingInstruction = confirmModel.ShippingInstruction,
                    BillingName = confirmModel.BillingName,
                    BillingEmail = confirmModel.BillingEmail,
                    BillingPhone = confirmModel.BillingPhone,
                    ShippingName = confirmModel.ShippingName,
                    ShippingEmail = confirmModel.ShippingEmail,
                    ShippingPhone = confirmModel.ShippingPhone
                };
                return Ok(new { Customer = CustomerDetails, OrgnisationDetails = orgDetails, CartItems = cartItems, BankDetails = bandetails, DeliveryDetails = dDetais });
            }
            catch (Exception Ex)
            {
                return StatusCode(500, new { error = "Something went wrong with server. Please try again." });
            }

        }

        private string CheckStock(long custId)
        {
            List<CartItem> cartItems = Shared.GetCartItemsByCustId(custId);
            string error = "";
            List<string> stocklessProducts = new();
            foreach (var item in cartItems)
            {
                long stockCount = Shared.GetProductStock(item.ProdID);
                if (stockCount < item.StockQuantity)
                {
                    stocklessProducts.Add(item.ProdCode);
                }
            }
            if (stocklessProducts.Count > 0)
            {
                error = "Items with Product Codes (" + string.Join(',', stocklessProducts) + ") has invalid stock quantity. Please check and try again.";
            }
            return error;
        }

        [HttpPost]
        [Route("Confirm")]
        [Authorize(Roles = "Customer,Reseller")]
        public async Task<IActionResult> Confirm(ConfirmModel confirmModel)
        {
            SqlConnection Conn = new SqlConnection(Shared.connString);
            try
            {
                Shared.DeliveryDetails details = Shared.getDeliveryDescID(confirmModel.DeliveryType);
                string strPaymentID = "" + confirmModel.PaymentId;
                string strShipID = "" + confirmModel.ShippingId;

                string strBraID = "" + confirmModel.NearestBranchId;
                string strOrdID = "";
                string strCost = "";
                string custRef = "";
                string strShippingInstruction = "";
                if (!string.IsNullOrEmpty(confirmModel.ShippingInstruction))
                {
                    strShippingInstruction = confirmModel.ShippingInstruction;
                }
                strCost = confirmModel.DeliveryCharge.ToString("0.00").Replace(",", ".");

                if (!string.IsNullOrWhiteSpace(confirmModel.CustRef))
                {
                    custRef = confirmModel.CustRef.Replace("'", "''");
                }
                string strDeliveryQuoteId = confirmModel.DeliveryQuoteId;


                string strSQL = "";
                long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                strSQL = @"SELECT WEBBasket.*, Products.PurchasePrice 
                            FROM WEBBasket INNER JOIN Products ON WEBBasket.ProdID = Products.ProdID
                            WHERE WEBBasket.OrgID=" + Shared.GetOrgID() + " AND WEBBasket.CustId=" + CustomerID;
                DataTable dtBasket = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, Shared.connString))
                {
                    adapter.Fill(dtBasket);
                }
                if (dtBasket.Rows.Count == 0)
                {
                    return StatusCode(500, new { error = "No items in the basket." });
                }
                decimal bundleDiscount = 0;
                Bundle bundle = new Bundle(Shared.connString);
                foreach (DataRow r in dtBasket.Rows)
                {
                    if (bundle.IsBundle((long)r["ProdID"]))
                    {
                        decimal dummy, purchaseDiscount;
                        bundle.GetBundleDiscount((long)r["ProdID"], 1, out dummy, out purchaseDiscount);
                        bundleDiscount += purchaseDiscount * (int)r["ProdQty"];
                    }
                }

                string notes = "";
                
                if (confirmModel.ShippingId == 0)
                {
                    strShipID = Shared.GetShippingId(CustomerID);

                }

                strSQL = "INSERT INTO WEBOrders (CustID, DeliveryMethod, DeliveryDescID, DeliveryCost, PayID, " +
                 "ShippingID, StatusID, OrgID, OrgBranchID, DeliveryQuoteID, DistOrdStatus, CustRef, Notes, Discount,DeliveryId,ShippingInstruction) VALUES " +
                 "(" + CustomerID + ", N'" +
                 details.DeliveryDesc.Replace("'", "''") + "'," +
                 details.DeliveryDescID.ToString() + "," + strCost + "," + confirmModel.PaymentId + "," + strShipID + ",2," + Shared.GetOrgID() +
                 "," + strBraID + ",N'" + strDeliveryQuoteId + "',1 ,N'" + custRef + "', N'" + notes + "'," +
                 bundleDiscount.ToString("0.00").Replace(",", ".") + ",N'" + details.DeliveryID + "',N'" + strShippingInstruction + "'); SELECT SCOPE_IDENTITY();";
                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                Shared.UpdateCartPrice(CustomerID);

                //SqlCommand Cmd = new SqlCommand(strSQL, Conn);
                using (var db = new SqlConnection(Shared.connString))
                {
                    strOrdID = db.Query<string>(strSQL).FirstOrDefault();
                }
                Shared.UpdateOrderStatus(Convert.ToInt64(strOrdID), 2, "" + CustomerID);

                //strOrdID = Cmd.ExecuteScalar().ToString();
                WebOrder wo = new WebOrder(Shared.connString);
                WebOrder.WebOrderDetails[] woDetails = wo.ProcessBasketForWebOrder(dtBasket, Shared.GetOrgID());
                StringBuilder sbSql = new StringBuilder();
                double TotalAmount = 0;
                foreach (WebOrder.WebOrderDetails detail in woDetails)
                {
                    sbSql.Append(@"INSERT INTO WEBOrderItems (OrderID, ProdID, ProdQty, Price, ProdDesc, ProdCode)
                                VALUES (" + Shared.Val(strOrdID) + "," + detail.ProdId + "," +
                        detail.Qty + "," + detail.Price.ToString("0.00").Replace(",", ".") +
                        ",'" + detail.Description.Replace("\'", "\'\'") + "','" +
                        detail.ProdCode.Replace("\'", "\'\'") + "');");
                    TotalAmount += Math.Round((Math.Round(detail.Price, 2) * detail.Qty), 2);
                }
                if (confirmModel.DeliveryCharge > 0)
                {
                    TotalAmount += Math.Round((Convert.ToDouble(confirmModel.DeliveryCharge)), 2);
                }
                if (sbSql.ToString().Length > 5)
                {
                    using (var db = new SqlConnection(Shared.connString))
                    {
                        db.Execute(sbSql.ToString());
                    }
                    //Cmd = new SqlCommand(sbSql.ToString(), Conn);
                    //Cmd.ExecuteNonQuery();
                }


                long OrderId = Convert.ToInt64(strOrdID);
                Shared.BranchDetail branchDetail = Shared.getBranchName("" + confirmModel.NearestBranchId);
                string confrimMail = branchDetail.BranchEMail;
                string branchName = branchDetail.OrgBraShort;


                if (confirmModel.PaymentId == Shared.PAY_ID_ELECTRONIC_TRANSFER)
                {
                    string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                    string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                    string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                    string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
                    if (string.IsNullOrEmpty(connectId))
                    {
                        Customer tempcustomer = Shared.GetCustomer(CustomerID);
                        notes = "";
                        string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + notes + "' Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearCartWithCustId(CustomerID);

                        string finconsubject = "Order Confirmation and Processing Update";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        List<string> Emails = new() { tempcustomer.Email };
                        List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                        string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                        return StatusCode(200, new { message = "Order confirmed successfully." });
                    }
                    Shared.FinconResult RESULT = await Shared.UpdateFincon(OrderId, connectId, FinconServerUsername, FinconServerPassword);
                    if (RESULT.Error)
                    {
                        if (RESULT.ErrorMessage == "Connection Error")
                        {
                            Customer tempcustomer = Shared.GetCustomer(CustomerID);
                            string tempnotes = "";
                            string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + tempnotes + "' Where OrderID=" + strOrdID;
                            using (var db = new SqlConnection(Shared.connString))
                            {
                                db.Execute(sql);
                            }

                            Shared.ClearCartWithCustId(CustomerID);

                            string finconsubject = "Order Confirmation and Processing Update";
                            string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                            List<string> Emails = new() { tempcustomer.Email };
                            List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                            string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                            BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                            return StatusCode(200, new { message = "Order confirmed successfully." });
                        }
                        else
                        {
                            string AccountNo = Shared.GetCustomerAccountNo(CustomerID);
                            string finconsubject = "Warning : Couldn't update order to fincon server.";
                            string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                            string storeName = Shared.GetWebConfigKeyValue("SiteName");
                            List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                            string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + CustomerID + "<br />Error : " + RESULT.ErrorMessage;
                            BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));

                            return StatusCode(500, new { error = RESULT.ErrorMessage });
                        }
                    }

                    if (!string.IsNullOrEmpty(RESULT.FinconId))
                    {
                        notes = "ON#: Order Number: " + RESULT.FinconId;
                        string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + notes + "',DistOrdStatus=4 Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearCartWithCustId(CustomerID);

                        Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, CustomerID, FinconServerUsername, FinconServerPassword);

                        string terms = "";
                        string CreditAvailable = "0.00";
                        if (BillingDetail != null)
                        {
                            terms = BillingDetail.Value.Terms;
                            CreditAvailable = BillingDetail.Value.CreditAvailable;
                        }
                        Customer customer = Shared.GetCustomer(CustomerID);
                        List<string> emails = PrepareEmail(Convert.ToInt64(strOrdID), RESULT.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
                        string emailbody = emails[0];
                        string doc = emails[1];

                        string subject = "Order Number: " + RESULT.FinconId + " From Esquire Technologies has been processed.";
                        string[] toEmail = new string[]
                         {
                        new(customer.Email)
                         };
                        List<string> bcc = new() { "test@esquire.co.za", confrimMail, "info@esquire.co.za" };

                        BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(RESULT.FinconId), "Order"));

                        return Ok(new { message = "Order confirmed successfully." });
                    }
                    else
                    {
                        string AccountNo = Shared.GetCustomerAccountNo(CustomerID);
                        string finconsubject = "Warning : Couldn't update order to fincon server.";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        string storeName = Shared.GetWebConfigKeyValue("SiteName");
                        List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                        string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + CustomerID + "<br />Error : " + RESULT.ErrorMessage;
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                        return StatusCode(500, new { error = RESULT.ErrorMessage });
                    }

                }
                else if (confirmModel.PaymentId == Shared.COLLECT_AND_PAY_AT_SHOP)
                {
                    string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                    string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                    string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                    string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);

                    if (string.IsNullOrEmpty(connectId))
                    {
                        Customer tempcustomer = Shared.GetCustomer(CustomerID);
                        notes = "";
                        string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + notes + "' Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearCartWithCustId(CustomerID);

                        string finconsubject = "Order Confirmation and Processing Update";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        List<string> Emails = new() { tempcustomer.Email };
                        List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                        string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                        return StatusCode(200, new { message = "Order confirmed successfully." });
                    }

                    Shared.FinconResult RESULT = await Shared.UpdateFincon(OrderId, connectId, FinconServerUsername, FinconServerPassword);
                    if (RESULT.Error)
                    {
                        if (RESULT.ErrorMessage == "Connection Error")
                        {
                            Customer tempcustomer = Shared.GetCustomer(CustomerID);
                            notes = "";
                            string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + notes + "' Where OrderID=" + strOrdID;
                            using (var db = new SqlConnection(Shared.connString))
                            {
                                db.Execute(sql);
                            }

                            Shared.ClearCartWithCustId(CustomerID);

                            string finconsubject = "Order Confirmation and Processing Update";
                            string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                            List<string> Emails = new() { tempcustomer.Email };
                            List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                            string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                            BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                            return StatusCode(200, new { message = "Order confirmed successfully." });
                        }
                        else
                        {
                            string AccountNo = Shared.GetCustomerAccountNo(CustomerID);
                            string finconsubject = "Warning : Couldn't update order to fincon server.";
                            string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                            string storeName = Shared.GetWebConfigKeyValue("SiteName");
                            List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                            string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + CustomerID + "<br />Error : " + RESULT.ErrorMessage;
                            BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                            return StatusCode(500, new { error = RESULT.ErrorMessage });
                        }
                    }

                    if (!string.IsNullOrEmpty(RESULT.FinconId))
                    {
                        notes = "ON#: Order Number: " + RESULT.FinconId;
                        string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + notes + "',DistOrdStatus=4 Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearCartWithCustId(CustomerID);

                        Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, CustomerID, FinconServerUsername, FinconServerPassword);

                        string terms = "";
                        string CreditAvailable = "0.00";
                        if (BillingDetail != null)
                        {
                            terms = BillingDetail.Value.Terms;
                            CreditAvailable = BillingDetail.Value.CreditAvailable;
                        }
                        Customer customer = Shared.GetCustomer(CustomerID);
                        List<string> emails = PrepareEmail(Convert.ToInt64(strOrdID), RESULT.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
                        string emailbody = emails[0];
                        string doc = emails[1];
                        string finconId = Convert.ToInt64(RESULT.FinconId).ToString().PadLeft(8, '0');
                        string subject = "Order Number: " + finconId + " From Esquire Technologies has been processed.";
                        string[] toEmail = new string[]
                         {
                        new(customer.Email)
                         };
                        List<string> bcc = new() { "test@esquire.co.za", confrimMail, "info@esquire.co.za" };

                        BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(RESULT.FinconId), "Order"));

                        return Ok(new { message = "Order confirmed successfully." });
                    }
                    else
                    {
                        string AccountNo = Shared.GetCustomerAccountNo(CustomerID);
                        string finconsubject = "Warning : Couldn't update order to fincon server.";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        string storeName = Shared.GetWebConfigKeyValue("SiteName");
                        List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                        string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + CustomerID + "<br />Error : " + RESULT.ErrorMessage;
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                        return StatusCode(500, new { error = RESULT.ErrorMessage });
                    }
                }
                else if (confirmModel.PaymentId == Shared.PAY_ID_CREDIT_CARD_INSTANT_EFT_MOBI_CREDIT)
                {
                    Serilog.Log.Error("Order No. : " + OrderId + " : Confirm Total Price : " + TotalAmount);
                    string refId = EncryptionService.EncryptString(OrderId + "-" + CustomerID) + "!" + OrderId;
                    return Ok(new { message = "Order confirmed successfully. Please proceed to make payment.", reference = refId, TotalAmount, CustomerEmail = User.Identity.Name });
                }
                return Ok(new { message = "Order confirmed successfully." });


            }
            catch (Exception Excp)
            {
                int linenumber = (new StackTrace(Excp, true)).GetFrame(0).GetFileLineNumber();
                Serilog.Log.Error(Excp.Message + " at line number " + linenumber);
                return StatusCode(500, new { error = "Something went wrong with the server. Please try again in a few minutes." });

            }           

        }

        private List<string> PrepareEmail(long orderId, string finconId, string terms, string credit, long OrgId)
        {
            finconId = Convert.ToInt64(finconId).ToString().PadLeft(8, '0');
            var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/api/Shop";
            Order order = Shared.GetOrder(orderId);
            Shared.DeliveryDetails details = Shared.getDeliveryDescID(order.DeliveryID);
            string AccountNumber = Shared.GetAccountNumber(order.CustID);
            double deliveryCharge = Math.Round(Convert.ToDouble(order.DeliveryCost), 2);
            List<OrderItem> items = Shared.GetOrderItems(orderId);
            string BillBody = "", PdfBody = "";
            double orderAmount = 0;
            Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.OrgBranchID);
            string currencyFormat = Shared.GetWebConfigKeyValue("CurrencyFormat");
            string confrimMail = branchDetail.BranchEMail;
            string strShippingInstruction = "";
            if (!string.IsNullOrEmpty(order.ShippingInstruction))
            {
                strShippingInstruction = "(" + order.ShippingInstruction + ")";
            }
            string paymentRefId = EncryptionService.EncryptString("" + orderId + "-" + order.CustID) + "!" + order.OrderID;
            foreach (var detail in items)
            {
                BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + detail.ProdCode + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><p style='white-space:pre-wrap;max-width: 650px;'>" + detail.ProdDesc + "</p></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price / 1.15), 2).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + detail.ProdQty + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round(((detail.Price / 1.15) * detail.ProdQty), 2).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price * detail.ProdQty), 2).ToString(currencyFormat) + "</td></tr>";
                PdfBody += "<tr><td style='padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'>" + detail.ProdCode + "</td><td style='white-space: nowrap;padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'><p style='white-space:pre-wrap;max-width: 650px;'>" + detail.ProdDesc + "</p></td><td style='padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price / 1.15), 2).ToString(currencyFormat) + "</td><td style='padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'>" + detail.ProdQty + "</td><td style='padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'>R " + Math.Round(((detail.Price / 1.15) * detail.ProdQty), 2).ToString(currencyFormat) + "</td><td style='padding: 0.5rem; text-align: left; font-size: 0.9rem; border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price * detail.ProdQty), 2).ToString(currencyFormat) + "</td></tr>";
                orderAmount += Math.Round((Math.Round(detail.Price, 2) * detail.ProdQty), 2);
            }
            string strDeliveryQuoteId = requestUrl + "/GetWayBill" + "?c=" + order.DeliveryQuoteID;
            string paySlipUrl = requestUrl + "/GetPackingSlip" + "?o=" + orderId + "&c=" + AccountNumber;
            if (deliveryCharge > 0)
            {
                BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>CDT001</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><div>Courier with Courier Direct:&nbsp;</div><div>Print out the <a href='" + strDeliveryQuoteId + "' target='_blank'>Waybill - " + order.DeliveryQuoteID + "</a></div></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge / 1.15).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>1</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge / 1.15).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td></tr>";
                PdfBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>CDT001</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><div>Courier with Courier Direct:&nbsp;</div><div>Print out the <a href='" + strDeliveryQuoteId + "' target='_blank'>Waybill - " + order.DeliveryQuoteID + "</a></div></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge / 1.15).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>1</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge / 1.15).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td></tr>";
            }

            string pdfContent = "";
            string emailbody;
            if ("" + order.DeliveryDescID == Shared.CD_DESC_ID)
            {
                DeliveryAddress dAddress = Shared.GetDeliveryAddress(order.ShippingID);
                string deliveryAddress = "";
                if (dAddress != null)
                {
                    deliveryAddress = dAddress.ShippingAddress + ", " + dAddress.Town + ", " + dAddress.ShippingCountry;
                }
                string orderInstruction = "Use this order number when talking to Esquire Technologies.";
                if (OrgId == 473)
                {
                    orderInstruction = "Use this order number when talking to Esquire Technologies*.";
                }
                string MailFormat = Shared.GetWebConfigKeyValue("OrderConfirmMailCourierDirect");
                string PdfFormat = Shared.GetWebConfigKeyValue("OrderConfirmPdfCourierDirect");
                string tAmount = Math.Round((orderAmount + deliveryCharge), 2).ToString(currencyFormat);
                string use_this = "Use this order number when talking to Esquire Technologies. | <span style='color:red';>&nbsp; &nbsp;" + strShippingInstruction + "</span>";

                string talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref :  Debit or Credit Card (" + paymentRefId + ")";
                if (order.PayID == 2)
                {
                    talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref : Electronic Funds Transfer (EFT)";
                }
                else if (order.PayID == 12)
                {
                    talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref : Collect And Pay At Shop";

                }
                emailbody = MailFormat.Replace("{0}", talkTo).Replace("{1}", finconId).Replace("{2}", "R " + tAmount).Replace("{3}", deliveryAddress).Replace("{4}", strDeliveryQuoteId).Replace("{5}", order.DeliveryQuoteID).Replace("{6}", paySlipUrl).Replace("{7}", BillBody).Replace("{8}", "R " + tAmount).Replace("{BranchMail}", confrimMail).Replace("{credit}", credit).Replace("{terms}", terms).Replace("{order_instruction}", orderInstruction).Replace("{use_this_while}", use_this);
                pdfContent = PdfFormat.Replace("{0}", talkTo).Replace("{1}", finconId).Replace("{2}", "R " + tAmount).Replace("{3}", deliveryAddress).Replace("{4}", strDeliveryQuoteId).Replace("{5}", order.DeliveryQuoteID).Replace("{6}", paySlipUrl).Replace("{7}", PdfBody).Replace("{8}", "R " + tAmount).Replace("{BranchMail}", confrimMail).Replace("{credit}", credit).Replace("{terms}", terms).Replace("{order_instruction}", orderInstruction).Replace("{use_this_while}", use_this);
            }
            else
            {
                string collect_courier = "Collect From Shop";
                if (order.DeliveryDescID == Shared.OWN_COURIER_TO_COLLECT)
                {
                    collect_courier = "Own Courier To Collect";
                }
                string MailFormat = Shared.GetWebConfigKeyValue("OrderConfirmMailCollectFromShop");
                string PdfFormat = Shared.GetWebConfigKeyValue("OrderConfirmPdfCollectFromShop");

                string orderInstruction = "Use this order number when talking to Esquire Technologies.";
                string use_this = "Use this order number when talking to Esquire Technologies. | <span style='color:red';>&nbsp; &nbsp;" + strShippingInstruction + "</span>";
                if (OrgId == 473)
                {
                    orderInstruction = "Use this order number when talking to Esquire Technologies*.";
                }
                string oAmount = orderAmount.ToString(currencyFormat);
                string talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref :  Debit or Credit Card (" + paymentRefId + ")";
                if (order.PayID == 2)
                {
                    talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref :  Electronic Funds Transfer (EFT)";
                }
                else if (order.PayID == 12)
                {
                    talkTo = finconId + " | " + "Web Ref : " + order.OrderID + " (Office use only) | Payment Ref : Collect And Pay At Shop";

                }

                emailbody = MailFormat.Replace("{0}", talkTo).Replace("{1}", finconId).Replace("{2}", "R " + oAmount).Replace("{3}", paySlipUrl).Replace("{4}", BillBody).Replace("{5}", "R " + oAmount).Replace("{BranchMail}", confrimMail).Replace("{credit}", credit).Replace("{terms}", terms).Replace("{order_instruction}", orderInstruction).Replace("{use_this_while}", use_this).Replace("{collect_courier}", collect_courier);
                pdfContent = PdfFormat.Replace("{0}", talkTo).Replace("{1}", finconId).Replace("{2}", "R " + oAmount).Replace("{3}", paySlipUrl).Replace("{4}", PdfBody).Replace("{5}", "R " + oAmount).Replace("{BranchMail}", confrimMail).Replace("{credit}", credit).Replace("{terms}", terms).Replace("{order_instruction}", orderInstruction).Replace("{use_this_while}", use_this).Replace("{collect_courier}", collect_courier);
            }
            List<string> returnString = new()
            {
                emailbody,
                pdfContent
            };
            double eTotal = orderAmount + deliveryCharge;
            Serilog.Log.Error("Order No. : " + order.OrderID + " : Email Total Price : " + eTotal);
            return returnString;
        }

        [HttpGet]
        [Route("GetPackingSlip")]
        public IActionResult GetPackingSlip(long o, string? c)
        {
            string html = Shared.GetOrderDetails(o, c);
            return Content(html, "text/html", Encoding.UTF8);
        }

        [HttpGet]
        [Route("GetWayBill")]
        public async Task<IActionResult> GetWayBill(string c)
        {
            string baseUrl = "https://courierdirect.couriermate.co.za/api/json";
            string courierUsername = Shared.GetWebConfigKeyValue("CourierUsername");
            string courierPassword = Shared.GetWebConfigKeyValue("CourierPassword");
            using (HttpClient client = new HttpClient())
            {
                string body = "{\"username\" : \"" + courierUsername + "\",\"password\" : \"" + courierPassword + "\",\"method\" : \"get_delivery_doc\",\"delivery_no\" : \"" + c + "\"}";
                var values = JsonObject.Parse(body).ToString();
                HttpResponseMessage response = await client.PostAsync(baseUrl, new StringContent(values, Encoding.UTF8, "application/json"));
                var responseBody = await response.Content.ReadAsStringAsync();
                WaybillModel.WayBillResponse resp_content = JsonConvert.DeserializeObject<WaybillModel.WayBillResponse>(responseBody);
                if (resp_content != null)
                {
                    string base64Value = resp_content.records[0].base64;
                    if (!string.IsNullOrWhiteSpace(base64Value))
                    {
                        byte[] byteArray = Convert.FromBase64String(base64Value);
                        return File(byteArray, "application/octet-stream", "Waybill - " + c + ".pdf");
                    }
                    else
                    {
                        return NotFound(new { error = "Waybill doesn't exist" });
                    }
                }
                else
                {
                    return NotFound(new { error = "Waybill doesn't exist" });
                }
            }
        }
        [HttpGet]
        [Route("PaymentMade/{id}")]
        public async Task<IActionResult> PaymentMade(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return StatusCode(401, "Invalid Reference Key.");
            }
            long OrderId = 0;
            try
            {
                string reqId = id.Split('!')[1];
                //string key = reqId.Replace("-", "=").Replace("_", "+").Replace(".", "/");
                //string decrypedKey = EncryptionService.DecryptString(key);
                OrderId = Convert.ToInt64(reqId);
                //long CustId = Convert.ToInt64(decrypedKey.Split('-')[1]);
            }
            catch
            {
                return StatusCode(401, "Invalid Reference Key.");
            }
            Order order = Shared.GetOrder(OrderId);

            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Order doesn't exist." });
            }
            Customer cust = Shared.GetCustomer(order.CustID);
            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Customer doesn't exist." });
            }

            Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.OrgBranchID);
            string confrimMail = branchDetail.BranchEMail;
            string branchName = branchDetail.OrgBraShort;
            string terms = "";
            string CreditAvailable = "0.00";
            string FinconId = "";
            string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
            string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
            string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
            if (order.FinconId == null)
            {
                string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
                if (string.IsNullOrEmpty(connectId))
                {
                    Customer tempcustomer = Shared.GetCustomer(order.CustID);
                    string tempnotes = "";
                    string sql = "Update WebOrders Set FinconId=-1,StatusID=3,Notes=N'" + tempnotes + "' Where OrderID=" + order.OrderID;
                    using (var db = new SqlConnection(Shared.connString))
                    {
                        db.Execute(sql);
                    }

                    Shared.ClearCartWithCustId(order.CustID);

                    string finconsubject = "Order Confirmation and Processing Update";
                    string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                    List<string> Emails = new() { tempcustomer.Email };
                    List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                    string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                    BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                    return StatusCode(200, new { message = "Order confirmed successfully." });
                }

                Shared.FinconResult RESULT = await Shared.UpdateFincon(order.OrderID, connectId, FinconServerUsername, FinconServerPassword);
                if (RESULT.Error)
                {
                    if (RESULT.ErrorMessage == "Connection Error")
                    {
                        Customer tempcustomer = Shared.GetCustomer(order.CustID);
                        string tempnotes = "";
                        string sql = "Update WebOrders Set FinconId=-1,StatusID=3,Notes=N'" + tempnotes + "' Where OrderID=" + order.OrderID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearCartWithCustId(order.CustID);

                        string finconsubject = "Order Confirmation and Processing Update";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        List<string> Emails = new() { tempcustomer.Email };
                        List<string> cc = new() { "syanthan1st@gmail.com", confrimMail, "info@esquire.co.za" };
                        string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za", false));

                        return StatusCode(200, new { message = "Order confirmed successfully." });
                    }
                    else
                    {
                        string AccountNo = Shared.GetCustomerAccountNo(order.CustID);
                        string finconsubject = "Warning : Couldn't update order to fincon server.";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        string storeName = Shared.GetWebConfigKeyValue("SiteName");
                        List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                        string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + order.CustID+ "<br />Error : " + RESULT.ErrorMessage;
                        BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                        return StatusCode(500, new { error = RESULT.ErrorMessage });
                    }
                }


                string notes = "";
                if (!string.IsNullOrEmpty(RESULT.FinconId))
                {
                    notes = "ON#: Order Number: " + RESULT.FinconId;
                    string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + notes + "',DistOrdStatus=4,StatusID=3,PayID=3 Where OrderID=" + OrderId;
                    using (var db = new SqlConnection(Shared.connString))
                    {
                        db.Execute(sql);
                    }


                    FinconId = RESULT.FinconId;
                    string changeby = "" + order.CustID;
                    Shared.UpdateOrderStatus(OrderId, 3, changeby);

                    Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, order.CustID, FinconServerUsername, FinconServerPassword);
                    if (BillingDetail != null)
                    {
                        terms = BillingDetail.Value.Terms;
                        CreditAvailable = BillingDetail.Value.CreditAvailable;
                    }

                    Shared.ClearCartWithCustId(order.CustID);

                }
                else
                {
                    string AccountNo = Shared.GetCustomerAccountNo(order.CustID);
                    string finconsubject = "Warning : Couldn't update order to fincon server.";
                    string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                    string storeName = Shared.GetWebConfigKeyValue("SiteName");
                    List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                    string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + order.CustID + "<br />Error : " + RESULT.ErrorMessage;
                    BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                    return StatusCode(500, new { error = RESULT.ErrorMessage });
                }


            }
            else
            {
                string sql = "Update WebOrders Set DistOrdStatus=4,StatusID=3,PayID=3 Where OrderID=" + OrderId;
                using (var db = new SqlConnection(Shared.connString))
                {
                    db.Execute(sql);
                }
                // FinconId = RESULT.FinconId;
                string changeby = "" + order.CustID;
                Shared.UpdateOrderStatus(OrderId, 3, changeby);

                FinconId = "" + order.FinconId;

                string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);

                Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, order.CustID, FinconServerUsername, FinconServerPassword);
                if (BillingDetail != null)
                {
                    terms = BillingDetail.Value.Terms;
                    CreditAvailable = BillingDetail.Value.CreditAvailable;
                }

                Shared.ClearCartWithCustId(order.CustID);
            }


            Customer customer = Shared.GetCustomer(order.CustID);
            List<string> emails = PrepareEmail(order.OrderID, FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
            string emailbody = emails[0];
            string doc = emails[1];

            string subject = "Order Number: " + FinconId + " From Esquire Technologies has been processed.";
            string[] toEmail = new string[]
            {
                        new(cust.Email)
            };
            List<string> bcc = new() { "test@esquire.co.za", confrimMail, "info@esquire.co.za" };

            BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(FinconId), "Order"));
            return Ok(new { message = "Order confirmed successfully." });
        }

        [HttpGet]
        [Route("ResendEmail/{id}")]
        [Authorize]
        public async Task<IActionResult> ResendEmail(long id, string? email)
        {
            Order order = Shared.GetOrder(id);

            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Order doesn't exist." });
            }
            Customer cust = Shared.GetCustomer(order.CustID);
            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Customer doesn't exist." });
            }
            if (order.FinconId == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Bill was generated using old system. Please use old system to resend email." });
            }
            string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
            string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
            string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
            string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
            Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.OrgBranchID);
            string confrimMail = branchDetail.BranchEMail;
            string branchName = branchDetail.OrgBraShort;
            Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, order.CustID, FinconServerUsername, FinconServerPassword);

            string terms = "";
            string CreditAvailable = "0.00";
            if (BillingDetail != null)
            {
                terms = BillingDetail.Value.Terms;
                CreditAvailable = BillingDetail.Value.CreditAvailable;
            }
            Customer customer = Shared.GetCustomer(order.CustID);
            List<string> emails = PrepareEmail(order.OrderID, "" + order.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
            string emailbody = emails[0];
            string doc = emails[1];

            string finconId = Convert.ToInt64(order.FinconId).ToString().PadLeft(8, '0');
            string subject = "Order Number: " + finconId + " From Esquire Technologies has been processed.";
            if (string.IsNullOrEmpty(email))
            {
                email = cust.Email;
            }
            string[] toEmail = new string[]
            {
                        new(email)
            };
            List<string> bcc = new();
            if (User.IsInRole("Admin"))
            {
                bcc = new() { "test@esquire.co.za", confrimMail, "info@esquire.co.za" };
            }
            else
            {
                bcc = new() { "test@esquire.co.za" };
            }
            BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(order.FinconId), "Order"));

            //doc.Close();
            return StatusCode(200, new { message = "Email resent successfully" });
        }


        [HttpGet]
        [Route("OrderCanceled/{id}")]
        public IActionResult OrderCanceled(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return StatusCode(401, "Invalid Reference Key.");
            }

            long OrderId = 0;
            try
            {
                string reqId = id.Split('!')[1];
                OrderId = Convert.ToInt64(reqId);

            }
            catch (Exception)
            {
                return StatusCode(401, "Invalid Reference Key.");
            }
            Order order = Shared.GetOrder(OrderId);

            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Order doesn't exist." });
            }

            string sql = "Update WebOrders Set StatusID=10,DistOrdStatus=6,Notes='',PayID=3 Where OrderID=" + OrderId;
            using (var db = new SqlConnection(Shared.connString))
            {
                db.Execute(sql);
            }

            string changeby = "" + order.CustID;

            Shared.UpdateOrderStatus(OrderId, 10, changeby);

            return StatusCode(200, new { message = "Order has been cancelled." });
        }

 

        [HttpGet]
        [Route("AutoCancelOrder")]
        public IActionResult CancelOrders(string u, string p)
        {
            try
            {
                LoginDetails l = Shared.AdminLogin(u, p);
                if (l.CustID == Shared.INVALID_LOGIN)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Invalid Login" });
                }
                string query = "Update WEBOrders Set StatusID=10,DistOrdStatus=6 WHERE PayID=3 AND StatusID=2 AND FinconId IS NULL AND ORGID=" + Shared.GetOrgID() + " AND OrderDate <= DATEADD(MINUTE, -10, GETDATE())";
                using (var db = new SqlConnection(Shared.connString))
                {
                    db.Execute(query);
                }
            }
            catch (Exception ex)
            {
                string finconsubject = "Warning : Order couldn't be cancelled automatically.";
                string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                string storeName = Shared.GetWebConfigKeyValue("SiteName");
                List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                string finconemailbody = "<br /><br />Error occurred while canceling order. Error is : " + ex.Message;
                BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
            }
            return Ok();
        }


        [HttpGet]
        [Route("ProcessOrders")]
        public async Task<IActionResult> ProcessOrders(string u, string p)
        {
            try
            {
                LoginDetails l = Shared.AdminLogin(u, p);
                if (l.CustID == Shared.INVALID_LOGIN)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Invalid Login" });
                }
                List<Order> orders = new();
                string query = "SELECT [OrderID],[CustID],[OrderDate],[DeliveryMethod],[DeliveryDescID],[DeliveryCost],[PayID],[StatusID],[Notes],[ShippingID],[OrgID],[OrgBranchID],[Insurance],[DeliveryQuoteID],[DistOrdStatus],[ReviewEmailSent],[DeliveryWaybillID],[CustRef],[DiscountRefCode],[Discount],[DeliveryID],[FinconId],[ShippingInstruction] FROM [dbo].[WEBOrders] WHERE [FinconId]=-1";
                using (var db = new SqlConnection(Shared.connString))
                {
                    orders = db.Query<Order>(query).ToList();
                }

                if (orders.Count == 0)
                {
                    return Ok(new { message = "No orders were found to be processed." });
                }

                string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
                string UpdateErrors = "";
                if (string.IsNullOrEmpty(connectId))
                {
                    return StatusCode(500, new { error = "Fincon Server unavailable." });
                }
                foreach (var order in orders)
                {
                    Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.OrgBranchID);
                    string confrimMail = branchDetail.BranchEMail;
                    string branchName = branchDetail.OrgBraShort;

                    Shared.FinconResult RESULT = await Shared.UpdateFincon(order.OrderID, connectId, FinconServerUsername, FinconServerPassword);
                    if (RESULT.Error)
                    {
                        if (RESULT.ErrorMessage == "Connection Error")
                        {
                            return StatusCode(500, new { error = "Fincon Server unavailable." });
                        }
                        else
                        {
                            UpdateErrors += RESULT.ErrorMessage;
                        }
                    }

                    if (!string.IsNullOrEmpty(RESULT.FinconId))
                    {
                        string notes = "ON#: Order Number: " + RESULT.FinconId;
                        string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + notes + "',DistOrdStatus=4 Where OrderID=" + order.OrderID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, order.CustID, FinconServerUsername, FinconServerPassword);

                        string terms = "";
                        string CreditAvailable = "0.00";
                        if (BillingDetail != null)
                        {
                            terms = BillingDetail.Value.Terms;
                            CreditAvailable = BillingDetail.Value.CreditAvailable;
                        }
                        Customer customer = Shared.GetCustomer(order.CustID);
                        List<string> emails = PrepareEmail(Convert.ToInt64(order.OrderID), RESULT.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
                        string emailbody = emails[0];
                        string doc = emails[1];

                        string subject = "Order Number: " + RESULT.FinconId + " From Esquire Technologies has been processed.";
                        string[] toEmail = new string[]
                         {
                        new(customer.Email)
                         };
                        List<string> bcc = new() { "test@esquire.co.za", confrimMail, "info@esquire.co.za" };

                        BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(RESULT.FinconId), "Order"));
                    }
                    else
                    {
                        return StatusCode(500, new { error = "Fincon Server unavailable." });
                    }
                }

                if (!string.IsNullOrEmpty(UpdateErrors))
                {
                    string AccountNo = Shared.GetCustomerAccountNo(Convert.ToInt64(l.CustID));
                    string finconsubject = "Warning : Couldn't update order to fincon server.";
                    string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                    string storeName = Shared.GetWebConfigKeyValue("SiteName");
                    List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                    string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + l.CustID + "<br />Error : " + UpdateErrors;
                    BackgroundJob.Enqueue(() => Shared.SendMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));
                    return StatusCode(500, new { error = UpdateErrors});
                }
                return Ok(new { message = "Orders processed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

}

