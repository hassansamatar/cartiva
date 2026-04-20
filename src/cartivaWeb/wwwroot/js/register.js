document.addEventListener("DOMContentLoaded", function () {
    var streetInput = document.getElementById("street");
    var postalCodeInput = document.getElementById("postalCode");
    var cityInput = document.getElementById("city");
    var stateInput = document.getElementById("state");
    var countryInput = document.getElementById("country");
    var suggestionsContainer = document.getElementById("addressSuggestions");

    var debounceTimer;
    var currentRequest = null;
    var isSelectingAddress = false; // Flag to prevent re-fetching after selection

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
            suggestionsContainer.innerHTML = '<div class="text-muted text-center p-2"><i class="bi bi-hourglass-split me-1"></i>Searching...</div>';
            suggestionsContainer.style.display = "block";
        }
    }

    function hideSuggestions() {
        if (suggestionsContainer) {
            suggestionsContainer.style.display = "none";
            suggestionsContainer.innerHTML = "";
        }
    }

    async function fetchAddresses(query) {
        // Don't fetch if we're programmatically setting the value after selection
        if (isSelectingAddress) {
            return;
        }

        if (!query || query.length < 3) {
            hideSuggestions();
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
                    suggestionsContainer.innerHTML = '<div class="text-warning text-center p-2"><i class="bi bi-exclamation-triangle me-1"></i>Unable to fetch addresses. Please enter manually.</div>';
                    setTimeout(function () {
                        hideSuggestions();
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
            hideSuggestions();
            return;
        }

        // Apply scrollable styles
        suggestionsContainer.style.maxHeight = "250px";
        suggestionsContainer.style.overflowY = "auto";
        suggestionsContainer.style.overflowX = "hidden";

        // Show up to 10 results (scrollable)
        var results = addresses.slice(0, 10);
        results.forEach(function (a) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "list-group-item list-group-item-action d-flex align-items-center";

            var streetText = a.adressetekst || "";
            var postalCode = a.postnummer || "";
            var city = a.poststed || "";

            // Create formatted content with icon
            item.innerHTML = 
                '<i class="bi bi-geo-alt text-primary me-2"></i>' +
                '<div>' +
                    '<div class="fw-medium">' + streetText + '</div>' +
                    '<small class="text-muted">' + postalCode + ' ' + city + '</small>' +
                '</div>';

            item.onclick = function (e) {
                e.preventDefault();
                e.stopPropagation();

                // Set flag to prevent re-fetching
                isSelectingAddress = true;

                // Clear any pending debounce timer
                clearTimeout(debounceTimer);

                // Cancel any in-flight request
                if (currentRequest) {
                    currentRequest.abort();
                    currentRequest = null;
                }

                // Populate form fields
                streetInput.value = streetText;
                postalCodeInput.value = postalCode;
                cityInput.value = city;
                if (stateInput) stateInput.value = city;
                if (countryInput) countryInput.value = "Norway";

                // Hide suggestions immediately
                hideSuggestions();

                // Trigger validation events (but flag prevents re-fetch)
                streetInput.dispatchEvent(new Event('change', { bubbles: true }));
                postalCodeInput.dispatchEvent(new Event('change', { bubbles: true }));
                cityInput.dispatchEvent(new Event('change', { bubbles: true }));
                if (countryInput) countryInput.dispatchEvent(new Event('change', { bubbles: true }));

                // Reset flag after a short delay to allow future manual typing
                setTimeout(function () {
                    isSelectingAddress = false;
                }, 100);
            };

            suggestionsContainer.appendChild(item);
        });

        // Add result count indicator if there are many results
        if (addresses.length > 10) {
            var moreIndicator = document.createElement("div");
            moreIndicator.className = "text-muted text-center small p-2 border-top";
            moreIndicator.innerHTML = '<i class="bi bi-three-dots"></i> ' + (addresses.length - 10) + ' more results. Keep typing to narrow down.';
            suggestionsContainer.appendChild(moreIndicator);
        }

        suggestionsContainer.style.display = "block";
    }

    if (streetInput) {
        streetInput.addEventListener("input", debounce(function (e) {
            // Don't fetch if selecting address programmatically
            if (isSelectingAddress) return;

            var query = e.target.value.trim();
            fetchAddresses(query);
        }, 400)); // Slightly faster debounce for better UX

        // Also handle focus to show suggestions if there's already text
        streetInput.addEventListener("focus", function () {
            if (!isSelectingAddress && streetInput.value.trim().length >= 3) {
                // Only re-fetch if suggestions aren't already visible
                if (suggestionsContainer && suggestionsContainer.style.display !== "block") {
                    fetchAddresses(streetInput.value.trim());
                }
            }
        });
    }

    // Close suggestions when clicking outside
    document.addEventListener("click", function (event) {
        if (suggestionsContainer && !suggestionsContainer.contains(event.target) && event.target !== streetInput) {
            hideSuggestions();
        }
    });

    // Close suggestions on Escape key
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && suggestionsContainer) {
            hideSuggestions();
            if (streetInput) streetInput.blur();
        }
    });
});
