$(document).ready(function () {
    $('#CategorySelect').change(function () {
        var categoryId = $(this).val();
        var url = $(this).data('size-system-url');
        if (categoryId && url) {
            $.get(url, { categoryId: categoryId })
                .done(function (data) {
                    if (data.hasSizeSystem) {
                        $('#sizeSystemName').text(data.sizeSystemName);
                        $('#sizeSystemInfo').show();
                    } else {
                        $('#sizeSystemInfo').show();
                        $('#sizeSystemName').text('No size system (accessory)');
                    }
                });
        } else {
            $('#sizeSystemInfo').hide();
        }
    });

    // Trigger change on page load if category is selected (for edit mode)
    var selectedCategory = $('#CategorySelect').val();
    if (selectedCategory) {
        $('#CategorySelect').trigger('change');
    }

    // Live image preview (optional)
    $('input[name="file"]').change(function () {
        var file = this.files[0];
        if (file) {
            console.log('Image selected: ' + file.name);
        }
    });
});
