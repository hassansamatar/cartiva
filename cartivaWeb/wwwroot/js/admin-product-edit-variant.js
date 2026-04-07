$(document).ready(function () {
    var hasSizeSystem = $('#Variant_Color').closest('form').data('has-size-system');

    // Live preview update on color change
    $('#Variant_Color').change(function () {
        var selectedColor = $(this).find('option:selected').text();
        $('#previewColorText').text(selectedColor);
    });

    if (hasSizeSystem) {
        // Live preview update on size change
        $('#Variant_SizeValueId').change(function () {
            var selectedOption = $(this).find('option:selected');
            var selectedText = selectedOption.text();
            var selectedValue = $(this).val();

            if (selectedValue) {
                $('#previewSizeText').text(selectedText);
                $('#previewSizeBadge').show();
            } else {
                $('#previewSizeText').text('No size');
                $('#previewSizeBadge').hide();
            }
        });
    }

    // Live preview update on price change
    $('#Variant_Price').on('input', function () {
        var price = parseFloat($(this).val()) || 0;
        var formattedPrice = 'kr ' + price.toFixed(2).replace('.', ',');
        $('#previewPrice').text(formattedPrice);

        updateTotalValue();
    });

    // Live preview update on stock change
    $('#Variant_Stock').on('input', function () {
        var stock = parseInt($(this).val()) || 0;
        var stockElement = $('#previewStock');

        if (stock > 10) {
            stockElement.html('<span class="text-success">' + stock + '</span>');
        } else if (stock > 0) {
            stockElement.html('<span class="text-warning">' + stock + '</span>');
        } else {
            stockElement.html('<span class="text-danger">Out of Stock</span>');
        }

        updateTotalValue();
    });

    function updateTotalValue() {
        var price = parseFloat($('#Variant_Price').val()) || 0;
        var stock = parseInt($('#Variant_Stock').val()) || 0;
        var total = price * stock;
        $('.card-body small:contains("Value:")').html(
            '<i class="bi bi-currency-exchange"></i> <strong>Value:</strong> ' +
            'kr ' + total.toFixed(2).replace('.', ',')
        );
    }

    // Focus on first field
    $('#Variant_Color').focus();
});

function selectSize(value, text) {
    $('#Variant_SizeValueId').val(value).trigger('change');

    // Scroll to form
    $('html, body').animate({
        scrollTop: $('#Variant_SizeValueId').offset().top - 100
    }, 500);
}
