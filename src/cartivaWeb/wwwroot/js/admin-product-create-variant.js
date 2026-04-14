$(document).ready(function () {
    var hasSizeSystem = $('#Variant_Color').closest('form').data('has-size-system');

    // Live preview updates
    $('#Variant_Color').change(function () {
        var selectedColor = $(this).find('option:selected').text();
        $('#previewColor').text(selectedColor || 'No color selected');
    });

    if (hasSizeSystem) {
        $('#Variant_SizeValueId').change(function () {
            var selectedSize = $(this).find('option:selected').text();
            $('#previewSize').text(selectedSize || 'No size selected');
        });
    }

    $('#Variant_Price').on('input', function () {
        var price = parseFloat($(this).val()) || 0;
        $('#previewPrice').text('kr ' + price.toFixed(2).replace('.', ','));
    });

    $('#Variant_Stock').on('input', function () {
        var stock = parseInt($(this).val()) || 0;
        $('#previewStock').text('Stock: ' + stock);
        if (stock > 10) {
            $('#previewStock').removeClass('bg-warning bg-danger').addClass('bg-success');
        } else if (stock > 0) {
            $('#previewStock').removeClass('bg-success bg-danger').addClass('bg-warning');
        } else {
            $('#previewStock').removeClass('bg-success bg-warning').addClass('bg-danger');
        }
    });

    // Focus on first field
    $('#Variant_Color').focus();
});
