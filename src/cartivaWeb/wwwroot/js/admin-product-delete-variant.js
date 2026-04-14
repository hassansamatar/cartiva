$(document).ready(function () {
    // Double confirmation on delete button
    $('#confirmDeleteBtn').click(function (e) {
        e.preventDefault();
        var form = $(this).closest('form');
        swalConfirmDelete('this variant').then(function (result) {
            if (result.isConfirmed) {
                form.submit();
            }
        });
    });
});
