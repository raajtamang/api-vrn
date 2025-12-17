using EsquireVRN.Models;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishListController : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = "Customer,Reseller")]
        public IActionResult Get()
        {
            long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
            return Ok(Shared.GetCustomerWishList(CustomerID));
        }

        // POST api/<WishListController>
        [HttpPost]
        [Authorize(Roles = "Customer,Reseller")]
        public IActionResult Post([FromBody] WishList wishList)
        {
            try
            {
                long CustomerID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value);
                wishList.CustID = CustomerID;
                wishList.CreationDate = DateTime.UtcNow.AddHours(2);
                WishList nWishList = Shared.SaveWishList(wishList);
                return Ok(nWishList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE api/<WishListController>/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer,Reseller,Admin")]
        public IActionResult Delete(long id)
        {
            WishList oldWishList = Shared.GetWishListDetail(id);
            if (oldWishList == null)
            {
                return NotFound(new { error = "Item doesn't exist in wishlist." });
            }
            try
            {
                if (Shared.DeleteWishList(id))
                {
                    return Ok(new { message = "Item removed from wish list removed successfully." });
                }
                else
                {
                    return StatusCode(500, new { error = "Item couldn't be added to wishlist. Please try again." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
