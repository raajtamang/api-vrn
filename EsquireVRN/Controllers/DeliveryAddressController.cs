using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DeliveryAddressController : ControllerBase
    {
        // GET: api/<DeliveryAddressController>
        [HttpGet]
        public IActionResult Get()
        {
            if (User.Identity.IsAuthenticated)
            {
                long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);

                return Ok(Shared.GetDeliveryAddresses(CustId));
            }
            return StatusCode(401, new { error = "Authentication failed. Please login and try again." });
        }

        // GET: api/<DeliveryAddressController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            return Ok(Shared.GetDeliveryAddress(id));
        }

        // POST api/<DeliveryAddressController>
        [HttpPost]
        public IActionResult Post([FromBody] DeliveryAddress deliveryaddress)
        {
            if (User.Identity.IsAuthenticated)
            {
                long CustId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                deliveryaddress.CustID = CustId;
                DeliveryAddress DeliverAddress = Shared.SaveDeliveryAddress(deliveryaddress);
                return Ok(DeliverAddress);
            }
            return StatusCode(401, new { error = "Authentication failed. Please login and try again." });
        }

        // PUT api/<DeliveryAddressController>/5
        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody] DeliveryAddress deliveryAddress)
        {
            DeliveryAddress odAddress = Shared.GetDeliveryAddress(id);
            if (odAddress == null)
            {
                return StatusCode(400, new { error = "Delivery address doesn't exit." });
            }
            DeliveryAddress nDeliverAddress = Shared.UpdateDeliveryAddress(id, deliveryAddress);
            return Ok(new { DeliverAddress = nDeliverAddress, message = "Delivery Address udated successfully." });
        }

        // DELETE api/<DeliveryAddressController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            DeliveryAddress odAddress = Shared.GetDeliveryAddress(id);
            if (odAddress == null)
            {
                return StatusCode(400, new { error = "Delivery address doesn't exit." });
            }

            if (Shared.DeleteDeliveryAddress(id))
            {
                return Ok(new { DeliverAddress = odAddress, message = "Delivery Address removed successfully." });
            }
            else
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }

        }
    }
}
