using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderDetailId { get; set; }

        [ForeignKey("OrderDetailId")]
        [ValidateNever]
        public OrderDetail OrderDetail { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        [Required]
        [StringLength(50)]
        public string Reason { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        // Admin response
        public string? AdminNote { get; set; }
        public DateTime? ResolvedDate { get; set; }

        // Refund info
        public decimal? RefundAmount { get; set; }
        public string? RefundId { get; set; }
        public DateTime? RefundDate { get; set; }
    }
}
