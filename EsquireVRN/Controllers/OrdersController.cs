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
