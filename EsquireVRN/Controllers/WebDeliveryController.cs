using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="Reseller")]
    public class WebDeliveryController : ControllerBase
    {
        [HttpGet("GetDeliveryDescriptions")]
        public IActionResult GetDeliveryDescriptions()
        {
            return Ok(Shared.GetDeliveryDescription());
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(Shared.GetDeliveryMethods());
        }


        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            return Ok(Shared.GetDeliveryMethod(id));
        }

        [HttpPost]
        public IActionResult Post([FromBody] CreateDeliveryDTO request)
        {
            try
            {
                if (!Shared.CanSaveDeliveryAddress(request.DeliveryDescID))
                {
                    return StatusCode(500, new { error = "There already exists a delivery type with is description. Please check and try again." });

                }
                request.OrgID = Shared.GetOrgID();
                request.Area = Shared.GetDeliveryAddressArea(request.DeliveryDescID);
                return Ok(Shared.SaveWebDelivery(request));
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Put(long id, [FromBody] CreateDeliveryDTO request)
        {
            try
            {
                var oldDelivery = Shared.GetDeliveryMethod(id);
                if (oldDelivery == null)
                {
                    return NotFound(new { error = "Delivery Method doesn't exist. Please check and try again." });
                }
                request.OrgID = Shared.GetOrgID();
                request.Area = Shared.GetDeliveryAddressArea(request.DeliveryDescID);
                return Ok(Shared.UpdateWebDelivery(id, request));
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            try
            {
                var oldDelivery = Shared.GetDeliveryMethod(id);
                if (oldDelivery == null)
                {
                    return NotFound(new { error = "Delivery Method doesn't exist. Please check and try again." });
                }
                if (!Shared.CanDeleteDeliveryAddress(id))
                {
                    return StatusCode(500, new { error = "There are order records related to this delivery address. Please remove all the orders before proceeding." });
                }
                Shared.DeleteWebDelivery(id);
                return Ok(new { message = "Delivery Address removed successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong. Please try again." });
            }
        }
    }

}