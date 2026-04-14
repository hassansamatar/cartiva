// Confirmation for delete
document.querySelector('form')?.addEventListener('submit', function (e) {
    e.preventDefault();
    var form = this;
    swalConfirmDelete('this category').then(function (result) {
        if (result.isConfirmed) {
            form.submit();
        }
    });
});
