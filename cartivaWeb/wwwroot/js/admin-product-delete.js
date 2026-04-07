function toggleDeleteButton() {
    var checkbox = document.getElementById('confirmDelete');
    var deleteButton = document.getElementById('deleteButton');
    deleteButton.disabled = !checkbox.checked;
}

// Initialize tooltips
document.addEventListener('DOMContentLoaded', function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
