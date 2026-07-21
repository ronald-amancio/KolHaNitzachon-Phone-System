//const RECIPIENT_API = "https://localhost:7218/api/Recipients";
//const RECORDING_API = "https://localhost:7218/api/Recordings/upload";

const RECIPIENT_API = "/api/Recipients";
const RECORDING_API = "/api/Recordings/upload";

const table = document.getElementById("recipientTable");
const tableContainer = document.getElementById("tableContainer");
const loadingState = document.getElementById("loadingState");
const emptyState = document.getElementById("emptyState");
const form = document.getElementById("recipientForm");
const modal = document.getElementById("recipientModal");
const addRecipientButton = document.getElementById("addRecipientButton");
const emptyAddButton = document.getElementById("emptyAddButton");
const closeModalButton = document.getElementById("closeModalButton");
const cancelButton = document.getElementById("cancelEdit");
const saveButton = document.getElementById("saveButton");
const saveButtonText = document.getElementById("saveButtonText");
const existingRecordingLink = document.getElementById("existingRecordingLink");

let toastTimer;
let lastFocusedElement = null;

form.addEventListener("submit", saveRecipient);
addRecipientButton.addEventListener("click", openAddModal);
emptyAddButton.addEventListener("click", openAddModal);
closeModalButton.addEventListener("click", closeModal);
cancelButton.addEventListener("click", closeModal);

modal.addEventListener("click", event => {
    if (event.target.hasAttribute("data-close-modal")) {
        closeModal();
    }
});

document.addEventListener("keydown", event => {
    if (event.key === "Escape" && modal.classList.contains("show")) {
        closeModal();
    }
});

loadRecipients();

async function loadRecipients() {
    setListState("loading");

    try {
        const response = await fetch(RECIPIENT_API);

        if (!response.ok) {
            throw new Error("Unable to load recipients.");
        }

        const data = await response.json();
        renderTable(Array.isArray(data) ? data : []);
    }
    catch (error) {
        console.error(error);
        setListState("empty");
        showToast(error.message, "error");
    }
}

function renderTable(data) {
    table.innerHTML = "";

    if (data.length === 0) {
        setListState("empty");
        return;
    }

    const rows = data.map(recipient => `
        <tr>
            <td><span class="code-badge">${escapeHtml(recipient.code)}</span></td>
            <td>
                <div class="recipient-name">${escapeHtml(recipient.name)}</div>
            </td>
            <td>${formatDate(recipient.startDate) || "—"}</td>
            <td>${formatDate(recipient.endDate) || "—"}</td>
            <td>${renderRecording(recipient.nameRecordingUrl)}</td>
            <td>
                <div class="action-group">
                    <button type="button" class="action edit" onclick="editRecipient('${escapeAttribute(recipient.id)}')">Edit</button>
                    <button type="button" class="action delete" onclick="deleteRecipient('${escapeAttribute(recipient.id)}')">Delete</button>
                </div>
            </td>
        </tr>
    `);

    table.innerHTML = rows.join("");
    setListState("table");
}

function renderRecording(url) {
    if (!url) {
        return '<span class="muted-text">No recording</span>';
    }

    const safeUrl = escapeAttribute(url);
    return `<a href="${safeUrl}" target="_blank" rel="noopener" class="play-link">▶ Play</a>`;
}

function openAddModal() {
    resetForm();
    document.getElementById("formTitle").textContent = "Add Recipient";
    saveButtonText.textContent = "Save Recipient";
    openModal();
}

async function editRecipient(id) {
    try {
        const response = await fetch(`${RECIPIENT_API}/${encodeURIComponent(id)}`);

        if (!response.ok) {
            throw new Error("Unable to load recipient details.");
        }

        const recipient = await response.json();

        resetForm();
        document.getElementById("recipientId").value = recipient.id ?? "";
        document.getElementById("existingRecordingUrl").value = recipient.nameRecordingUrl ?? "";
        document.getElementById("code").value = recipient.code ?? "";
        document.getElementById("name").value = recipient.name ?? "";
        document.getElementById("startDate").value = toDateInputValue(recipient.startDate);
        document.getElementById("endDate").value = toDateInputValue(recipient.endDate);
        document.getElementById("formTitle").textContent = "Edit Recipient";
        saveButtonText.textContent = "Update Recipient";
        updateExistingRecordingLink(recipient.nameRecordingUrl);

        openModal();
    }
    catch (error) {
        console.error(error);
        showToast(error.message, "error");
    }
}

async function saveRecipient(event) {
    event.preventDefault();

    if (!validateForm()) {
        return;
    }

    setSavingState(true);

    try {
        const id = document.getElementById("recipientId").value;
        let recordingUrl = await uploadRecording();

        if (id && !recordingUrl) {
            recordingUrl = document.getElementById("existingRecordingUrl").value;
        }

        const body = {
            code: Number(document.getElementById("code").value),
            name: document.getElementById("name").value.trim(),
            nameRecordingUrl: recordingUrl,
            startDate: document.getElementById("startDate").value,
            endDate: document.getElementById("endDate").value || null
        };

        const isEditing = Boolean(id);
        const response = await fetch(isEditing ? `${RECIPIENT_API}/${encodeURIComponent(id)}` : RECIPIENT_API, {
            method: isEditing ? "PUT" : "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(body)
        });

        if (!response.ok) {
            const message = await readErrorMessage(response);
            throw new Error(message || (isEditing ? "Unable to update recipient." : "Unable to save recipient."));
        }

        closeModal();
        await loadRecipients();
        showToast(isEditing ? "Recipient updated successfully." : "Recipient saved successfully.");
    }
    catch (error) {
        console.error(error);
        showToast(error.message, "error");
    }
    finally {
        setSavingState(false);
    }
}

