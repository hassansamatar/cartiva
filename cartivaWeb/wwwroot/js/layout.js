toastr.options = {
    "positionClass": "toast-top-right",
    "timeOut": "3000",
    "progressBar": true,
    "closeButton": true
};

function updateCartCount() {
    var isAuthenticated = document.body.dataset.authenticated === "true";

    if (!isAuthenticated) {
        const cartBadge = document.getElementById('cartCount');
        if (cartBadge) cartBadge.style.display = 'none';
        return;
    }

    fetch('/Customer/Cart/GetCartCount')
        .then(r => r.json())
        .then(data => {
            const cartBadge = document.getElementById('cartCount');
            if (!cartBadge) return;

            if (data.count > 0) {
                cartBadge.textContent = data.count;
                cartBadge.style.display = 'inline';

                cartBadge.classList.add('scale-up');
                setTimeout(() => cartBadge.classList.remove('scale-up'), 300);
            }
            else {
                cartBadge.style.display = 'none';
            }
        })
        .catch(() => { });
}

document.addEventListener('DOMContentLoaded', updateCartCount);
document.addEventListener('cartUpdated', updateCartCount);
$(document).ajaxComplete(updateCartCount);
