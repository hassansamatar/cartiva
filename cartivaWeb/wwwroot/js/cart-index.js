$(document).ready(function () {
    var cartConfig = $('#cart-items-container');
    var incrementUrl = cartConfig.data('increment-url');
    var decrementUrl = cartConfig.data('decrement-url');
    var updateCountUrl = cartConfig.data('update-count-url');
    var removeUrl = cartConfig.data('remove-url');
    var removeAllUrl = cartConfig.data('remove-all-url');

    // Helper Functions
    function formatCurrency(amount) {
        return new Intl.NumberFormat('nb-NO', {
            style: 'currency',
            currency: 'NOK'
        }).format(amount);
    }

    function showToast(message, type) {
        type = type || 'success';
        var bgColor = type === 'success' ? 'bg-success' : (type === 'error' ? 'bg-danger' : 'bg-warning');
        var icon = type === 'success' ? 'check-circle' : (type === 'error' ? 'exclamation-triangle' : 'info-circle');

        var toast = $(
            '<div class="toast align-items-center text-white ' + bgColor + ' border-0 show" role="alert" data-bs-autohide="true" data-bs-delay="3000">' +
                '<div class="d-flex">' +
                    '<div class="toast-body">' +
                        '<i class="bi bi-' + icon + ' me-2"></i> ' + message +
                    '</div>' +
                    '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
                '</div>' +
            '</div>'
        );

        $('.toast-container').append(toast);

        setTimeout(function () {
            toast.fadeOut(300, function () { toast.remove(); });
        }, 3000);
    }

    function updateCartTotals() {
        var total = 0;
        var itemCount = 0;

        $('.cart-item').each(function () {
            var price = parseFloat($(this).data('price'));
            var qty = parseInt($(this).find('.quantity-display').text());
            total += price * qty;
            itemCount++;
        });

        $('#cartTotal').text(formatCurrency(total));
        $('#totalItemsBadge').text(itemCount + (itemCount === 1 ? ' item' : ' items'));

        if (typeof window.updateCartCount === 'function') {
            window.updateCartCount();
        }
    }

    function updateSubtotal(itemId, newQty) {
        var price = $('#cart-item-' + itemId).data('price');
        var subtotal = price * newQty;
        $('#subtotal-' + itemId).html('<strong class="text-primary">' + formatCurrency(subtotal) + '</strong>');
    }

    function getToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // Increment Button
    $('.increment-btn').click(function () {
        var btn = $(this);
        var itemId = btn.data('item-id');
        var qtySpan = $('#qty-' + itemId);
        var currentQty = parseInt(qtySpan.text());
        var maxStock = parseInt($('#cart-item-' + itemId).data('stock'));

        if (currentQty < maxStock) {
            var newQty = currentQty + 1;
            btn.prop('disabled', true);
            $('#qty-input-' + itemId).val(newQty);

            $.ajax({
                url: incrementUrl,
                type: 'POST',
                data: { id: itemId, __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        qtySpan.text(newQty);
                        updateSubtotal(itemId, newQty);

                        if (newQty >= maxStock) btn.prop('disabled', true);
                        else btn.prop('disabled', false);

                        if (newQty > 1) $('[data-item-id="' + itemId + '"].decrement-btn').prop('disabled', false);

                        updateCartTotals();
                        showToast('Quantity updated');
                    } else {
                        showToast(response.message || 'Failed to update', 'error');
                        btn.prop('disabled', false);
                    }
                },
                error: function () {
                    showToast('Network error', 'error');
                    btn.prop('disabled', false);
                }
            });
        }
    });

    // Decrement Button
    $('.decrement-btn').click(function () {
        var btn = $(this);
        var itemId = btn.data('item-id');
        var qtySpan = $('#qty-' + itemId);
        var currentQty = parseInt(qtySpan.text());

        if (currentQty > 1) {
            var newQty = currentQty - 1;
            btn.prop('disabled', true);
            $('#qty-input-' + itemId).val(newQty);

            $.ajax({
                url: decrementUrl,
                type: 'POST',
                data: { id: itemId, __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        if (response.removed) {
                            $('#cart-item-' + itemId).addClass('cart-item-removing').fadeOut(300, function () {
                                $(this).remove();
                                updateCartTotals();
                                if ($('.cart-item').length === 0) location.reload();
                                showToast('Item removed');
                            });
                        } else {
                            qtySpan.text(newQty);
                            updateSubtotal(itemId, newQty);

                            if (newQty <= 1) btn.prop('disabled', true);
                            else btn.prop('disabled', false);

                            var maxStock = $('#cart-item-' + itemId).data('stock');
                            if (newQty < maxStock) $('[data-item-id="' + itemId + '"].increment-btn').prop('disabled', false);

                            updateCartTotals();
                            showToast('Quantity updated');
                        }
                    } else {
                        showToast(response.message || 'Failed to update', 'error');
                        btn.prop('disabled', false);
                    }
                },
                error: function () {
                    showToast('Network error', 'error');
                    btn.prop('disabled', false);
                }
            });
        }
    });

    // Update Quantity Button
    $('.update-qty-btn').click(function () {
        var btn = $(this);
        var itemId = btn.data('item-id');
        var input = $('#qty-input-' + itemId);
        var newQty = parseInt(input.val());
        var maxStock = parseInt(input.attr('max'));
        var minStock = parseInt(input.attr('min'));
        var currentQty = parseInt($('#qty-' + itemId).text());

        if (isNaN(newQty) || newQty < minStock) {
            showToast('Minimum quantity is ' + minStock, 'warning');
            input.val(currentQty);
            return;
        }

        if (newQty > maxStock) {
            showToast('Only ' + maxStock + ' available', 'warning');
            input.val(currentQty);
            return;
        }

        if (newQty === currentQty) return;

        btn.prop('disabled', true);
        btn.html('<span class="loading-spinner-small"></span>');

        $.ajax({
            url: updateCountUrl,
            type: 'POST',
            data: { id: itemId, count: newQty, __RequestVerificationToken: getToken() },
            success: function (response) {
                if (response.success) {
                    if (response.removed) {
                        $('#cart-item-' + itemId).addClass('cart-item-removing').fadeOut(300, function () {
                            $(this).remove();
                            updateCartTotals();
                            if ($('.cart-item').length === 0) location.reload();
                            showToast('Item removed');
                        });
                    } else {
                        $('#qty-' + itemId).text(newQty);
                        updateSubtotal(itemId, newQty);

                        $('[data-item-id="' + itemId + '"].decrement-btn').prop('disabled', newQty <= 1);
                        $('[data-item-id="' + itemId + '"].increment-btn').prop('disabled', newQty >= maxStock);

                        updateCartTotals();
                        showToast('Quantity updated');
                    }
                } else {
                    showToast(response.message || 'Failed to update', 'error');
                    input.val(currentQty);
                }
                btn.prop('disabled', false);
                btn.html('<i class="bi bi-check"></i>');
            },
            error: function () {
                showToast('Network error', 'error');
                input.val(currentQty);
                btn.prop('disabled', false);
                btn.html('<i class="bi bi-check"></i>');
            }
        });
    });

    // Remove Item
    $('.remove-item-btn').click(function () {
        var btn = $(this);
        var itemId = btn.data('item-id');
        var productName = btn.data('product-name');

        if (confirm('Remove ' + productName + ' from your cart?')) {
            btn.prop('disabled', true);
            btn.html('<span class="loading-spinner-small"></span>');

            $.ajax({
                url: removeUrl,
                type: 'POST',
                data: { id: itemId, __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        $('#cart-item-' + itemId).addClass('cart-item-removing').fadeOut(300, function () {
                            $(this).remove();
                            updateCartTotals();
                            if ($('.cart-item').length === 0) location.reload();
                            showToast(productName + ' removed');
                        });
                    } else {
                        showToast(response.message || 'Failed to remove', 'error');
                        btn.prop('disabled', false);
                        btn.html('<i class="bi bi-trash"></i>');
                    }
                },
                error: function () {
                    showToast('Network error', 'error');
                    btn.prop('disabled', false);
                    btn.html('<i class="bi bi-trash"></i>');
                }
            });
        }
    });

    // Clear Cart
    $('#clearCartBtn').click(function () {
        if (confirm('Remove all items from your cart?')) {
            var btn = $(this);
            btn.prop('disabled', true);
            btn.html('<span class="spinner-border spinner-border-sm"></span> Clearing...');

            $.ajax({
                url: removeAllUrl,
                type: 'POST',
                data: { __RequestVerificationToken: getToken() },
                success: function (response) {
                    if (response.success) {
                        showToast('Cart cleared');
                        setTimeout(function () { location.reload(); }, 1000);
                    } else {
                        showToast(response.message || 'Failed to clear', 'error');
                        btn.prop('disabled', false);
                        btn.html('<i class="bi bi-trash"></i> Clear Cart');
                    }
                },
                error: function () {
                    showToast('Network error', 'error');
                    btn.prop('disabled', false);
                    btn.html('<i class="bi bi-trash"></i> Clear Cart');
                }
            });
        }
    });

    // Enter key on quantity input
    $('.quantity-input').keypress(function (e) {
        if (e.which === 13) {
            $(this).closest('td').find('.update-qty-btn').click();
        }
    });
});
