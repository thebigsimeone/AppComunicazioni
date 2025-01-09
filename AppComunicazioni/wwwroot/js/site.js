// Abilita spinner al submit
document.addEventListener('submit', function (e) {
    const form = e.target;
    if (form.tagName === 'FORM') {
        const submitButton = form.querySelector(':submit');
        if (submitButton) {
            submitButton.disabled = true;
            const spinner = document.createElement('div');
            spinner.className = 'spinner-border';
            spinner.setAttribute('role', 'status');
            document.body.appendChild(spinner);
        }
    }
});

// Logica per la gestione del FileName
document.getElementById("FileName")?.addEventListener("input", function () {
    const fileName = this.value;
    const parts = fileName.split("_");
    const protocolPart = parts.length > 0 ? parts[parts.length - 1] : "";
    const servicePart = parts.length > 2 ? parts[2] : ""; // Terza parte del nome file

    // Estrai il numero di protocollo
    const protocolNumber = parseInt(protocolPart, 10);
    if (!isNaN(protocolNumber)) {
        document.getElementById("NProtocol").value = protocolNumber;
    }

    // Estrai il servizio e gestisci il caso "35" -> "S035"
    if (servicePart) {
        const normalizedService = servicePart === "035" ? "S035" : servicePart;
        const serviceDropdown = document.getElementById("Servizio");
        const optionToSelect = Array.from(serviceDropdown.options).find(option => option.value === normalizedService);
        if (optionToSelect) {
            serviceDropdown.value = normalizedService;
        }
    }
});

// Logica per aggiornare le opzioni di servizio
function updateServizioOptions() {
    const codCor = document.getElementById("codCorSelect")?.value;
    const servizioSelect = document.getElementById("servizioSelect");

    if (!codCor || !servizioSelect) return;

    // Pulisce le opzioni precedenti
    servizioSelect.innerHTML = '<option value="">-- Seleziona Servizio --</option>';

    // Aggiunge nuove opzioni in base al CodCor selezionato
    let options = [];
    switch (codCor) {
        case "CESSIONI":
            options = ["BA2", "BAN", "CEP"];
            break;
        case "DATAVIZ":
            options = ["APP", "APT", "BA2", "DIM", "DV", "ERE", "MA7", "MIM", "PDL", "S035", "VL1", "VLA", "VL3", "VPP", "VSA", "VSS"];
            break;
        case "FORZA":
            options = ["DP1", "VED"];
            break;
        default:
            options = [];
    }

    options.forEach(function (option) {
        const opt = document.createElement("option");
        opt.value = option;
        opt.text = option;
        servizioSelect.add(opt);
    });
}

// Logica per pulire l'input DateF
function clearDateF() {
    const dateFInput = document.getElementById("DateF");
    if (dateFInput) {
        dateFInput.value = '';
    }
}

document.addEventListener("DOMContentLoaded", function () {
    const toastElement = document.getElementById("toastMessage");
    const toastBody = document.getElementById("toastBody");
    const toastContainer = document.getElementById("toastContainer");
    const submitButton = document.getElementById("submitButton");

    // Mostra lo spinner durante l'invio del form
    const form = document.querySelector("form");
    if (form) {
        form.addEventListener("submit", function () {
            if (submitButton) {
                submitButton.disabled = true;
            }
            const spinner = document.createElement("div");
            spinner.className = "spinner-border text-primary";
            spinner.setAttribute("role", "status");
            spinner.innerHTML = '<span class="sr-only"></span>';
            document.body.appendChild(spinner);
        });
    }

    // Mostra il toast dopo il caricamento della pagina se TempData contiene un messaggio
    if (toastElement && toastBody && toastContainer) {
        const toast = new bootstrap.Toast(toastElement, { delay: 5000 });

        const message = toastBody.dataset.message;
        if (message) {
            toastBody.textContent = message;
            toast.show();
        }
    }
});