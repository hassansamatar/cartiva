$(document).ready(function () {
    'use strict';
    console.log('Product details page loaded');

    // ======================
    // Quantity Input Validation
    // ======================
    $('.quantity-input').on('change', function () {
        const input = $(this);
        const maxStock = parseInt(input.attr('max'));
        const minStock = parseInt(input.attr('min'));
        let value = parseInt(input.val());

        if (isNaN(value) || value < minStock) value = minStock;
        if (value > maxStock) {
            value = maxStock;
            swalToast('Maximum quantity is ' + maxStock, 'warning');
        }

        input.val(value);
    });

    // ======================
    // Add to Cart AJAX
    // ======================
    $('.add-to-cart-btn').on('click', function () {
        const button = $(this);
        const variantId = button.data('variant-id');
        const quantityInput = button.closest('.add-to-cart-wrapper').find('.quantity-input');
        const quantity = parseInt(quantityInput.val());
        const maxStock = parseInt(button.data('max-stock'));
        const feedbackDiv = button.closest('.add-to-cart-wrapper').find('.add-to-cart-feedback');

        if (isNaN(quantity) || quantity < 1) {
            feedbackDiv.text('Please enter a valid quantity').show().fadeOut(2000);
            return;
        }

        if (quantity > maxStock) {
            feedbackDiv.text(`Only ${maxStock} available`).show().fadeOut(2000);
            return;
        }

        const originalHtml = button.html();
        button.html('<span class="loading-spinner"></span>').prop('disabled', true);
        feedbackDiv.hide();

        // Get the anti-forgery token from the page
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: {
                productVariantId: variantId,
                count: quantity,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    feedbackDiv.html('<i class="bi bi-check-circle"></i> Added to cart!')
                        .addClass('text-success')
                        .show();

                    if (typeof updateCartCount === 'function') updateCartCount();
                    else document.dispatchEvent(new CustomEvent('cartUpdated'));

                    setTimeout(() => {
                        button.html(originalHtml).prop('disabled', false);
                        feedbackDiv.fadeOut(2000);
                    }, 1500);
                } else {
                    feedbackDiv.html('<i class="bi bi-exclamation-circle"></i> ' + (response.message || 'Error adding to cart'))
                        .removeClass('text-success')
                        .addClass('text-danger')
                        .show();
                    button.html(originalHtml).prop('disabled', false);
                    setTimeout(() => feedbackDiv.fadeOut(2000), 3000);
                }
            },
            error: function () {
                feedbackDiv.html('<i class="bi bi-exclamation-circle"></i> Network error. Please try again.')
                    .removeClass('text-success')
                    .addClass('text-danger')
                    .show();
                button.html(originalHtml).prop('disabled', false);
                setTimeout(() => feedbackDiv.fadeOut(2000), 3000);
            }
        });
    });
});