document.addEventListener('DOMContentLoaded', function () {
    var roleSelect = document.getElementById('roleSelect');
    var companyDiv = document.getElementById('companyDiv');
    var companyRole = roleSelect.dataset.companyRole;

    roleSelect.addEventListener('change', function () {
        if (this.value === companyRole) {
            companyDiv.style.display = 'block';
        } else {
            companyDiv.style.display = 'none';
        }
    });
});
