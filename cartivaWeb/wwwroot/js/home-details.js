$(document).ready(function () {
    // ======================
    // ADD TO CART AJAX
    // ======================
    var addToCartUrl = $('#variants-container').data('add-to-cart-url');

    $('.add-to-cart-btn').on('click', function () {
        var button = $(this);
        var variantId = button.data('variant-id');
        var quantityInput = button.closest('.add-to-cart-wrapper').find('.quantity-input');
        var quantity = parseInt(quantityInput.val());
        var maxStock = parseInt(button.data('max-stock'));
        var feedbackDiv = button.closest('.add-to-cart-wrapper').find('.add-to-cart-feedback');

        // Validate quantity
        if (isNaN(quantity) || quantity < 1) {
            feedbackDiv.text('Please enter a valid quantity').show().fadeOut(2000);
            return;
        }

        if (quantity > maxStock) {
            feedbackDiv.text('Only ' + maxStock + ' available').show().fadeOut(2000);
            return;
        }

        // Disable button and show loading
        var originalHtml = button.html();
        button.html('<span class="loading-spinner"></span>').prop('disabled', true);
        feedbackDiv.hide();

        // AJAX request
        $.ajax({
            url: addToCartUrl,
            type: 'POST',
            data: {
                productVariantId: variantId,
                count: quantity,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    // Show success message
                    feedbackDiv.html('<i class="bi bi-check-circle"></i> Added to cart!').addClass('text-success').show();

                    // Update cart count in navbar
                    if (typeof updateCartCount === 'function') {
                        updateCartCount();
                    } else {
                        // Dispatch custom event
                        document.dispatchEvent(new CustomEvent('cartUpdated'));
                    }

                    // Update stock display
                    var stockSpan = button.closest('.variant-card').find('.stock-count');
                    if (stockSpan.length) {
                        var newStock = response.cartCount ? 'Updated' : '';
                    }

                    // Reset button
                    setTimeout(function () {
                        button.html(originalHtml).prop('disabled', false);
                        feedbackDiv.fadeOut(2000);
                    }, 1500);
                } else {
                    feedbackDiv.html('<i class="bi bi-exclamation-circle"></i> ' + (response.message || 'Error adding to cart')).removeClass('text-success').addClass('text-danger').show();
                    button.html(originalHtml).prop('disabled', false);
                    setTimeout(function () { feedbackDiv.fadeOut(2000); }, 3000);
                }
            },
            error: function () {
                feedbackDiv.html('<i class="bi bi-exclamation-circle"></i> Network error. Please try again.').removeClass('text-success').addClass('text-danger').show();
                button.html(originalHtml).prop('disabled', false);
                setTimeout(function () { feedbackDiv.fadeOut(2000); }, 3000);
            }
        });
    });

    // ======================
    // QUANTITY INPUT VALIDATION
    // ======================
    $('.quantity-input').on('change', function () {
        var input = $(this);
        var maxStock = parseInt(input.attr('max'));
        var value = parseInt(input.val());

        if (isNaN(value) || value < 1) {
            value = 1;
        }
        if (value > maxStock) {
            value = maxStock;
        }
        input.val(value);
    });
});
