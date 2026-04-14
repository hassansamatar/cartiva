function cancelOrder(orderId) {
    swalConfirm('Cancel this order?', 'This action cannot be undone.', 'Yes, cancel it!', 'No, keep it').then(function (result) {
        if (result.isConfirmed) {
            var url = document.getElementById('cancelOrderUrl').value;
            $.post(url, { id: orderId })
                .done(function () { location.reload(); });
        }
    });
}

function copyToClipboard(text) {
    swalCopyToClipboard(text, 'Tracking link copied to clipboard!');
}
