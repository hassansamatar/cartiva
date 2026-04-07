document.addEventListener("DOMContentLoaded", function () {
    var streetInput = document.getElementById("street");
    var postalCodeInput = document.getElementById("postalCode");
    var cityInput = document.getElementById("city");
    var stateInput = document.getElementById("state");
    var countryInput = document.getElementById("country");
    var suggestionsContainer = document.getElementById("addressSuggestions");

    var debounceTimer;
    var currentRequest = null;

    function debounce(func, delay) {
        return function () {
            var args = arguments;
            var context = this;
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(function () { func.apply(context, args); }, delay);
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
            var controller = new AbortController();
            currentRequest = controller;
            var timeoutId = setTimeout(function () { controller.abort(); }, 10000);

            // Use local API endpoint
            var response = await fetch("/api/address/search?q=" + encodeURIComponent(query), {
                signal: controller.signal
            });
            clearTimeout(timeoutId);

            if (!response.ok) throw new Error("HTTP error! status: " + response.status);

            var data = await response.json();
            var addresses = data.adresser || [];

            renderSuggestions(addresses);
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error("Address lookup failed:", error);
                if (suggestionsContainer) {
                    suggestionsContainer.innerHTML = '<div class="text-warning text-center p-2">Unable to fetch addresses. Please enter manually.</div>';
                    setTimeout(function () {
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
        var top = addresses.slice(0, 5);
        top.forEach(function (a) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "list-group-item list-group-item-action";

            var streetText = a.adressetekst || "";
            var postalCode = a.postnummer || "";
            var city = a.poststed || "";
            item.textContent = (streetText + ", " + postalCode + " " + city).trim();

            item.onclick = function (e) {
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
        streetInput.addEventListener("input", debounce(function (e) {
            var query = e.target.value.trim();
            fetchAddresses(query);
        }, 500));
    }

    document.addEventListener("click", function (event) {
        if (suggestionsContainer && !suggestionsContainer.contains(event.target) && event.target !== streetInput) {
            suggestionsContainer.style.display = "none";
        }
    });
});
