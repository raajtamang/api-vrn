using Dapper;
using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;


namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {       
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public IActionResult Get()
        {
            long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            List<Order> orders = Shared.GetCustomerOrders(CustomerID);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            Order order = Shared.GetOrder(id);
            if (order == null)
            {
                return NotFound(new { error = "Order doesn't exist." });
            }
            string PaymentDate = Shared.GetPaymentDate(id);
            var customer = Shared.GetCustomer(order.CustID);
            List<OrderItem> items = Shared.GetOrderItems(id);
            List<OrderTracking> trackings = Shared.GetOrderTracking(id);
            DeliveryAddress deliverAddress = Shared.GetDeliveryAddress(order.ShippingID);
            string PaymentReference = EncryptionService.EncryptString(order.OrderID + "-" + order.CustID);
            return Ok(new { OrderDetails = order, OrderItems = items, OrderTrackings = trackings, ShippingDetails = deliverAddress, CustomerDetail = customer, PaymentDate, PaymentReference });
        }

        [HttpGet]
        [Route("GetOrderTracking")]
        public IActionResult GetOrderTracking(long OrderId)
        {
            List<OrderTracking> trackings = Shared.GetOrderTracking(OrderId);
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

        [HttpGet]
        [Route("OrderStatus")]
        [Authorize]
        public IActionResult GetOrderStatus()
        {
            List<Shared.OrderStatus> orderStatus = Shared.GetOrderStaus();
            return Ok(orderStatus);
        }

    }
}
