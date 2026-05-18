using EsquireVRN.Models;
using EsquireVRN.Models.DTO;
using EsquireVRN.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EsquireVRN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class MenuController : ControllerBase
    {
        // GET: api/<MenuController>
        [HttpGet]
        public IEnumerable<Menu> Get()
        {
            return Shared.GetMenu();
        }

        [HttpPost]
        [Authorize(Roles = "Reseller")]
        public Menu Post([FromBody] Menu menu)
        {
            menu.OrgId = Shared.GetOrgID();
            menu.Date = DateTime.UtcNow.AddHours(2);
            Menu sMenu = Shared.SaveMenu(menu);
            return sMenu;
        }

        // PUT api/<MenuController>/5
        [HttpPut("{id}")]
        //[Authorize(Roles = "Reseller")]
        public IActionResult Put(int id, [FromBody] Menu menu)
        {
            Menu oldMenu = Shared.GetMenuDetail(id);
            if (oldMenu == null)
            {
                return StatusCode(404, new { error = "Menu doesn't exist anymore." });
            }
            if (menu.OrgId == null)
            {
                menu.OrgId = oldMenu.OrgId;
            }
            if (menu.Date == null)
            {
                menu.Date = oldMenu.Date;
            }
            if (menu.Position == null)
            {
                menu.Position = oldMenu.Position;
            }
            if (menu.ImageUrl == null)
            {
                menu.ImageUrl = oldMenu.ImageUrl;
            }
            if (menu.Contents == null)
            {
                menu.Contents = oldMenu.Contents;
            }
            if (menu.Department == null)
            {
                menu.Department = oldMenu.Department;
            }
            if (menu.ImageUrl == null)
            {
                menu.ImageUrl = oldMenu.ImageUrl;
            }
            Menu sMenu = Shared.UpdateMenu(id, menu);
            return Ok(sMenu);
        }

        [HttpPost]
        [Route("OrderMenu")]
        [Authorize(Roles = "Reseller")]
        public IActionResult OrderMenu([FromBody] MenuIdList menuIdList)
        {
            try
            {
                List<MenuIdPositionPair> IdPositionList = new();
                int postion = 1;
                foreach (var item in menuIdList.Ids)
                {
                    MenuIdPositionPair selectOption = new()
                    {
                        Id = item,
                        Position = postion
                    };
                    IdPositionList.Add(selectOption);
                    postion++;
                }
                List<Menu> menus = Shared.UpdateMenuOrder(IdPositionList);
                return Ok(menus);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

        }
        // DELETE api/<MenuController>/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Reseller")]
        public IActionResult Delete(int id)
        {
            Menu oldMenu = Shared.GetMenuDetail(id);
            if (oldMenu == null)
            {
                return StatusCode(404, new { error = "Menu doesn't exist anymore." });
            }
            try
            {
                Shared.DeleteMenu(id);
                return Ok(new { message = "Menu removed successfully." });
            }
            catch
            {
                return StatusCode(500, new { error = "Something went wrong with the server. Please try again." });
            }
        }
    }
}
