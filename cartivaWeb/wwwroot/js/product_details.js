// product_details.js

$(document).ready(function () {
    'use strict';

    // ======================
    // Console Logging
    // ======================
    console.log('Product details page loaded');

    // ======================
    // Quantity Input Validation
    // ======================
    $('.quantity-input').on('change', function () {
        const input = $(this);
        const maxStock = parseInt(input.attr('max'));
        const minStock = parseInt(input.attr('min'));
        let value = parseInt(input.val());

        if (isNaN(value)) {
            value = minStock;
        }

        if (value > maxStock) {
            value = maxStock;
            alert(`Maximum quantity is ${maxStock}`);
        }

        if (value < minStock) {
            value = minStock;
        }

        input.val(value);
    });

    // ======================
    // Add to Cart Animation
    // ======================
    $('.add-to-cart-btn').on('click', function () {
        const button = $(this);
        const originalHtml = button.html();

        button.html('<span class="spinner-border spinner-border-sm"></span>');
        button.prop('disabled', true);

        setTimeout(function () {
            if (button.prop('disabled')) {
                button.html(originalHtml);
                button.prop('disabled', false);
            }
        }, 2000);
    });

    // ======================
    // Stock Warning Hover Effect
    // ======================
    $('.stock-low').on('mouseenter', function () {
        $(this).css('transform', 'scale(1.05)');
    }).on('mouseleave', function () {
        $(this).css('transform', 'scale(1)');
    });

    // ======================
    // Smooth Scroll to Variants
    // ======================
    if (window.location.hash === '#variants') {
        $('html, body').animate({
            scrollTop: $('.variants-list').offset().top - 100
        }, 500);
    }

    // ======================
    // Auto-focus on first available quantity input
    // ======================
    const firstAvailableInput = $('.variant-card:not(.variant-out-of-stock) .quantity-input').first();
    if (firstAvailableInput.length) {
        firstAvailableInput.focus();
    }
});