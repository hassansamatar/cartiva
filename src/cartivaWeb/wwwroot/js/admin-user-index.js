function deactivateUser(id) {
    swalConfirm('Deactivate this user?', 'They will no longer be able to log in.', 'Yes, deactivate!', 'Cancel').then(function (result) {
        if (result.isConfirmed) {
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
                swalToast('Deactivation failed', 'error');
            });
        }
    });
}

function activateUser(id) {
    swalConfirm('Activate this user?', 'They will be able to log in again.', 'Yes, activate!', 'Cancel').then(function (result) {
        if (result.isConfirmed) {
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
                swalToast('Activation failed', 'error');
            });
        }
    });
}
