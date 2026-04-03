using EsquireVRN.Models;
using EsquireVRN.Utils;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles ="Reseller")]
    public class UsersController : ControllerBase
    {
        // GET: api/<UsersController>
        [HttpGet]
        public IActionResult Get(int? page_number,int? page_size)
        {
            long AccountId = Convert.ToInt64(User.Claims.First(claim => claim.Type == "AcountId").Value);
            int pSize = (page_size ?? 12);
            int pNum=(page_number ?? 1);
            return Ok(Shared.GetCutomers(pNum,pSize,AccountId));
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            return Ok(Shared.GetCustomer(id));
        }

        // POST api/<UsersController>
        [HttpPost]
        public IActionResult Post([FromBody] Customer customer)
        {
            try
            {
                if (customer.AccountID == null)
                {
                    customer.AccountID = Convert.ToInt64(User.Claims.First(claim => claim.Type == "AcountId").Value);
                }
                
                customer.OrgID = Shared.GetOrgID();
                customer.SendEmails = 255;
                customer.DateCreated = DateTime.UtcNow.AddHours(2);
                customer.CommissionOnProfit = false;
                customer.Active = true;
                customer.IsCommissionActive = false;
                customer.UserType = "Customer";
                return Ok(Shared.AddCustomer(customer));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Customer customer)
        {
            Customer oldCustomer = Shared.GetCustomer(id);
            if (oldCustomer == null)
            {
                return StatusCode(404, new { error = "Customer doesn't exit. Please check and try again." });
            }
            try
            {
                if (customer.MarkFradulent != null)
                {

                    if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
                    {
                        if (customer.MarkFradulent == true)
                        {
                            if (oldCustomer.FraudulentUserID == null)
                            {
                                Shared.MarkFradulent(Convert.ToInt64(User.Claims.First(claim => claim.Type == "CustomerID").Value), id);
                            }
                        }
                        else
                        {
                            if (oldCustomer.FraudulentUserID != null)
                            {
                                Shared.MarkFradulent(null, id);
                            }
                        }

                    }
                    else
                    {
                        return StatusCode(401, new { error = "You are not authorized to mark user fradulent." });
                    }
                }
            }
            catch
            {

            }
            customer.UserType = "Customer";
            if (customer.DateCreated == null)
            {
                customer.DateCreated = oldCustomer.DateCreated;
            }
            if (string.IsNullOrWhiteSpace(customer.Company))
            {
                customer.Company = oldCustomer.Company;
            }
            Customer newCustomer = Shared.UpdateCustomer(id, customer);
            string emailBody = "";
            string subject = "";
            bool sendEmail = false;
            if (oldCustomer.Active == true && newCustomer.Active == false)
            {
                string EmailFormat = Shared.GetWebConfigKeyValue("AccountDeactivated");
                emailBody = EmailFormat.Replace("name", newCustomer.Title + " " + newCustomer.FirstName + " " + newCustomer.Surname);
                subject = "Your online account at Esquire Online Store is now deactivated!";
                sendEmail = true;

            }
            else if (oldCustomer.Active == false && newCustomer.Active == true)
            {
                string EmailFormat = Shared.GetWebConfigKeyValue("AccountActivated");
                emailBody = EmailFormat.Replace("{title}", oldCustomer.Title).Replace("{fname}", oldCustomer.FirstName).Replace("{sname}", oldCustomer.Surname).Replace("{url}", "https://esquire.co.za");
                subject = "Your online account at Esquire Online Store is now activated!";
                sendEmail = true;
            }
            if (sendEmail)
            {
                List<string> toEmail = new() { newCustomer.Email };
                string fromEmail = "noreply@esquire.co.za";
                BackgroundJob.Enqueue(() => Shared.SendMailHangFire(subject, emailBody, toEmail, fromEmail, false));
            }
            return Ok();
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            Customer oldCustomer = Shared.GetCustomer(id);
            if (oldCustomer == null)
            {
                return StatusCode(404, new { error = "Customer doesn't exit. Please check and try again." });
            }
            List<Order> custOrders = Shared.GetCustomerOrders(id);
            if (custOrders.Count() > 0)
            {
                return StatusCode(500, new { error = "No Delete (Orders against user)" });
            }
            bool delete = Shared.DeleteCustomer(id);
            if (delete)
            {
                return Ok(new { message = "Customer removed successfully." });
            }
            return StatusCode(500, new { error = "Something went wrong. Please try again." });
        }
    }
}
