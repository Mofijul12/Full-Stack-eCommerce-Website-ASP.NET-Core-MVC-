using AuthTest_RoleBased.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthTest_RoleBased.Controllers
{

    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleController(RoleManager<IdentityRole> _roleManager, UserManager<ApplicationUser> _userManager, ApplicationDbContext contex)
        {
                this._roleManager = _roleManager;
                this._userManager = _userManager;
                this._context = contex;
        }
        [Authorize(Roles = "ADMIN")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string userRole)
        {
            string msg = "";
            if (!string.IsNullOrEmpty(userRole)) 
            { 
                if(await _roleManager.RoleExistsAsync(userRole))
                {
                    msg = "Role [" + userRole + "] already exist!!!";
                }
                else
                {
                    IdentityRole r=new IdentityRole(userRole);
                    await _roleManager.CreateAsync(r);
                    msg = "Role [" + userRole + "] has been created successfully!!!";
                }
            }
            else
            {
                msg = "Please enter a valid role name!!";
            }
            ViewBag.msg = msg;
            return View("Index");
        }
        [Authorize(Roles = "ADMIN")]
        public IActionResult AssignRole()
        {
            ViewBag.users = _userManager.Users;
            ViewBag.roles = _roleManager.Roles;

            ViewBag.msg = TempData["msg"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userData,string roleData)
        {
            string msg = "";
            if (!string.IsNullOrEmpty(userData) && !string.IsNullOrEmpty(roleData)) 
            { 
                ApplicationUser u=await _userManager.FindByEmailAsync(userData);
                if (u != null) 
                {
                    if(await _roleManager.RoleExistsAsync(roleData))
                    {
                        await _userManager.AddToRoleAsync(u, roleData);
                        msg = "Role has been assigned to user!!";
                    }
                    else
                    {
                        msg = "Role does not exist!!";
                    }
                }
                else
                {
                    msg = "User is not found!!";
                }
            }
            else
            {
                msg = "Please select a valid user or role data!!";
            }
            TempData["msg"] = msg;
            return RedirectToAction(nameof(AssignRole));
        }
        [Authorize(Roles = "ADMIN,MANAGER")]
        public IActionResult UserList()
        {
            var usersWithLatestOrder = _context.Orders
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LatestOrderId = g.Max(o => o.OrderId) // Assuming higher Order.Id means more recent
                })
                .OrderByDescending(x => x.LatestOrderId)
                .ToList();

            var userIdsOrdered = usersWithLatestOrder.Select(x => x.UserId).ToList();

            var users = _userManager.Users
                .Where(u => userIdsOrdered.Contains(u.Id))
                .ToList();

            // Order users by the order of IDs from the above list
            users = users.OrderBy(u => userIdsOrdered.IndexOf(u.Id)).ToList();

            return View(users);
        }

        public IActionResult UserOrders(string userId)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            ViewBag.UserName = user.UserName;
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order != null)
            {
                order.Status = status;
                _context.SaveChanges();
            }
            return RedirectToAction("UserOrders", new { userId = order.UserId });
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AllUser()
        {
            var users = _userManager.Users.ToList();
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();

            var userRolesDict = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var rolesForUser = await _userManager.GetRolesAsync(user);
                userRolesDict[user.Id] = rolesForUser;
            }

            ViewBag.Roles = roles;
            ViewBag.UserRolesDict = userRolesDict;
            ViewBag.msg = TempData["msg"];

            return View(users);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["msg"] = "Invalid user ID!";
                return RedirectToAction(nameof(UserList));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                TempData["msg"] = result.Succeeded ? "User deleted!" : "Failed to delete user!";
            }
            else
            {
                TempData["msg"] = "User not found!";
            }

            return RedirectToAction(nameof(AllUser));
        }

    }
}
