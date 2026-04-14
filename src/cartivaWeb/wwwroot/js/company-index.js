$(document).ready(function () {
    $('#tblCompany').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        lengthChange: true,
        pageLength: 10,
        columnDefs: [
            { orderable: false, targets: 7 }
        ]
    });
});
