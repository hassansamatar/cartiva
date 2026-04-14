// =============================================
// SweetAlert2 Helpers — Global reusable functions
// =============================================

// Toast notification (non-blocking, top-right)
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
    didOpen: function (toast) {
        toast.addEventListener('mouseenter', Swal.stopTimer);
        toast.addEventListener('mouseleave', Swal.resumeTimer);
    }
});

function swalToast(message, icon) {
    icon = icon || 'success';
    Toast.fire({ icon: icon, title: message });
}

function swalSuccess(title, message) {
    return Swal.fire({
        icon: 'success',
        title: title || 'Success!',
        text: message || '',
        confirmButtonColor: '#198754',
        timer: 3000,
        timerProgressBar: true
    });
}

function swalError(title, message) {
    return Swal.fire({
        icon: 'error',
        title: title || 'Error!',
        text: message || 'Something went wrong.',
        confirmButtonColor: '#dc3545'
    });
}

function swalInfo(title, message) {
    return Swal.fire({
        icon: 'info',
        title: title || 'Info',
        text: message || '',
        confirmButtonColor: '#0d6efd'
    });
}

function swalConfirm(title, text, confirmText, cancelText) {
    return Swal.fire({
        title: title || 'Are you sure?',
        text: text || '',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: confirmText || 'Yes, do it!',
        cancelButtonText: cancelText || 'Cancel',
        reverseButtons: true
    });
}

function swalConfirmDelete(itemName) {
    return swalConfirm(
        'Delete ' + (itemName || 'this item') + '?',
        'This action cannot be undone.',
        'Yes, delete it!',
        'Cancel'
    );
}

// Clipboard copy with SweetAlert feedback
function swalCopyToClipboard(text, successMsg) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(function () {
            swalToast(successMsg || 'Copied to clipboard!', 'success');
        }).catch(function () {
            swalToast('Failed to copy', 'error');
        });
    } else {
        swalToast('Clipboard not supported', 'error');
    }
}

// =============================================
// Global handlers for data-attribute confirms
// =============================================
document.addEventListener('DOMContentLoaded', function () {

    // Handle <a> links with class btn-swal-confirm
    document.querySelectorAll('.btn-swal-confirm').forEach(function (el) {
        el.addEventListener('click', function (e) {
            e.preventDefault();
            var href = el.getAttribute('href');
            var title = el.dataset.swalTitle || 'Are you sure?';
            var text = el.dataset.swalText || '';

            swalConfirm(title, text, 'Yes, do it!', 'Cancel').then(function (result) {
                if (result.isConfirmed) {
                    window.location.href = href;
                }
            });
        });
    });

    // Handle <form> with class btn-swal-confirm-form
    document.querySelectorAll('.btn-swal-confirm-form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var title = form.dataset.swalTitle || 'Are you sure?';
            var text = form.dataset.swalText || '';

            swalConfirm(title, text, 'Yes, do it!', 'Cancel').then(function (result) {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        });
    });
});
