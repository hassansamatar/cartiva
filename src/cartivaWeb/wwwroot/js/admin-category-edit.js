$(document).ready(function () {
    // Live preview on name change
    var originalName = $('#Name').data('original');
    $('#Name').on('input', function () {
        var name = $(this).val();
        $('#previewName').text(name || originalName);
    });

    // Live preview on size system change
    $('#SizeSystemSelect').change(function () {
        var selectedOption = $(this).find('option:selected');
        var selectedText = selectedOption.text();
        var selectedValue = $(this).val();

        if (selectedValue) {
            $('#previewSizeSystem').html('<span class="badge bg-info">' + selectedText + '</span>');
        } else {
            $('#previewSizeSystem').html('<span class="badge bg-secondary">No size system</span>');
        }
    });

    // Clear button functionality
    $('#clearSizeSystem').click(function () {
        $('#SizeSystemSelect').val('').trigger('change');
    });

    // Focus on name field
    $('#Name').focus();

    // Tooltip initialization
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
