using Cartiva.Domain;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing review operations
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Get all reviews with user and product details (for admin)
    /// </summary>
    Task<List<Review>> GetAllReviewsAsync();

    /// <summary>
    /// Get reviews for a specific product
    /// </summary>
    Task<List<Review>> GetProductReviewsAsync(int productId, bool approvedOnly = true);

    /// <summary>
    /// Get reviews for a specific product variant
    /// </summary>
    Task<List<Review>> GetVariantReviewsAsync(int productVariantId, bool approvedOnly = true);

    /// <summary>
    /// Get a review by ID
    /// </summary>
    Task<Review?> GetReviewByIdAsync(int id);

    /// <summary>
    /// Create a new review (customer)
    /// </summary>
    Task<ReviewOperationResult> CreateReviewAsync(string userId, int productVariantId, int orderId, int rating, string? comment);

    /// <summary>
    /// Approve a review (admin)
    /// </summary>
    Task<ReviewOperationResult> ApproveReviewAsync(int id);

    /// <summary>
    /// Reject a review (admin)
    /// </summary>
    Task<ReviewOperationResult> RejectReviewAsync(int id);

    /// <summary>
    /// Delete a review (admin)
    /// </summary>
    Task<ReviewOperationResult> DeleteReviewAsync(int id);

    /// <summary>
    /// Check if user can review a product variant (purchased and delivered)
    /// </summary>
    Task<bool> CanUserReviewAsync(string userId, int productVariantId, int orderId);

    /// <summary>
    /// Check if user already reviewed a product variant
    /// </summary>
    Task<bool> HasUserReviewedAsync(string userId, int productVariantId);

    /// <summary>
    /// Get average rating for a product
    /// </summary>
    Task<double> GetAverageRatingAsync(int productId);

    /// <summary>
    /// Get review count for a product
    /// </summary>
    Task<int> GetReviewCountAsync(int productId);
}

/// <summary>
/// Result of a review operation
/// </summary>
public class ReviewOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ReviewId { get; set; }

    public static ReviewOperationResult Succeeded(string message, int? reviewId = null)
        => new() { Success = true, Message = message, ReviewId = reviewId };

    public static ReviewOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}
