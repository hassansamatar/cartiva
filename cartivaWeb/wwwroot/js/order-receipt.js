$(document).ready(function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

function copyToClipboard(text) {
    swalCopyToClipboard(text, 'Tracking link copied to clipboard!');
}
