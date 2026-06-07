/* ==============================================
   ADMIN DASHBOARD SHARED JAVASCRIPT
   Reusable DataTables config and utilities
   ============================================== */

// ==============================================
// DATATABLES CONFIGURATION
// ==============================================

// English language settings for DataTables
var englishLanguage = {
    search: "Search:",
    lengthMenu: "Show _MENU_ per page",
    info: "Showing _START_ to _END_ of _TOTAL_ entries",
    infoEmpty: "No entries found",
    infoFiltered: "(filtered from _MAX_ total)",
    zeroRecords: "No matching records found",
    paginate: {
        first: "First",
        last: "Last",
        next: "Next",
        previous: "Previous"
    },
    emptyTable: "No data available"
};

// Common DataTable settings factory
function getDataTableSettings(options) {
    var defaults = {
        paging: true,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        searching: true,
        ordering: true,
        info: true,
        responsive: true,
        language: englishLanguage,
        dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
        autoWidth: false
    };

    return $.extend({}, defaults, options || {});
}

// ==============================================
// INITIALIZATION HELPER
// ==============================================

// Initialize a DataTable with standard settings
function initAdminTable(selector, options) {
    if ($(selector).length) {
        return $(selector).DataTable(getDataTableSettings(options));
    }
    return null;
}

// ==============================================
// UTILITY FUNCTIONS
// ==============================================

// Format currency (Norwegian Krone)
function formatCurrency(amount) {
    return new Intl.NumberFormat('nb-NO', {
        style: 'currency',
        currency: 'NOK'
    }).format(amount);
}

// Format date (Norwegian format dd.MM.yyyy)
function formatDate(dateString) {
    if (!dateString) return '-';
    var date = new Date(dateString);
    return date.toLocaleDateString('nb-NO', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    });
}

// Format number with Norwegian locale
function formatNumber(num) {
    return new Intl.NumberFormat('nb-NO').format(num);
}

// ==============================================
// CONFIRM DIALOGS
// ==============================================

// Confirm action with custom message
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// Confirm delete
function confirmDelete(itemName, callback) {
    confirmAction('Are you sure you want to delete ' + itemName + '?', callback);
}

// Confirm deactivate
function confirmDeactivate(itemName, callback) {
    confirmAction('Are you sure you want to deactivate ' + itemName + '?', callback);
}

// ==============================================
// AJAX HELPERS
// ==============================================

// POST request with anti-forgery token
function postWithToken(url, data, callback) {
    var token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: {
            'RequestVerificationToken': token
        },
        success: function(response) {
            if (callback) callback(response);
        },
        error: function(xhr, status, error) {
            console.error('Request failed:', error);
            alert('An error occurred. Please try again.');
        }
    });
}

// ==============================================
// TOAST NOTIFICATIONS
// ==============================================

// Show success toast (requires toastr)
function showSuccess(message) {
    if (typeof toastr !== 'undefined') {
        toastr.success(message);
    } else {
        alert(message);
    }
}

// Show error toast
function showError(message) {
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else {
        alert(message);
    }
}

// Show warning toast
function showWarning(message) {
    if (typeof toastr !== 'undefined') {
        toastr.warning(message);
    } else {
        alert(message);
    }
}

// ==============================================
// BOOTSTRAP TOOLTIPS
// ==============================================

// Initialize all Bootstrap tooltips
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// ==============================================
// DOM READY
// ==============================================

$(document).ready(function() {
    // Initialize tooltips on page load
    initTooltips();

    // Log initialization
    console.log('Admin Dashboard JS initialized');
});
