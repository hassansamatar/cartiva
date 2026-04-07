function cancelOrder(orderId) {
    if (confirm('Are you sure you want to cancel this order?')) {
        var url = document.getElementById('cancelOrderUrl').value;
        $.post(url, { id: orderId })
            .done(function () { location.reload(); });
    }
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert('Tracking link copied to clipboard!');
    }).catch(function () {
        alert('Failed to copy link');
    });
}