async function deleteRecipient(id) {
    const confirmed = confirm("Delete this recipient? This action cannot be undone.");

    if (!confirmed) {
        return;
    }

    try {
        const response = await fetch(`${RECIPIENT_API}/${encodeURIComponent(id)}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            throw new Error("Unable to delete recipient.");
        }

        await loadRecipients();
        showToast("Recipient deleted successfully.");
    }
    catch (error) {
        console.error(error);
        showToast(error.message, "error");
    }
}

async function uploadRecording() {
    const fileInput = document.getElementById("recording");

    if (!fileInput.files || fileInput.files.length === 0) {
        return "";
    }

    const file = fileInput.files[0];

    const isMp3 =
        file.type === "audio/mpeg" ||
        file.type === "audio/mp3" ||
        file.type === "application/octet-stream" ||
        file.name.toLowerCase().endsWith(".mp3");

    if (!isMp3) {
        throw new Error("Please select a valid MP3 recording.");
    }

    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(RECORDING_API, {
        method: "POST",
        body: formData
    });

    if (!response.ok) {
        const message = await getApiErrorMessage(
            response,
            "Unable to upload the recording.");

        throw new Error(message);
    }

    const result = await response.json();

    if (!result.url) {
        throw new Error(
            "The recording was uploaded, but no recording URL was returned."
        );
    }

    return result.url;
}

function openModal() {
    lastFocusedElement = document.activeElement;
    modal.classList.add("show");
    modal.setAttribute("aria-hidden", "false");
    document.body.classList.add("modal-open");

    window.setTimeout(() => {
        document.getElementById("code").focus();
    }, 50);
}

function closeModal() {
    modal.classList.remove("show");
    modal.setAttribute("aria-hidden", "true");
    document.body.classList.remove("modal-open");
    resetForm();

    if (lastFocusedElement instanceof HTMLElement) {
        lastFocusedElement.focus();
    }
}

function resetForm() {
    form.reset();
    document.getElementById("recipientId").value = "";
    document.getElementById("existingRecordingUrl").value = "";
    document.getElementById("formTitle").textContent = "Add Recipient";
    saveButtonText.textContent = "Save Recipient";
    updateExistingRecordingLink("");
}

function updateExistingRecordingLink(url) {
    if (!url) {
        existingRecordingLink.hidden = true;
        existingRecordingLink.removeAttribute("href");
        return;
    }

    existingRecordingLink.href = url;
    existingRecordingLink.hidden = false;
}

function validateForm() {
    const code = document.getElementById("code").value.trim();
    const name = document.getElementById("name").value.trim();
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;

    if (!code || Number(code) <= 0) {
        showToast("Enter a valid recipient code.", "error");
        document.getElementById("code").focus();
        return false;
    }

    if (!name) {
        showToast("Name is required.", "error");
        document.getElementById("name").focus();
        return false;
    }

    if (!startDate) {
        showToast("Start Date is required.", "error");
        document.getElementById("startDate").focus();
        return false;
    }

    if (endDate && new Date(endDate) < new Date(startDate)) {
        showToast("End Date must be on or after Start Date.", "error");
        document.getElementById("endDate").focus();
        return false;
    }

    return true;
}

function setSavingState(isSaving) {
    saveButton.disabled = isSaving;
    cancelButton.disabled = isSaving;
    saveButtonText.textContent = isSaving
        ? "Saving..."
        : (document.getElementById("recipientId").value ? "Update Recipient" : "Save Recipient");
}

function setListState(state) {
    loadingState.hidden = state !== "loading";
    emptyState.hidden = state !== "empty";
    tableContainer.hidden = state !== "table";
}

function formatDate(date) {
    if (!date) {
        return "";
    }

    const value = new Date(date);

    if (Number.isNaN(value.getTime())) {
        return String(date).substring(0, 10);
    }

    return new Intl.DateTimeFormat("en", {
        year: "numeric",
        month: "short",
        day: "2-digit"
    }).format(value);
}

function toDateInputValue(date) {
    return date ? String(date).substring(0, 10) : "";
}

function showToast(message, type = "success") {
    const toast = document.getElementById("toast");
    const toastMessage = document.getElementById("toastMessage");

    window.clearTimeout(toastTimer);
    toast.className = `toast ${type} show`;
    toastMessage.textContent = message;

    toastTimer = window.setTimeout(() => {
        toast.classList.remove("show");
    }, 3500);
}

async function readErrorMessage(response) {
    try {
        const contentType = response.headers.get("content-type") || "";

        if (contentType.includes("application/json")) {
            const data = await response.json();
            return data.detail || data.message || data.title || data.error || "";
        }

        return await response.text();
    }
    catch {
        return "";
    }
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function escapeAttribute(value) {
    return escapeHtml(value);
}

async function getApiErrorMessage(
    response,
    fallbackMessage
) {
    try {
        const contentType =
            response.headers.get("content-type") || "";

        if (contentType.includes("application/json") ||
            contentType.includes("application/problem+json")) {
            const result = await response.json();

            return result.detail ||
                result.message ||
                result.title ||
                fallbackMessage;
        }
    } catch (error) {
        console.error("Unable to parse API error response.", error);
    }

    return fallbackMessage;
}