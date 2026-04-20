using Cartiva.Domain;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing return request operations
/// </summary>
public interface IReturnService
{
    #region Queries

    /// <summary>
    /// Get all return requests with details (for admin)
    /// </summary>
    Task<List<ReturnRequest>> GetAllReturnRequestsAsync();

    /// <summary>
    /// Get return requests for a specific user
    /// </summary>
    Task<List<ReturnRequest>> GetUserReturnRequestsAsync(string userId);

    /// <summary>
    /// Get a return request by ID
    /// </summary>
    Task<ReturnRequest?> GetReturnRequestByIdAsync(int id);

    /// <summary>
    /// Check if a return request already exists for an order detail
    /// </summary>
    Task<bool> HasExistingReturnAsync(int orderDetailId);

    #endregion

    #region Customer Operations

    /// <summary>
    /// Validate if customer can request a return
    /// </summary>
    Task<ReturnValidationResult> ValidateReturnRequestAsync(string userId, int orderDetailId);

    /// <summary>
    /// Create a new return request (customer)
    /// </summary>
    Task<ReturnOperationResult> CreateReturnRequestAsync(string userId, int orderDetailId, string reason, string? description, int quantity);

    #endregion

    #region Admin Operations

    /// <summary>
    /// Approve a return request and restore stock
    /// </summary>
    Task<ReturnOperationResult> ApproveReturnAsync(int id, string? adminNote);

    /// <summary>
    /// Reject a return request
    /// </summary>
    Task<ReturnOperationResult> RejectReturnAsync(int id, string? adminNote);

    /// <summary>
    /// Process refund for an approved return
    /// </summary>
    Task<ReturnOperationResult> ProcessRefundAsync(int id);

    #endregion

    #region Helpers

    /// <summary>
    /// Get days remaining in return window
    /// </summary>
    Task<int> GetDaysRemainingInReturnWindowAsync(int orderDetailId);

    /// <summary>
    /// Get list of return reasons
    /// </summary>
    List<string> GetReturnReasons();

    #endregion
}

/// <summary>
/// Result of return validation
/// </summary>
public class ReturnValidationResult
{
    public bool CanReturn { get; set; }
    public string? ErrorMessage { get; set; }
    public OrderDetail? OrderDetail { get; set; }
    public int DaysRemaining { get; set; }

    public static ReturnValidationResult Success(OrderDetail orderDetail, int daysRemaining)
        => new() { CanReturn = true, OrderDetail = orderDetail, DaysRemaining = daysRemaining };

    public static ReturnValidationResult Failure(string message)
        => new() { CanReturn = false, ErrorMessage = message };
}

/// <summary>
/// Result of a return operation
/// </summary>
public class ReturnOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? ReturnRequestId { get; set; }
    public decimal? RefundAmount { get; set; }

    public static ReturnOperationResult Succeeded(string message, int? returnRequestId = null, decimal? refundAmount = null)
        => new() { Success = true, Message = message, ReturnRequestId = returnRequestId, RefundAmount = refundAmount };

    public static ReturnOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}
