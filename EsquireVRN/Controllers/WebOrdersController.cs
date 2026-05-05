using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles =("Reseller"))]
    public class WebOrdersController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(long?page_number,long?page_size,string? search,string? StartDate,string?EndDate)
        {
            return Ok(Shared.GetWebOrders(page_number, page_size,search, StartDate, EndDate));
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            var order = Shared.GetOrder(id);
            if (order == null)
            {
                return NotFound(new { error = "Order doesn't exist." });
            }
            string PaymentDate = Shared.GetPaymentDate(id);
            var customer = Shared.GetCustomer(order.CustID);
            List<ResellerOrderItems> items = Shared.GetResellerOrderItems(id);
            List<OrderTracking> trackings = Shared.GetResellerOrderTracking(id);
            DeliveryAddress deliverAddress = Shared.GetDeliveryAddress(order.ShippingID);
            string PaymentReference = EncryptionService.EncryptString(order.OrderID + "-" + order.CustID);
            return Ok(new { OrderDetails = order, OrderItems = items, OrderTrackings = trackings, ShippingDetails = deliverAddress, CustomerDetail = customer, PaymentDate, PaymentReference });
        }

        [HttpGet]
        [Route("GetTracking")]
        public IActionResult GetOrderTracking(long OrderId)
        {
            List<OrderTracking> trackings = Shared.GetOrderTracking(OrderId);
            return Ok(trackings);
        }
    }
}
