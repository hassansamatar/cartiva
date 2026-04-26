using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing review operations
/// </summary>
public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(ApplicationDbContext db, ILogger<ReviewService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Review>> GetAllReviewsAsync()
    {
        return await _db.Reviews
            .Include(r => r.ApplicationUser)
            .Include(r => r.ProductVariant)
                .ThenInclude(pv => pv.Product)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();
    }

    public async Task<List<Review>> GetProductReviewsAsync(int productId, bool approvedOnly = true)
    {
        var query = _db.Reviews
            .Include(r => r.ApplicationUser)
            .Include(r => r.ProductVariant)
            .Where(r => r.ProductVariant.ProductId == productId);

        if (approvedOnly)
        {
            query = query.Where(r => r.IsApproved);
        }

        return await query.OrderByDescending(r => r.ReviewDate).ToListAsync();
    }

    public async Task<List<Review>> GetVariantReviewsAsync(int productVariantId, bool approvedOnly = true)
    {
        var query = _db.Reviews
            .Include(r => r.ApplicationUser)
            .Where(r => r.ProductVariantId == productVariantId);

        if (approvedOnly)
        {
            query = query.Where(r => r.IsApproved);
        }

        return await query.OrderByDescending(r => r.ReviewDate).ToListAsync();
    }

    public async Task<Review?> GetReviewByIdAsync(int id)
    {
        return await _db.Reviews
            .Include(r => r.ApplicationUser)
            .Include(r => r.ProductVariant)
                .ThenInclude(pv => pv.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ReviewOperationResult> CreateReviewAsync(string userId, int productVariantId, int orderId, int rating, string? comment)
    {
        // Verify the user can review this product
        if (!await CanUserReviewAsync(userId, productVariantId, orderId))
        {
            return ReviewOperationResult.Failed("You can only review products from delivered orders.");
        }

        // Check for existing review
        if (await HasUserReviewedAsync(userId, productVariantId))
        {
            return ReviewOperationResult.Failed("You have already reviewed this product.");
        }

        var review = new Review
        {
            ApplicationUserId = userId,
            ProductVariantId = productVariantId,
            Rating = rating,
            Comment = comment?.Trim(),
            ReviewDate = DateTime.UtcNow,
            IsApproved = false
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Review created by user {UserId} for variant {VariantId}", userId, productVariantId);
        return ReviewOperationResult.Succeeded("Thank you! Your review has been submitted and is pending approval.", review.Id);
    }

    public async Task<ReviewOperationResult> ApproveReviewAsync(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null)
        {
            return ReviewOperationResult.Failed("Review not found.");
        }

        review.IsApproved = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} approved", id);
        return ReviewOperationResult.Succeeded("Review approved.");
    }

    public async Task<ReviewOperationResult> RejectReviewAsync(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null)
        {
            return ReviewOperationResult.Failed("Review not found.");
        }

        review.IsApproved = false;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} rejected", id);
        return ReviewOperationResult.Succeeded("Review rejected.");
    }

    public async Task<ReviewOperationResult> DeleteReviewAsync(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null)
        {
            return ReviewOperationResult.Failed("Review not found.");
        }

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Review {ReviewId} deleted", id);
        return ReviewOperationResult.Succeeded("Review deleted.");
    }

    public async Task<bool> CanUserReviewAsync(string userId, int productVariantId, int orderId)
    {
        return await _db.OrderDetails
            .Include(od => od.OrderHeader)
            .AnyAsync(od => od.OrderHeader.Id == orderId
                && od.ProductVariantId == productVariantId
                && od.OrderHeader.ApplicationUserId == userId
                    && od.OrderHeader.OrderStatus == Cartiva.Domain.Enums.OrderStatus.Delivered);
    }

    public async Task<bool> HasUserReviewedAsync(string userId, int productVariantId)
    {
        return await _db.Reviews
            .AnyAsync(r => r.ApplicationUserId == userId && r.ProductVariantId == productVariantId);
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var ratings = await _db.Reviews
            .Where(r => r.ProductVariant.ProductId == productId && r.IsApproved)
            .Select(r => r.Rating)
            .ToListAsync();

        return ratings.Any() ? ratings.Average() : 0;
    }

    public async Task<int> GetReviewCountAsync(int productId)
    {
        return await _db.Reviews
            .CountAsync(r => r.ProductVariant.ProductId == productId && r.IsApproved);
    }
}
