using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(int? page_number, int? page_size, string? search, string? start_date, string? end_date)
        {

            if (User.IsInRole("Customer"))
            {
                long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                var orders = Shared.GetPagedCustomerOrders(CustomerID, page_number, page_size, search, start_date, end_date);
                return Ok(orders);
            }
            else
            {
                var orders = Shared.GetResellerOrders(page_number, page_size, search, start_date, end_date);
                return Ok(orders);
            }

        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var order = Shared.GetResellerOrder(id);
            if (order == null)
            {
                return NotFound(new { error = "Order doesn't exist." });
            }
            string PaymentDate = Shared.GetPaymentDate(id);
            var customer = Shared.GetCustomer(order.CustomerID);
            List<ResellerOrderItems> items = Shared.GetResellerOrderItems(id);
            List<OrderTracking> trackings = Shared.GetResellerOrderTracking(id);
            DeliveryAddress deliverAddress = Shared.GetDeliveryAddress(order.ShippingID);
            string PaymentReference = EncryptionService.EncryptString(order.ResellerOrderID + "-" + order.CustomerID);
            return Ok(new { OrderDetails = order, OrderItems = items, OrderTrackings = trackings, ShippingDetails = deliverAddress, CustomerDetail = customer, PaymentDate, PaymentReference });
        }

        [HttpGet]
        [Route("GetOrderTracking")]
        public IActionResult GetOrderTracking(long OrderId)
        {
            List<OrderTracking> trackings = Shared.GetResellerOrderTracking(OrderId);
            return Ok(trackings);
        }

        [HttpGet]
        [Route("PaymentTypes")]
        [Authorize]
        public IActionResult GetPaymentTypes()
        {
            List<Shared.PaymentMethod> paymentMethods = Shared.GetPaymentMethod();
            return Ok(paymentMethods);
        }

        [Authorize(Roles = "Reseller")]
        [HttpPost("UpdateStatus")]
        public IActionResult UpdateOrderStatus([FromBody] UpdateOrderStatusDTO req)
        {
            try
            {
                string[] statusList = ["Basket", "Awaiting proof of payment", "Payment received", "Order assembly", "Shipping", "Ready for collection", "Collected", "Delivered", "Declined", "Cancelled"];

                long userId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                Shared.ChangeResellerOrderStatus(req.ResellerOrderID, req.StatusId, userId);
                var oldOrder = Shared.GetResellerOrder(req.ResellerOrderID);
                if (oldOrder == null)
                {
                    return NotFound(new { error = "Order doesn't exist. Please check and try again." });
                }
                OrderStatusEmailModel model = Shared.GetStatusEmail(req.StatusId);
                if (model != null)
                {
                    Shared.BranchDetail branchDetail = Shared.getBranchName("" + oldOrder.NearestBranchId);
                    string confrimMail = Shared.GetWebConfigKeyValue("AdminEmail");
                    string branchName = branchDetail.OrgBraShort;
                    var custome = Shared.GetCustomer(oldOrder.CustomerID);
                    if (custome == null)
                    {
                        return NotFound(new { error = "Customer doesn't exist. Please check and try again." });

                    }
                    string paymentType = Shared.GetpaymentType(oldOrder.PayID ?? 0);
                    string header = model.Header.Replace("{OrderNumber}", "" + oldOrder.ResellerOrderID).Replace("{NewStatus}", statusList[req.StatusId - 1]);
                    string body = model.Detail.Replace("{CustomerTitle}", custome.Title).Replace("{CustomerFirstName}", custome.FirstName).Replace("{CustomerSurname}", custome.Surname).Replace("{NewStatus}", statusList[req.StatusId - 1]).Replace("{OrderNumber}", "" + oldOrder.ResellerOrderID).Replace("{OrderDate}", oldOrder.DateCreated.ToString("yyyy/MM/dd hh:mm:ss tt")).Replace("{OrderPayMethod}", paymentType).Replace("{OrderDelivery}", oldOrder.DeliveryMethod).Replace("{FromOrgName}", Shared.GetOrgName());

                    string[] toEmail = [new(custome.Email)];
                    List<string> bcc = ["4me.suren@gmail.com", confrimMail];

                    BackgroundJob.Enqueue(() => Shared.SendMail(header, body, toEmail, confrimMail, bcc, false));
                }
                return Ok(new { message = "Order status updated to " + statusList[req.StatusId - 1] });
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }

        [HttpGet]
        [Route("OrderStatus")]
        public IActionResult GetOrderStatus()
        {
            List<Shared.OrderStatus> orderStatus = Shared.GetOrderStaus();
            return Ok(orderStatus);
        }

        [HttpPost("ConvertToOrder")]
        [Authorize(Roles = "Reseller")]
        public async Task<IActionResult> ConvertToOrder([FromBody] ConvertResellerOrderToOrderModel model)
        {
            var quotations = Shared.GetResellerOrder(model.ResellerOrderId);
            if (quotations == null)
            {
                return StatusCode(404, new { error = "Quotation doesn't exist" });
            }
            long userId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            List<ResellerOrderItems> quotationDetails = Shared.GetResellerOrderItems(model.ResellerOrderId);
            if (!quotationDetails.Any())
            {
                return StatusCode(400, new { error = "Quotation has no items added. Please add some items first." });
            }
            string error = Shared.CheckStock(userId);
            if (!string.IsNullOrEmpty(error))
            {
                return StatusCode(400, new { error });
            }
            string DeliveryType = "Collect from shop";
            if (quotations.DeliveryCost > 0)
            {
                DeliveryType = "Courier Direct";
            }
            try
            {
                string strOrdID = "";
                long deliveryId = Shared.getDeliveryID(DeliveryType);
                Shared.DeliveryDetails details = Shared.getDeliveryDescID(deliveryId);
                decimal bundleDiscount = (quotations.Discount ?? 0);
                decimal deliveryCost = 0;
                if (quotations.DeliveryCost != null)
                {
                    deliveryCost = Convert.ToDecimal(quotations.DeliveryCost);
                }

                string strSQL = "INSERT INTO WEBOrders (CustID, DeliveryMethod, DeliveryDescID, DeliveryCost, PayID, " +
                     "ShippingID, StatusID, OrgID, OrgBranchID, DeliveryQuoteID, DistOrdStatus, CustRef, Notes, Discount,DeliveryId,ShippingInstruction) OUTPUT inserted.OrderID VALUES " +
                     "(" + userId + ", N'" +
                     details.DeliveryDesc.Replace("'", "''") + "'," +
                     details.DeliveryDescID.ToString() + "," + deliveryCost + "," + model.PaymentId + "," + quotations.ShippingID + ",2," + Shared.GetOrgID() +
                     "," + quotations.NearestBranchId + ",N'" + quotations.DeliveryQuoteID + "',1 ,N'', N'" + quotations.Notes + "'," +
                     bundleDiscount.ToString("0.00").Replace(",", ".") + "," + details.DeliveryID + ",N'" + quotations.ShippingInstruction + "'); SELECT SCOPE_IDENTITY();";
                double TotalAmount = 0;

                using (var db = new SqlConnection(Shared.connString))
                {
                    strOrdID = db.Query<string>(strSQL).FirstOrDefault();
                    StringBuilder sbSql = new StringBuilder();
                    foreach (var item in quotationDetails)
                    {
                        double price = Shared.GetPriceByProductId(item.ProdID);
                        sbSql.Append(@"INSERT INTO WEBOrderItems (OrderID, ProdID, ProdQty, Price, ProdDesc, ProdCode)
                                VALUES (" + strOrdID + "," + item.ProdID + "," +
                            item.ProdQty + "," + Convert.ToDouble(price).ToString("0.00").Replace(",", ".") +
                            ",'" + item.ProdDesc.Replace("\'", "\'\'") + "','" +
                            item.ProdCode.Replace("\'", "\'\'") + "');");
                        TotalAmount += Math.Round((Math.Round(Convert.ToDouble(price), 2) * Convert.ToDouble(item.ProdQty)), 2);
                    }
                    if (quotations.DeliveryCost > 0)
                    {
                        TotalAmount += Math.Round((Convert.ToDouble(quotations.DeliveryCost)), 2);
                    }
                    if (sbSql.ToString().Length > 5)
                    {

                        db.Execute(sbSql.ToString());
                    }
                }
                Shared.UpdateOrderStatus(Convert.ToInt64(strOrdID), 2, "" + userId);
                Shared.UpdateResellerOrderStatus(strOrdID, model.ResellerOrderId);
                long OrderId = Convert.ToInt64(strOrdID);
                Shared.BranchDetail branchDetail = Shared.getBranchName("" + quotations.NearestBranchId);
                string confrimMail = branchDetail.BranchEMail;
                string branchName = branchDetail.OrgBraShort;
                string custID = "" + userId;

                if (model.PaymentId == Shared.PAY_ID_ELECTRONIC_TRANSFER)
                {
                    string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                    string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                    string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                    string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);
                    if (string.IsNullOrEmpty(connectId))
                    {
                        Customer tempcustomer = Shared.GetCustomer(Convert.ToInt64(userId));
                        quotations.Notes = "";
                        string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + quotations.Notes + "' Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearQuotationCartWithCustId(custID);

                        string finconsubject = "Order Confirmation and Processing Update";
                        List<string> Emails = new() { tempcustomer.Email };
                        List<string> cc = new() { "syanthan1st@gmail.com", confrimMail };
                        string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                        BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za ", false));


                        return StatusCode(200, new { message = "Order confirmed successfully." });
                    }

                    Shared.FinconResult RESULT = await Shared.UpdateFincon(OrderId, connectId, FinconServerUsername, FinconServerPassword);
                    if (RESULT.Error)
                    {
                        if (RESULT.ErrorMessage == "Connection Error")
                        {
                            Customer tempcustomer = Shared.GetCustomer(Convert.ToInt64(userId));
                            string tempnotes = "";
                            string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + tempnotes + "' Where OrderID=" + strOrdID;
                            using (var db = new SqlConnection(Shared.connString))
                            {
                                db.Execute(sql);
                            }


                            string finconsubject = "Order Confirmation and Processing Update";
                            List<string> Emails = new() { tempcustomer.Email };
                            List<string> cc = new() { "syanthan1st@gmail.com", confrimMail };
                            string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                            BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, cc, "info@esquire.co.za ", false));



                            return StatusCode(200, new { message = "Order confirmed successfully." });
                        }
                        else
                        {
                            string AccountNo = Shared.GetCustomerAccountNo(Convert.ToInt64(userId));
                            string finconsubject = "Warning : Couldn't update order to fincon server.";
                            string storeName = Shared.GetWebConfigKeyValue("SiteName");
                            List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                            string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + custID + "<br />Error : " + RESULT.ErrorMessage;
                            BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za ", true));

                            return StatusCode(500, new { error = RESULT.ErrorMessage });
                        }
                    }

                    if (!string.IsNullOrEmpty(RESULT.FinconId))
                    {
                        quotations.Notes = "ON#: Order Number: " + RESULT.FinconId;
                        string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + quotations.Notes + "',DistOrdStatus=4 Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }


                        Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, Convert.ToInt64(userId), FinconServerUsername, FinconServerPassword);

                        string terms = "";
                        string CreditAvailable = "0.00";
                        if (BillingDetail != null)
                        {
                            terms = BillingDetail.Value.Terms;
                            CreditAvailable = BillingDetail.Value.CreditAvailable;
                        }
                        Customer customer = Shared.GetCustomer(Convert.ToInt64(userId));
                        List<string> emails = PrepareEmail(Convert.ToInt64(strOrdID), RESULT.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
                        string emailbody = emails[0];
                        string doc = emails[1];

                        string subject = "Order Number: " + RESULT.FinconId + " From Esquire Technologies has been processed.";
                        string[] toEmail = new string[]
                         {
                        new(customer.Email)
                         };
                        List<string> bcc = new() { "test@esquire.co.za", confrimMail };

                        BackgroundJob.Enqueue(() => Shared.SendEsquireMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(RESULT.FinconId), "Quotation"));


                        return Ok(new { message = "Order confirmed successfully." });
                    }
                    else
                    {
                        string AccountNo = Shared.GetCustomerAccountNo(Convert.ToInt64(userId));
                        string finconsubject = "Warning : Couldn't update order to fincon server.";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        string storeName = Shared.GetWebConfigKeyValue("SiteName");
                        List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                        string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + userId + "<br />Error : " + RESULT.ErrorMessage;
                        BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));

                        return StatusCode(500, new { error = RESULT.ErrorMessage });
                    }

                }
                else if (model.PaymentId == Shared.COLLECT_AND_PAY_AT_SHOP)
                {
                    string FinconUrl = Shared.GetWebConfigKeyValue("FinconUrl");
                    string FinconServerUsername = Shared.GetWebConfigKeyValue("FinconServerUsername");
                    string FinconServerPassword = Shared.GetWebConfigKeyValue("FinconServerPassword");
                    string connectId = await Shared.GetConnectID(FinconUrl, FinconServerUsername, FinconServerPassword);

                    if (string.IsNullOrEmpty(connectId))
                    {
                        Customer tempcustomer = Shared.GetCustomer(Convert.ToInt64(userId));
                        quotations.Notes = "";
                        string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + quotations.Notes + "' Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearQuotationCartWithCustId(custID);

                        string finconsubject = "Order Confirmation and Processing Update";
                        List<string> Emails = new() { tempcustomer.Email };
                        List<string> cc = new() { "syanthan1st@gmail.com", confrimMail };
                        string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                        BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, cc, "quote@esquire.co.za ", false));



                        return StatusCode(200, new { message = "Order confirmed successfully." });
                    }

                    Shared.FinconResult RESULT = await Shared.UpdateFincon(OrderId, connectId, FinconServerUsername, FinconServerPassword);
                    if (RESULT.Error)
                    {
                        if (RESULT.ErrorMessage == "Connection Error")
                        {
                            Customer tempcustomer = Shared.GetCustomer(Convert.ToInt64(userId));
                            quotations.Notes = "";
                            string sql = "Update WebOrders Set FinconId=-1,Notes=N'" + quotations.Notes + "' Where OrderID=" + strOrdID;
                            using (var db = new SqlConnection(Shared.connString))
                            {
                                db.Execute(sql);
                            }

                            Shared.ClearQuotationCartWithCustId(custID);

                            string finconsubject = "Order Confirmation and Processing Update";
                            List<string> Emails = new() { tempcustomer.Email };
                            List<string> cc = new() { "syanthan1st@gmail.com", confrimMail };
                            string finconemailbody = Shared.GetWebConfigKeyValue("OrderReceived").Replace("{title}", tempcustomer.Title).Replace("{firstname}", tempcustomer.FirstName).Replace("{surname}", tempcustomer.Surname);
                            BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, cc, "quote@esquire.co.za ", false));



                            return StatusCode(200, new { message = "Order confirmed successfully." });
                        }
                        else
                        {
                            string AccountNo = Shared.GetCustomerAccountNo(Convert.ToInt64(userId));
                            string finconsubject = "Warning : Couldn't update order to fincon server.";
                            string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                            string storeName = Shared.GetWebConfigKeyValue("SiteName");
                            List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                            string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + userId + "<br />Error : " + RESULT.ErrorMessage;
                            BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));

                            return StatusCode(500, new { error = RESULT.ErrorMessage });
                        }
                    }

                    if (!string.IsNullOrEmpty(RESULT.FinconId))
                    {
                        quotations.Notes = "ON#: Order Number: " + RESULT.FinconId;
                        string sql = "Update WebOrders Set FinconId=" + RESULT.FinconId + ",Notes=N'" + quotations.Notes + "',DistOrdStatus=4 Where OrderID=" + strOrdID;
                        using (var db = new SqlConnection(Shared.connString))
                        {
                            db.Execute(sql);
                        }

                        Shared.ClearQuotationCartWithCustId(custID);

                        Shared.BillingDetail? BillingDetail = await Shared.GetBillingDetail(connectId, Convert.ToInt64(userId), FinconServerUsername, FinconServerPassword);

                        string terms = "";
                        string CreditAvailable = "0.00";
                        if (BillingDetail != null)
                        {
                            terms = BillingDetail.Value.Terms;
                            CreditAvailable = BillingDetail.Value.CreditAvailable;
                        }
                        var customer = Shared.GetCustomer(Convert.ToInt64(userId));
                        List<string> emails = PrepareEmail(Convert.ToInt64(strOrdID), RESULT.FinconId, terms, CreditAvailable, Convert.ToInt64(customer.OrgID));
                        string emailbody = emails[0];
                        string doc = emails[1];
                        string finconId = Convert.ToInt64(RESULT.FinconId).ToString().PadLeft(8, '0');
                        string subject = "Order Number: " + finconId + " From Esquire Technologies has been processed.";
                        string[] toEmail =
                         [
                        new(customer.Email)
                         ];
                        List<string> bcc = ["test@esquire.co.za", confrimMail];

                        BackgroundJob.Enqueue(() => Shared.SendEsquireMail(subject, emailbody, toEmail, confrimMail, bcc, false, doc, Convert.ToString(RESULT.FinconId), "Quotation"));



                        return Ok(new { message = "Order confirmed successfully." });
                    }
                    else
                    {
                        string AccountNo = Shared.GetCustomerAccountNo(Convert.ToInt64(userId));
                        string finconsubject = "Warning : Couldn't update order to fincon server.";
                        string adminEmail = Shared.GetWebConfigKeyValue("AdminEmail");
                        string storeName = Shared.GetWebConfigKeyValue("SiteName");
                        List<string> Emails = new() { "nicholas@esquire.co.za", "mahomed@esquire.co.za", "kabir@esquire.co.za", "irfhan@esquire.co.za", "syanthan1st@gmail.com", "senzo@esquire.co.za", "khanyisa@esquire.co.za", "mccalvin@esuire.co.za", "tumelo@esquire.co.za", "prince@esquire.co.za", "mariamw@esquire.co.za" };
                        string finconemailbody = "<br /><br />Error occurred while sending order to fincon. Please check the fincon server. <br />Account Number : " + AccountNo + ".<br /><br />Customer Id : " + userId + "<br />Error : " + RESULT.ErrorMessage;
                        BackgroundJob.Enqueue(() => Shared.SendEsquireMailHangFire(finconsubject, finconemailbody, Emails, "info@esquire.co.za", true));

                        return StatusCode(500, new { error = RESULT.ErrorMessage });
                    }
                }
                else if (model.PaymentId == Shared.PAY_ID_CREDIT_CARD_INSTANT_EFT_MOBI_CREDIT)
                {
                    Serilog.Log.Error("Order No. : " + OrderId + " : Confirm Total Price : " + TotalAmount);
                    string refId = EncryptionService.EncryptString(OrderId + "-" + custID) + "!" + OrderId;
                    return Ok(new { message = "Order confirmed successfully. Please proceed to make payment.", reference = refId, TotalAmount, CustomerEmail = User.Identity.Name });
                }

                return Ok(new { message = "Order confirmed successfully." });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, ex.Message);
                return StatusCode(500, new { error = ex.Message });

            }
        }
        [HttpPost]
        [Route("MakePayment")]
        [Authorize(Roles = "Reseller")]
        public IActionResult MakePayment([FromBody] OrderRequest req)
        {
            long OrderId = req.OrderId;
            long userId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            string refId = EncryptionService.EncryptString(OrderId + "-" + userId) + "!" + OrderId;
            var order = Shared.GetOrder(OrderId);
            if (order == null)
            {
                return NotFound(new { error = "Order doesn't exist. Please check and try again." });
            }
            if (order.StatusID != 2 || order.PayID != Shared.PAY_ID_CREDIT_CARD_INSTANT_EFT_MOBI_CREDIT)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Invalid order." });
            }
            List<OrderItem> orderItems = Shared.GetOrderItems(OrderId);
            decimal TotalAmount = Convert.ToDecimal(orderItems.Sum(x => x.Price * x.ProdQty)) + Convert.ToDecimal(order.DeliveryCost) - Convert.ToDecimal(order.Discount);
            return Ok(new { message = "Please proceed to make payment.", reference = refId, TotalAmount, CustomerEmail = User.Identity.Name,OrderId });

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

        public class OrderRequest()
        {
            public required long OrderId { get; set; }
        }
    }
}
