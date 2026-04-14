$(document).ready(function () {
    // Show selected size system info
    $('#SizeSystemId').change(function () {
        var selectedText = $(this).find('option:selected').text();
        if (selectedText && selectedText !== '-- No Default Size System --') {
            console.log('Selected size system: ' + selectedText);
        }
    });

    // Focus on name field
    $('#Name').focus();
});
