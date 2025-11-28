using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StoreApp.Models
{
    [Table("promotion_redemptions")]
    [Index(nameof(PromotionId), Name = "idx_pr_promotion")]
    [Index(nameof(CustomerId), Name = "idx_pr_customer")]
    public class PromotionRedemption
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("promotion_id")]
        public int PromotionId { get; set; }

        [Column("customer_id")]
        public int? CustomerId { get; set; }

        [Column("order_id")]
        public int? OrderId { get; set; }

        [Column("redeemed_at")]
        public DateTime RedeemedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(PromotionId))]
        public virtual Promotion? Promotion { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }
    }
}
