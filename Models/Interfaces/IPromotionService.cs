namespace Models.Interfaces
{
    public interface IPromotionService
    {
        /// <summary>
        /// Calculates the total discount for a list of cart items based on active promotions.
        /// The cheapest items in each qualifying group are made free.
        /// </summary>
        Task<PromotionDiscount> CalculateDiscountAsync(IEnumerable<ShoppingCart> cartItems);
    }

    public class PromotionDiscount
    {
        public decimal TotalDiscount { get; set; }
        public List<PromotionApplied> AppliedPromotions { get; set; } = new();
    }

    public class PromotionApplied
    {
        public string PromotionName { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Discount { get; set; }
        public int FreeItemCount { get; set; }
    }
}
