document.addEventListener("DOMContentLoaded", function () {
    const streetInput = document.getElementById("street");
    const postalCodeInput = document.getElementById("postalCode");
    const cityInput = document.getElementById("city");
    const stateInput = document.getElementById("state");
    const countryInput = document.getElementById("country");
    const suggestionsContainer = document.getElementById("addressSuggestions");

    let debounceTimer;
    let currentRequest = null;

    function debounce(func, delay) {
        return function (...args) {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    function showLoading() {
        if (suggestionsContainer) {
            suggestionsContainer.innerHTML = '<div class="text-muted text-center p-2">Searching...</div>';
            suggestionsContainer.style.display = "block";
        }
    }

    async function fetchAddresses(query) {
        if (!query || query.length < 3) {
            suggestionsContainer.style.display = "none";
            return;
        }

        if (currentRequest) {
            currentRequest.abort();
        }

        showLoading();

        try {
            const controller = new AbortController();
            currentRequest = controller;
            const timeoutId = setTimeout(() => controller.abort(), 10000);

            // Use your local API endpoint
            const response = await fetch(`/api/address/search?q=${encodeURIComponent(query)}`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

            const data = await response.json();
            const addresses = data.adresser || [];

            renderSuggestions(addresses);
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error("Address lookup failed:", error);
                if (suggestionsContainer) {
                    suggestionsContainer.innerHTML = '<div class="text-warning text-center p-2">Unable to fetch addresses. Please enter manually.</div>';
                    setTimeout(() => {
                        suggestionsContainer.style.display = "none";
                    }, 2000);
                }
            }
        } finally {
            currentRequest = null;
        }
    }

    function renderSuggestions(addresses) {
        if (!suggestionsContainer) return;
        suggestionsContainer.innerHTML = "";

        if (!addresses || addresses.length === 0) {
            suggestionsContainer.style.display = "none";
            return;
        }

        // Show only first 5
        const top = addresses.slice(0, 5);
        top.forEach(a => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "list-group-item list-group-item-action";

            const streetText = a.adressetekst || "";
            const postalCode = a.postnummer || "";
            const city = a.poststed || "";
            item.textContent = `${streetText}, ${postalCode} ${city}`.trim();

            item.onclick = (e) => {
                e.preventDefault();

                streetInput.value = streetText;
                postalCodeInput.value = postalCode;
                cityInput.value = city;
                if (stateInput) stateInput.value = city;
                if (countryInput) countryInput.value = "Norway";

                suggestionsContainer.style.display = "none";
                suggestionsContainer.innerHTML = "";

                // Trigger validation events
                streetInput.dispatchEvent(new Event('input', { bubbles: true }));
                postalCodeInput.dispatchEvent(new Event('input', { bubbles: true }));
                cityInput.dispatchEvent(new Event('input', { bubbles: true }));
                if (countryInput) countryInput.dispatchEvent(new Event('input', { bubbles: true }));
            };

            suggestionsContainer.appendChild(item);
        });

        suggestionsContainer.style.display = "block";
    }

    if (streetInput) {
        streetInput.addEventListener("input", debounce((e) => {
            const query = e.target.value.trim();
            fetchAddresses(query);
        }, 500));
    }

    document.addEventListener("click", function (event) {
        if (suggestionsContainer && !suggestionsContainer.contains(event.target) && event.target !== streetInput) {
            suggestionsContainer.style.display = "none";
        }
    });
});