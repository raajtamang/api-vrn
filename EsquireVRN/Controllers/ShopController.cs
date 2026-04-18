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
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("UpdateUserId")]
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Customer")]
        public IActionResult GetClientCartItems()
        {
            long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            List<CartItem> cartItems = Shared.GetCartItemsByCustId(CustId);
            return Ok(cartItems);
        }

        [HttpPut]
        [Route("UpdateCart")]
        [Authorize(Roles = "Customer")]
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
                string OutOfStockProducts = Shared.CheckStock(custId);
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

                List<InactiveProducts> iProducts = [];
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
            catch
            {
                return StatusCode(500, new { error = "Something went wrong with server. Please try again." });
            }

        }



        [HttpPost]
        [Route("Confirm")]
        [Authorize(Roles = "Customer")]
        public IActionResult Confirm(ConfirmModel confirmModel)
        {
            SqlConnection Conn = new SqlConnection(Shared.connString);
            try
            {
                Shared.DeliveryDetails details = Shared.getDeliveryDescID(confirmModel.DeliveryType);
                string strPaymentID = "" + confirmModel.PaymentId;
                string strShipID = "" + confirmModel.ShippingId;

                string strBraID = "" + confirmModel.NearestBranchId;
                string strOrdID = "";
                string strCost = "0.00";
                string custRef = "";
                string strShippingInstruction = "";
                if (!string.IsNullOrEmpty(confirmModel.ShippingInstruction))
                {
                    strShippingInstruction = confirmModel.ShippingInstruction;
                }
                if (confirmModel.DeliveryCharge > 0)
                {
                    strCost = confirmModel.DeliveryCharge.ToString("0.00").Replace(",", ".");
                }

                if (!string.IsNullOrWhiteSpace(confirmModel.CustRef))
                {
                    custRef = confirmModel.CustRef.Replace("'", "''");
                }
                string strDeliveryQuoteId = confirmModel.DeliveryQuoteId;


                string strSQL = "";
                long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                strSQL = @"SELECT ResellerWEBBasket.*, Products.PurchasePrice 
                            FROM ResellerWEBBasket INNER JOIN Products ON ResellerWEBBasket.ProdID = Products.ProdID
                            WHERE ResellerWEBBasket.OrgID=" + Shared.GetOrgID() + " AND ResellerWEBBasket.CustId=" + CustomerID;
                DataTable dtBasket = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, Shared.connString))
                {
                    adapter.Fill(dtBasket);
                }
                if (dtBasket.Rows.Count == 0)
                {
                    return StatusCode(500, new { error = "No items in the basket." });
                }
               

                string notes = "";

                if (confirmModel.ShippingId == 0)
                {
                    strShipID = Shared.GetShippingId(CustomerID);

                }
                decimal discount = 0;
                if (confirmModel.Discount > 0)
                {
                    discount = Convert.ToDecimal(confirmModel.Discount);
                }
                strSQL = "INSERT INTO ResellerOrders (CustomerID, DeliveryMethod, DeliveryDescID, DeliveryCost, PayID, " +
                 "ShippingID, OrgID, NearestBranchId, DeliveryQuoteID, Notes, Discount,DiscountVoucher,DeliveryID,ShippingInstruction,StatusID,DateCreated) VALUES " +
                 "(" + CustomerID + ", N'" +
                 details.DeliveryDesc.Replace("'", "''") + "'," +
                 details.DeliveryDescID.ToString() + "," + strCost + "," + confirmModel.PaymentId + "," + strShipID + "," + Shared.GetOrgID() +
                 "," + strBraID + ",N'" + strDeliveryQuoteId + "', N'" + notes + "'," +
                discount + ",N'" + confirmModel.DiscountVoucher + "',N'" + details.DeliveryID + "',N'" + strShippingInstruction + "',2,N'"+DateTime.Now+"'); SELECT SCOPE_IDENTITY();";
                if (Conn.State == ConnectionState.Closed)
                {
                    Conn.Open();
                }
                Shared.UpdateCartPrice(CustomerID);

                List<CartItem> OItems = Shared.GetCartItemsByCustId(CustomerID);

                using (var db = new SqlConnection(Shared.connString))
                {
                    strOrdID = db.Query<string>(strSQL).FirstOrDefault();
                }
                Shared.UpdateResellerOrderStatus(Convert.ToInt64(strOrdID), 2, "" + CustomerID);

                StringBuilder sbSql = new StringBuilder();
                double TotalAmount = 0;
                foreach (var detail in OItems)
                {
                    sbSql.Append(@"INSERT INTO ResellerOrderItems (ResellerOrderID, ProdID, ProdQty, Price, ProdDesc, ProdCode)
                                VALUES (" + Shared.Val(strOrdID) + "," + detail.ProdID + "," +
                        detail.ProdQty + "," + detail.Price.ToString("0.00").Replace(",", ".") +
                        ",'" + detail.ProdDesc.Replace("\'", "\'\'") + "','" +
                        detail.ProdCode.Replace("\'", "\'\'") + "');");
                    TotalAmount += Convert.ToDouble(Math.Round((Math.Round(detail.Price, 2) * detail.ProdQty), 2));
                }
                if (confirmModel.DeliveryCharge > 0)
                {
                    TotalAmount += Math.Round((Convert.ToDouble(confirmModel.DeliveryCharge)), 2);
                }
                sbSql.Append(@"Update ResellerOrders Set TotalAmountExcl=" + (TotalAmount / 1.15) + " Where ResellerOrderID=" + strOrdID);
                if (sbSql.ToString().Length > 5)
                {
                    using (var db = new SqlConnection(Shared.connString))
                    {
                        db.Execute(sbSql.ToString());
                    }

                }

                long OrderId = Convert.ToInt64(strOrdID);
                Shared.BranchDetail branchDetail = Shared.getBranchName("" + confirmModel.NearestBranchId);
                string confrimMail = branchDetail.BranchEMail;
                string branchName = branchDetail.OrgBraShort;

                Shared.ClearCartWithCustId(CustomerID);

                Customer customer = Shared.GetCustomer(CustomerID);
                List<string> emails = PrepareResellerOrderEmail(Convert.ToInt64(strOrdID), customer);
                string emailbody = emails[0];
                string doc = emails[1];

                string subject = "Order Confirmation";
                string[] toEmail =
                 [
                    new(customer.Email)
                 ];
                List<string> bcc = [confrimMail, "4me.suren@gmail.com"];

                BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(OrderId), "Order"));

                return Ok(new { message = "Order confirmed successfully." });
            }
            catch (Exception Excp)
            {
                int linenumber = (new StackTrace(Excp, true)).GetFrame(0).GetFileLineNumber();
                Serilog.Log.Error(Excp.Message + " at line number " + linenumber);
                return StatusCode(500, new { error = "Something went wrong with the server. Please try again in a few minutes." });

            }

        }


        [HttpGet("RecordPayment")]
        public IActionResult PaymentReceived(long OrderId, int PaymentId)
        {
            try
            {
                var order = Shared.GetResellerOrder(OrderId);
                double deliveryCharge = 0;
                if (order == null)
                {
                    return NotFound(new { error = "Order doesn't exist. Please check and try again." });
                }

                if (order.DeliveryCost > 0)
                {
                    deliveryCharge = Convert.ToDouble(order.DeliveryCost);
                }
                var customer = Shared.GetCustomer(order.CustomerID);
                if (customer == null)
                {
                    return NotFound(new { error = "Customer doesn't exist. Please check and try again." });
                }

                string query = "UPDATE ResellerOrders SET PayId=" + PaymentId + ",PaymentDate=N'" + DateTime.Now + "' WHERE ResellerOrderId=" + OrderId;
                using var db = new SqlConnection(Shared.connString);
                db.Execute(query);

                Shared.UpdateResellerOrderStatus(order.ResellerOrderID, 3, "" + order.CustomerID);

                Shared.DeliveryDetails details = Shared.getDeliveryDescID(order.DeliveryDescID);
                string BillBody = "";
                double orderAmount = 0, taxAmount = 0;
                string currencyFormat = Shared.GetWebConfigKeyValue("CurrencyFormat");

                List<ResellerOrderItems> items = Shared.GetResellerOrderItems(OrderId);
                foreach (var detail in items)
                {
                    BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><p style='white-space:pre-wrap;max-width: 650px;'>" + detail.ProdDesc + "</p></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + detail.ProdQty + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + detail.Price + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price * detail.ProdQty), 2).ToString(currencyFormat) + "</td></tr>";
                    orderAmount += (detail.Price / 1.15) * detail.ProdQty;
                    taxAmount += (detail.Price / 1.15) * detail.ProdQty * 0.15;
                }

                if (deliveryCharge > 0)
                {
                    taxAmount += (deliveryCharge / 1.15) * deliveryCharge * 0.15;
                    BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><div>Courier with Courier Direct:&nbsp;</div><div>Print out the <a href='" + order.DeliveryQuoteID + "' target='_blank'>Waybill - " + order.DeliveryQuoteID + "</a></div></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>1</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td></tr>";
                }

                string emailbody = "";
                string deliveryAddress = "";

                if ("" + order.DeliveryDescID == Shared.CD_DESC_ID)
                {
                    DeliveryAddress dAddress = Shared.GetDeliveryAddress(order.ShippingID);

                    if (dAddress != null)
                    {
                        deliveryAddress = dAddress.ShippingAddress + ", " + dAddress.Town + ", " + dAddress.ShippingCountry;
                    }
                }
                else
                {
                    deliveryAddress = "Collect From Shop";
                    if (order.DeliveryDescID == Shared.OWN_COURIER_TO_COLLECT)
                    {
                        deliveryAddress = "Own Courier To Collect";
                    }
                }

                string tAmount = Math.Round((orderAmount + deliveryCharge), 2).ToString(currencyFormat);
                string MailFormat = Shared.GetWebConfigKeyValue("ResellerPaymentMail");
                emailbody = MailFormat.Replace("{customer_name}", customer.FirstName + ' ' + customer.Surname).Replace("customer_email", customer.Email).Replace("{invoice_no}", order.ResellerOrderID.ToString("D8")).Replace("{invoice_date}", DateTime.Now.ToString("yyyy/MM/dd")).Replace("{payment_method}", "EFT").Replace("{invoice_items}", BillBody).Replace("{subtotal}", orderAmount.ToString("00")).Replace("{tax}", taxAmount.ToString("00")).Replace("{shipping}", (deliveryCharge / 1.15).ToString("00")).Replace("{total}", tAmount).Replace("{shipping_address}", deliveryAddress).Replace("{orgname}", Shared.GetOrgName()).Replace("{website}", Shared.GetWebConfigKeyValue("Website")).Replace("{logo_url}", Shared.GetOrgLogo());

                Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.NearestBranchId);
                string confrimMail = branchDetail.BranchEMail;
                string branchName = branchDetail.OrgBraShort;

                string subject = "Invoice";
                string[] toEmail =
                 [
                    new(customer.Email)
                 ];
                List<string> bcc = ["4me.suren@gmail.com", confrimMail];

                BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, "", Convert.ToString(OrderId), "Order"));

                return Ok(new { message = "Order status updated to paid successfully." });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong. Please try again." });
            }
        }

        [HttpGet("CancelOrder")]
        public IActionResult CancelOrder(long OrderId, string rejection_reason)
        {
            try
            {
                var order = Shared.GetResellerOrder(OrderId);
                if (order == null)
                {
                    return NotFound(new { error = "Order doesn't exist. Please check and try again." });
                }
                double deliveryCharge = 0;
                if (order.DeliveryCost > 0)
                {
                    deliveryCharge = Convert.ToDouble(order.DeliveryCost);
                }
                var customer = Shared.GetCustomer(order.CustomerID);
                if (customer == null)
                {
                    return NotFound(new { error = "Customer doesn't exist. Please check and try again." });
                }

                string query = "UPDATE ResellerOrders SET Rejected=1,Rejection_Reason=@reason WHERE ResellerOrderId=" + OrderId;
                using var db = new SqlConnection(Shared.connString);
                db.Execute(query, new { reason = rejection_reason });

                Shared.UpdateResellerOrderStatus(order.ResellerOrderID, 7, "" + customer.CustID);

                string SupportUrl = Shared.GetWebConfigKeyValue("SupportUrl");


                string emailbody = "";

                List<ResellerOrderItems> items = Shared.GetResellerOrderItems(OrderId);
                string BillBody = "";
                double orderAmount = 0;
                string currencyFormat = Shared.GetWebConfigKeyValue("CurrencyFormat");
                double tAmount = 0;
                foreach (var detail in items)
                {
                    BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><p style='white-space:pre-wrap;max-width: 650px;'>" + detail.ProdDesc + "</p></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + detail.ProdQty + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + detail.Price + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price * detail.ProdQty), 2).ToString(currencyFormat) + "</td></tr>";
                    orderAmount += Math.Round((Math.Round(detail.Price, 2) * detail.ProdQty), 2);
                }
                if (deliveryCharge > 0)
                {
                    BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><div>Courier with Courier Direct:&nbsp;</div><div>Print out the <a href='" + order.DeliveryQuoteID + "' target='_blank'>Waybill - " + order.DeliveryQuoteID + "</a></div></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>1</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td></tr>";
                }
                tAmount = orderAmount + deliveryCharge;
                string MailFormat = Shared.GetWebConfigKeyValue("OrderCancellationMail");
                emailbody = MailFormat.Replace("{orgname}", Shared.GetOrgName()).Replace("{customer_name}", customer.FirstName + " " + customer.Surname).Replace("{order_id}", order.ResellerOrderID.ToString("D8")).Replace("{order_date}", order.DateCreated.ToString("yyyy/MM/dd")).Replace("{cancel_reason}", rejection_reason).Replace("{support_link}", SupportUrl).Replace("{year}", DateTime.Now.ToString("yyyy")).Replace("{canceled_items}", BillBody).Replace("{subtotal}", "" + orderAmount).Replace("{shipping}", "" + deliveryCharge).Replace("{total}", "" + tAmount);

                Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.NearestBranchId);
                string confrimMail = branchDetail.BranchEMail;
                string branchName = branchDetail.OrgBraShort;

                string subject = "Order Canceled";
                string[] toEmail =
                 [
                    new(customer.Email)
                 ];
                List<string> bcc = ["test@esquire.co.za", confrimMail, "info@esquire.co.za"];

                BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, "", Convert.ToString(OrderId), "Order"));

                return Ok(new { message = "Order status updated to paid successfully." });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Something went wrong. Please try again." });
            }
        }

        private List<string> PrepareResellerOrderEmail(long orderId, Customer customer)
        {
            var requestUrl = $"{Request.Scheme}://{Request.Host.Value}/api/Shop";
            var order = Shared.GetResellerOrder(orderId);
            Shared.DeliveryDetails details = Shared.getDeliveryDescID(order.DeliveryDescID);
            string AccountNumber = Shared.GetAccountNumber(order.CustomerID);
            double deliveryCharge = Math.Round(Convert.ToDouble(order.DeliveryCost), 2);
            List<ResellerOrderItems> items = Shared.GetResellerOrderItems(orderId);
            string BillBody = "", PdfBody = "";
            double orderAmount = 0;
            Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.NearestBranchId);
            string currencyFormat = Shared.GetWebConfigKeyValue("CurrencyFormat");
            string confrimMail = branchDetail.BranchEMail;

            foreach (var detail in items)
            {
                BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><p style='white-space:pre-wrap;max-width: 650px;'>" + detail.ProdDesc + "</p></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + Math.Round(detail.Price, 2).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>" + detail.ProdQty + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + Math.Round((detail.Price * detail.ProdQty), 2).ToString(currencyFormat) + "</td></tr>";
                orderAmount += Math.Round((Math.Round(detail.Price, 2) * detail.ProdQty), 2);
            }
            string strDeliveryQuoteId = requestUrl + "/GetWayBill" + "?c=" + order.DeliveryQuoteID;
            string paySlipUrl = requestUrl + "/GetPackingSlip" + "?o=" + orderId + "&c=" + AccountNumber;
            if (deliveryCharge > 0)
            {
                BillBody += "<tr><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'><div>Courier with Courier Direct:&nbsp;</div><div>Print out the <a href='" + strDeliveryQuoteId + "' target='_blank'>Waybill - " + order.DeliveryQuoteID + "</a></div></td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>1</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td><td style='white-space: nowrap;padding: 10px;border-bottom: 2px solid #077ea2;'>R " + (deliveryCharge).ToString(currencyFormat) + "</td></tr>";
            }

            string pdfContent = "";
            string emailbody = "";
            if ("" + order.DeliveryDescID == Shared.CD_DESC_ID)
            {
                DeliveryAddress dAddress = Shared.GetDeliveryAddress(order.ShippingID);
                string deliveryAddress = "";
                if (dAddress != null)
                {
                    deliveryAddress = dAddress.ShippingAddress + ", " + dAddress.Town + ", " + dAddress.ShippingCountry;
                }

                string MailFormat = Shared.GetWebConfigKeyValue("ResellerOrderConfirmMail");
                string tAmount = Math.Round((orderAmount + deliveryCharge), 2).ToString(currencyFormat);
                emailbody = MailFormat.Replace("{customer_name}", customer.FirstName + ' ' + customer.Surname).Replace("{order_id}", order.ResellerOrderID.ToString("D8")).Replace("{order_date}", order.DateCreated.ToString("yyyy/MM/dd")).Replace("{payment_method}", "EFT").Replace("{order_items}", BillBody).Replace("{subtotal}", orderAmount.ToString("00")).Replace("{shipping}", deliveryCharge.ToString("00")).Replace("{total}", tAmount).Replace("{shipping_address}", deliveryAddress).Replace("{orgname}", Shared.GetOrgName()).Replace("{website}", Shared.GetWebConfigKeyValue("Website"));
            }
            else
            {
                string deliveryAddress = "Collect From Shop";
                if (order.DeliveryDescID == Shared.OWN_COURIER_TO_COLLECT)
                {
                    deliveryAddress = "Own Courier To Collect";
                }
                string MailFormat = Shared.GetWebConfigKeyValue("ResellerOrderConfirmMail");

                string oAmount = orderAmount.ToString(currencyFormat);
                emailbody = MailFormat.Replace("{customer_name}", customer.FirstName + ' ' + customer.Surname).Replace("{order_id}", order.ResellerOrderID.ToString("D8")).Replace("{order_date}", order.DateCreated.ToString("yyyy/MM/dd")).Replace("{payment_method}", "EFT").Replace("{order_items}", BillBody).Replace("{subtotal}", oAmount).Replace("{shipping}", deliveryCharge.ToString("00")).Replace("{total}", oAmount).Replace("{shipping_address}", deliveryAddress).Replace("{orgname}", Shared.GetOrgName()).Replace("{website}", Shared.GetWebConfigKeyValue("Website")).Replace("{year}",DateTime.Now.ToString("yyyy"));

            }
            List<string> returnString = new()
            {
                emailbody,
                pdfContent
            };
            double eTotal = orderAmount + deliveryCharge;
            Serilog.Log.Error("Order No. : " + order.ResellerOrderID + " : Email Total Price : " + eTotal);
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
        [Route("ResendEmail/{id}")]
        [Authorize]
        public IActionResult ResendEmail(long id, string? email)
        {
            var order = Shared.GetResellerOrder(id);

            if (order == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Order doesn't exist." });
            }
            var cust = Shared.GetCustomer(order.CustomerID);
            if (cust == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new { error = "Customer doesn't exist." });
            }
          
            Shared.BranchDetail branchDetail = Shared.getBranchName("" + order.NearestBranchId);
            string confrimMail = branchDetail.BranchName;

            List<string> emails = PrepareResellerOrderEmail(order.ResellerOrderID, cust);
            string emailbody = emails[0];
            string doc = emails[1];

            string subject = "Re : Order Confirmation";
            if (string.IsNullOrEmpty(email))
            {
                email = cust.Email;
            }
            string[] toEmail =
            [
                        new(email)
            ];
            List<string> bcc = [];

            bcc = [confrimMail,"4me.suren@gmail.com"];
            BackgroundJob.Enqueue(() => Shared.SendMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(order.ResellerOrderID), "Order"));

            //doc.Close();
            return StatusCode(200, new { message = "Email resent successfully" });
        }
    }

}

