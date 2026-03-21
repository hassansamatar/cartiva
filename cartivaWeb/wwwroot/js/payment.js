@section Scripts {
    <script>
        $(document).ready(function() {
            let stripe = null;

        // Initialize Stripe only when needed
        function initStripe() {
                if (!stripe) {
            stripe = Stripe('@Model.PublishableKey');
                }
        return stripe;
            }

        // Toggle between card and Vipps
        $('input[name="paymentMethod"]').change(function() {
                if ($(this).val() === 'vipps') {
            $('#payment-form-container').hide();
        $('#vipps-message').show();
        // Destroy Stripe elements when not needed
        if (window.paymentElement) {
            window.paymentElement.destroy();
                    }
                } else {
            $('#payment-form-container').show();
        $('#vipps-message').hide();
        // Initialize Stripe elements only when switching to card
        initializeCardPayment();
                }
            });

        // Initialize card payment
        function initializeCardPayment() {
                if (window.paymentElement) return;

        const stripeInstance = initStripe();
        const elements = stripeInstance.elements({
            clientSecret: '@Model.ClientSecret',
        appearance: {theme: 'stripe' } 
                });
        window.paymentElement = elements.create('payment');
        window.paymentElement.mount('#payment-element');
            }

        // Initialize card payment by default
        if ($('#cardPayment').is(':checked')) {
            initializeCardPayment();
            }

        const form = document.getElementById('payment-form');
        const submitButton = document.getElementById('submit-button');
        const buttonText = document.getElementById('button-text');
        const spinner = document.getElementById('spinner');
        const messageDiv = document.getElementById('payment-message');

            // Card payment submission
            form.addEventListener('submit', async (event) => {
            event.preventDefault();
        submitButton.disabled = true;
        buttonText.style.display = 'none';
        spinner.style.display = 'inline-block';
        messageDiv.style.display = 'none';

        try {
                    const stripeInstance = initStripe();
        const {error} = await stripeInstance.confirmPayment({
            elements: window.paymentElement ? {elements: window.paymentElement } : undefined,
        confirmParams: {
            return_url: '@Url.Action("ConfirmPayment", "Order", new {orderId = Model.Order.Id})'
                        },
        redirect: 'if_required'
                    });

        if (error) {
            messageDiv.style.display = 'block';
        messageDiv.textContent = error.message;
        submitButton.disabled = false;
        buttonText.style.display = 'inline';
        spinner.style.display = 'none';
                    } else {
            window.location.href = '@Url.Action("Receipt", "Order", new { id = Model.Order.Id })';
                    }
                } catch (err) {
            messageDiv.style.display = 'block';
        messageDiv.textContent = 'An unexpected error occurred. Please try again.';
        submitButton.disabled = false;
        buttonText.style.display = 'inline';
        spinner.style.display = 'none';
                }
            });

        // Vipps button click
        $('#vipps-button').click(async function() {
                const vippsButton = $(this);
        vippsButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

        try {
                    const response = await fetch('/Customer/Order/CreateVippsSession', {
            method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
        body: JSON.stringify({orderId: @Model.Order.Id })
                    });

        if (!response.ok) {
                        const errorText = await response.text();
        throw new Error(errorText || 'Failed to create Vipps session');
                    }

        const session = await response.json();

        if (!session.id) {
                        throw new Error('No session ID returned');
                    }

        const stripeInstance = initStripe();
        const {error} = await stripeInstance.redirectToCheckout({sessionId: session.id });

        if (error) {
                        throw error;
                    }
                } catch (err) {
            alert('Failed to start Vipps payment: ' + err.message);
        console.error('Vipps error:', err);
        vippsButton.prop('disabled', false).html('<i class="bi bi-phone me-2"></i>Continue with Vipps');
                }
            });
        });
    </script>
}