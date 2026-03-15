document.addEventListener("DOMContentLoaded", function () {

    const streetInput = document.getElementById("street");
    const postalCodeInput = document.getElementById("postalCode");
    const cityInput = document.getElementById("city");
    const suggestionsContainer = document.getElementById("addressSuggestions");

    function debounce(func, delay) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    async function fetchAddresses(query) {

        if (!query || query.length < 3) {
            suggestionsContainer.innerHTML = "";
            return;
        }

        try {
            const response = await fetch(`/api/address/search?q=${encodeURIComponent(query)}`);
            const data = await response.json();

            const addresses = data.adresser ?? [];

            renderSuggestions(addresses);
        } catch (error) {
            console.error("Address lookup failed:", error);
            suggestionsContainer.innerHTML = "";
        }
    }

    function renderSuggestions(addresses) {

        suggestionsContainer.innerHTML = "";

        addresses.forEach(a => {

            const item = document.createElement("button");

            item.type = "button";
            item.className = "list-group-item list-group-item-action";

            item.textContent =
                `${a.adressetekst ?? ""}, ${a.postnummer ?? ""} ${a.poststed ?? ""}`;

            item.onclick = () => {

                streetInput.value = a.adressetekst ?? "";
                postalCodeInput.value = a.postnummer ?? "";
                cityInput.value = a.poststed ?? "";

                suggestionsContainer.innerHTML = "";
            };

            suggestionsContainer.appendChild(item);
        });
    }

    streetInput.addEventListener("input", debounce((e) => {
        fetchAddresses(e.target.value);
    }, 300));

});