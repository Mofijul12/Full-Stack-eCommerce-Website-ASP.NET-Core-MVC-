using AuthTest_RoleBased.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthTest_RoleBased.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        [Required, StringLength(50)]
        public string Name { get; set; } = default!;
        public string Unit { get; set; } = default!;
        public double Price { get; set; }
        public double Quantity { get; set; }
        public string? Photo { get; set; } = default!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }

    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; } = default!;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";


        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ApplicationUser User { get; set; } = default!;
    }

    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public double Quantity { get; set; }
        public double Price { get; set; }

        public virtual Order Order { get; set; } = default!;
        public virtual Product Product { get; set; } = default!;
    }

    public class ProductLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; } // Just a value, no FK constraint

        public string? ProductName { get; set; } // Copy of product name
        public string? Action { get; set; }      // Created / Updated / Deleted
        public string? Details { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; } // Still FK to user
    }

}
