$(document).ready(function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert('Tracking link copied to clipboard!');
    }).catch(function () {
        alert('Failed to copy link');
    });
}
