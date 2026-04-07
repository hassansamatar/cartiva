function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert('Tracking link copied to clipboard!');
    }).catch(function () {
        alert('Failed to copy link');
    });
}
