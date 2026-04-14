$(document).ready(function () {

    $("#companyForm").validate({
        highlight: function (element) {
            $(element).addClass('is-invalid');
        },
        unhighlight: function (element) {
            $(element).removeClass('is-invalid');
        }
    });

    // Update label text on toggle
    $('input[name="IsActive"]').on('change', function () {
        $(this).next('label').text(this.checked ? 'Active' : 'Inactive');
    });

});
