using AuthTest_RoleBased.Data;
using AuthTest_RoleBased.Models;
using AuthTest_RoleBased.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthTest_RoleBased.Controllers
{
    public class ShoppingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public ShoppingController(ApplicationDbContext _context, UserManager<ApplicationUser> userManager)
        {
            this._context = _context;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 8; 
            int totalProducts = await _context.Products.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.msg = TempData["msg"];

            var products = await _context.Products
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(products);
        }



        public IActionResult AddToCart(int pId, double qty)
        {
            bool itemFound = false;
            string msg = "";
            if (qty > 0 && pId > 0)
            {
                var prod = _context.Products.FirstOrDefault(p => p.ProductId == pId);
                if (prod != null)
                {
                    List<Product> items = new List<Product>();
                    var xItems = HttpContext.Session.GetObject<List<Product>>("cart");
                    if (xItems != null)
                    {
                        foreach (var item in xItems)
                        {
                            if (pId == item.ProductId)
                            {
                                item.Quantity += qty;
                                itemFound = true;
                                msg = "Item already added, new qty is added!!";
                            }
                            items.Add(item);
                        }
                        if (!itemFound)
                        {
                            prod.Quantity = qty;
                            items.Add(prod);
                            msg = "New item is added with existing items!!!!";
                        }
                        HttpContext.Session.SetObject<List<Product>>("cart", items);
                    }
                    else
                    {
                        prod.Quantity = qty;
                        items.Add(prod);
                        HttpContext.Session.SetObject<List<Product>>("cart", items);
                        msg = "New item is added to empty cart!!";
                    }

                }
                else
                {
                    msg = "No data found!!";
                }
            }
            else
            {
                msg = "Item id or qty could not be zero!!";
            }
            TempData["msg"] = msg;
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ShowCart()
        {
            List<Product> items = HttpContext.Session.GetObject<List<Product>>("cart");

            ApplicationUser currentUser = await _userManager.GetUserAsync(User); // Get the logged-in user
            string userCountry = currentUser?.Country; // Access the 'Country' field
            string userCellphone = currentUser?.CellPhone;

            if (items != null && items.Count != 0)
            {
               
                var model = new ShowCartViewModel
                {
                    Products = items.ToList(),
                    Country = userCountry,
                    CellPhone = userCellphone
                };
                return View(model);
            }
            else
            {
                items = new List<Product>();
                var model = new ShowCartViewModel
                {
                    Products = items,
                    Country = userCountry,
                    CellPhone = userCellphone
                };
                return View(model);
            }
        }
        public IActionResult RemoveFromCart(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            List<Product> productList = HttpContext.Session.GetObject<List<Product>>("cart");
            var removeItem = productList.FirstOrDefault(x => x.ProductId == id);
            productList.Remove(removeItem);
            HttpContext.Session.SetObject("cart", productList);
            return RedirectToAction(nameof(ShowCart));
        }

        [Authorize]
        public IActionResult CheckOut()
        {
            return RedirectToAction(nameof(ConfirmOrder));
        }
        [Authorize]
        public IActionResult ConfirmOrder()
        {
            var cartItems = HttpContext.Session.GetObject<List<Product>>("cart");
            if (cartItems == null || !cartItems.Any())
            {
                TempData["msg"] = "Cart is empty!";
                return RedirectToAction("Index");
            }

            string userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("cart");
            TempData["msg"] = "Order placed successfully!";

            return RedirectToAction("MyOrders");
        }
        [Authorize]

        public IActionResult MyOrders()
        {
            string userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.User) 
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }



       
        public IActionResult Create()
        {
            return View();
        }



        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CellPhone = user.CellPhone,
                Country = user.Country
            };

            return View(model);
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.CellPhone = model.CellPhone;
            user.Country = model.Country;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return RedirectToAction("ShowCart", "Shopping");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

    }
}
