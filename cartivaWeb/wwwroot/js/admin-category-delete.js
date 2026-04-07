// Confirmation for delete
document.querySelector('form')?.addEventListener('submit', function (e) {
    if (!confirm('Are you absolutely sure you want to delete this category?')) {
        e.preventDefault();
    }
});
