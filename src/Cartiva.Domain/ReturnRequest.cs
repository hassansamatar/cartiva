using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cartiva.Domain.Enums;

namespace Cartiva.Domain
{
    public class ReturnRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderDetailId { get; set; }

        [ForeignKey(nameof(OrderDetailId))]
        [ValidateNever]
        public OrderDetail OrderDetail { get; set; } = null!;

        [Required]
        public string ApplicationUserId { get; set; } = null!;

        [ForeignKey(nameof(ApplicationUserId))]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [StringLength(30)]
        public ReturnStatus Status { get; set; } = Enums.ReturnStatus.Pending;

        // Admin response
        public string? AdminNote { get; set; }
        public DateTime? ResolvedDate { get; set; }

        // Refund info
        public decimal? RefundAmount { get; set; }
        public string? RefundId { get; set; }
        public DateTime? RefundDate { get; set; }
    }
}