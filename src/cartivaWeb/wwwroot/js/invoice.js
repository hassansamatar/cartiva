/* ==============================================
   INVOICE DASHBOARD JAVASCRIPT
   DataTables initialization and utilities
   ============================================== */

// Wait for DOM to be ready
$(document).ready(function () {

    // ==============================================
    // DATATABLES CONFIGURATION
    // ==============================================

    // Common DataTable settings
    var commonSettings = {
        // Pagination
        paging: true,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],

        // Search
        searching: true,

        // Sorting
        ordering: true,

        // Info
        info: true,

        // Responsive
        responsive: true,

        // Language (Norwegian)
        language: {
            search: "Søk:",
            lengthMenu: "Vis _MENU_ per side",
            info: "Viser _START_ til _END_ av _TOTAL_ fakturaer",
            infoEmpty: "Ingen fakturaer funnet",
            infoFiltered: "(filtrert fra _MAX_ totalt)",
            zeroRecords: "Ingen matchende fakturaer funnet",
            paginate: {
                first: "Første",
                last: "Siste",
                next: "Neste",
                previous: "Forrige"
            },
            emptyTable: "Ingen data tilgjengelig"
        },

        // DOM structure for Bootstrap 5
        dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",

        // Disable auto-width for better control
        autoWidth: false
    };

    // ==============================================
    // INITIALIZE OVERDUE TABLE
    // ==============================================
    if ($('#overdueTable').length) {
        $('#overdueTable').DataTable($.extend({}, commonSettings, {
            order: [[3, 'asc']], // Sort by due date ascending (oldest first)
            columnDefs: [
                { targets: [4, 5, 6], className: 'text-end' }, // Right-align amount columns
                { targets: [-1], orderable: false } // Disable sorting on action column
            ]
        }));
    }

    // ==============================================
    // INITIALIZE PENDING TABLE
    // ==============================================
    if ($('#pendingTable').length) {
        $('#pendingTable').DataTable($.extend({}, commonSettings, {
            order: [[3, 'asc']], // Sort by due date ascending
            columnDefs: [
                { targets: [4, 5, 6], className: 'text-end' },
                { targets: [-1], orderable: false }
            ]
        }));
    }

    // ==============================================
    // INITIALIZE PAID TABLE
    // ==============================================
    if ($('#paidTable').length) {
        $('#paidTable').DataTable($.extend({}, commonSettings, {
            order: [[3, 'desc']], // Sort by payment date descending (newest first)
            columnDefs: [
                { targets: [4, 5, 6], className: 'text-end' },
                { targets: [-1], orderable: false }
            ]
        }));
    }

    // ==============================================
    // UTILITY FUNCTIONS
    // ==============================================

    // Format currency (Norwegian Krone)
    window.formatCurrency = function(amount) {
        return new Intl.NumberFormat('nb-NO', {
            style: 'currency',
            currency: 'NOK'
        }).format(amount);
    };

    // Format date (Norwegian format)
    window.formatDate = function(dateString) {
        if (!dateString) return '-';
        var date = new Date(dateString);
        return date.toLocaleDateString('nb-NO', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    };

    // ==============================================
    // CARD ANIMATIONS
    // ==============================================

    // Add subtle entrance animation to cards
    $('.invoice-card').each(function(index) {
        $(this).css({
            'opacity': '0',
            'transform': 'translateY(20px)'
        }).delay(index * 100).animate({
            'opacity': '1'
        }, 400).css('transform', 'translateY(0)');
    });

    // ==============================================
    // CONFIRM DIALOGS
    // ==============================================

    // Confirm before sending invoice
    $(document).on('submit', 'form[data-confirm]', function(e) {
        var message = $(this).data('confirm') || 'Are you sure?';
        if (!confirm(message)) {
            e.preventDefault();
            return false;
        }
    });

    // ==============================================
    // TOOLTIPS (Bootstrap 5)
    // ==============================================

    // Initialize Bootstrap tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // ==============================================
    // REFRESH DATA (for future AJAX implementation)
    // ==============================================

    window.refreshInvoiceData = function() {
        // Placeholder for future AJAX refresh
        location.reload();
    };

    console.log('Invoice Dashboard initialized');
});
