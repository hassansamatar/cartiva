function deactivateUser(id) {
    if (confirm('Are you sure you want to deactivate this user?')) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        var url = document.getElementById('deactivateUrl').value;

        $.post(url, {
            id: id,
            __RequestVerificationToken: token
        })
        .done(function () {
            location.reload();
        })
        .fail(function () {
            alert('Deactivation failed');
        });
    }
}

function activateUser(id) {
    if (confirm('Are you sure you want to activate this user?')) {
        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        var url = document.getElementById('activateUrl').value;

        $.post(url, {
            id: id,
            __RequestVerificationToken: token
        })
        .done(function () {
            location.reload();
        })
        .fail(function () {
            alert('Activation failed');
        });
    }
}
