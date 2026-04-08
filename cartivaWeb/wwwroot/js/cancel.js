

$(document).ready(function () {
    'use strict';

    // Initialize modal
    const cancelModal = new bootstrap.Modal(document.getElementById('cancelConfirmModal'));

    // Get order ID from hidden input
    const orderId = $('#orderId').val();

    // Get anti-forgery token
    const token = $('input[name="__RequestVerificationToken"]').val();

    // Show confirmation modal when cancel button is clicked
    $('#confirmCancelBtn').click(function () {
        cancelModal.show();
    });

    // Handle final confirmation
    $('#finalConfirmBtn').click(function () {
        const button = $(this);
        const originalText = button.html();

        // Disable button and show spinner
        button.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

        // Make AJAX request to cancel order
        $.ajax({
            url: '/Customer/Order/ConfirmCancel',
            type: 'POST',
            data: {
                id: orderId,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    swalToast(response.message, 'success');

                    // Redirect to order details after delay
                    setTimeout(function () {
                        window.location.href = `/Customer/Order/Details/${orderId}`;
                    }, 2000);
                } else {
                    swalToast(response.message, 'error');

                    // Reset button
                    resetButton(button, originalText);
                    cancelModal.hide();
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', error);
                swalToast('An error occurred while cancelling the order.', 'error');

                // Reset button
                resetButton(button, originalText);
                cancelModal.hide();
            }
        });
    });

    // Handle modal hidden event
    $('#cancelConfirmModal').on('hidden.bs.modal', function () {
        const button = $('#finalConfirmBtn');
        resetButton(button, 'Yes, Cancel Order');
    });

    // Helper function to reset button state
    function resetButton(button, text) {
        button.prop('disabled', false)
            .html('<i class="bi bi-check-circle me-2"></i>' + text);
    }

    // Log page view
    console.log('Cancel order page loaded for order #' + orderId);
});