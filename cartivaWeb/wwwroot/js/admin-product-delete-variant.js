$(document).ready(function () {
    // Double confirmation on delete button
    $('#confirmDeleteBtn').click(function (e) {
        if (!confirm('Are you absolutely sure you want to delete this variant? This action cannot be undone.')) {
            e.preventDefault();
            return false;
        }
        return true;
    });
});
