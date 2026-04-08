$(document).ready(function () {
    var paymentConfig = document.getElementById('payment-config');
    var publishableKey = paymentConfig.dataset.publishableKey;
    var clientSecret = paymentConfig.dataset.clientSecret;
    var returnUrl = paymentConfig.dataset.returnUrl;

    // Toggle between payment methods
    $('input[name="paymentMethod"]').change(function () {
        if ($(this).val() === 'vipps') {
            $('#payment-form-container').hide();
            $('#vipps-message').show();
        } else {
            $('#payment-form-container').show();
            $('#vipps-message').hide();
        }
    });

    // Initialize Stripe
    var stripe = Stripe(publishableKey);

    var elements = stripe.elements({
        clientSecret: clientSecret,
        appearance: {
            theme: 'stripe',
            variables: {
                colorPrimary: '#198754',
                colorBackground: '#ffffff',
                colorText: '#1e1e1e',
                colorDanger: '#dc3545',
                fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
                borderRadius: '8px'
            },
            rules: {
                '.Label': { fontWeight: '500', marginBottom: '8px' },
                '.Input': { padding: '12px', border: '1px solid #ced4da' },
                '.Input:focus': { borderColor: '#198754', boxShadow: '0 0 0 0.25rem rgba(25, 135, 84, 0.25)' }
            }
        }
    });

    var paymentElement = elements.create('payment');
    paymentElement.mount('#payment-element');

    var form = document.getElementById('payment-form');
    var submitButton = document.getElementById('submit-button');
    var buttonText = document.getElementById('button-text');
    var spinner = document.getElementById('spinner');
    var messageDiv = document.getElementById('payment-message');

    form.addEventListener('submit', async function (event) {
        event.preventDefault();

        submitButton.disabled = true;
        buttonText.style.display = 'none';
        spinner.style.display = 'inline-block';

        var result = await stripe.confirmPayment({
            elements: elements,
            confirmParams: { return_url: returnUrl },
            redirect: 'always'
        });

        if (result.error) {
            messageDiv.style.display = 'block';
            messageDiv.textContent = result.error.message;
            submitButton.disabled = false;
            buttonText.style.display = 'inline';
            spinner.style.display = 'none';
        }
        // On success, Stripe will automatically redirect to returnUrl
    });
});

function copyToClipboard(text) {
    swalCopyToClipboard(text, 'Tracking link copied to clipboard!');
}
