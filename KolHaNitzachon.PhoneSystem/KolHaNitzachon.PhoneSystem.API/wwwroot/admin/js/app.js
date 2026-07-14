const RECIPIENT_API = "https://localhost:7218/api/Recipients";
const RECORDING_API = "https://localhost:7218/api/Recordings/upload";

const table = document.getElementById("recipientTable");

const form = document.getElementById("recipientForm");

const cancelButton = document.getElementById("cancelEdit");

let editingId = null;

loadRecipients();

cancelButton.style.display = "none";

form.addEventListener("submit", saveRecipient);

cancelButton.addEventListener("click", clearForm);

async function loadRecipients() {

    const response = await fetch(RECIPIENT_API);

    const data = await response.json();

    renderTable(data);

}

function renderTable(data) {
    table.innerHTML = "";
    data.forEach(r => {
        table.innerHTML += `
            <tr>
                <td>${r.code}</td>
                <td>${r.name}</td>
                <td>${formatDate(r.startDate)}</td>
                <td>${formatDate(r.endDate)}</td>
                <td>${r.nameRecordingUrl?
                    `<a href="${r.nameRecordingUrl}"
                        target="_blank"
                        class="play-link">
                        ▶ Play
                        </a>` : "" }
                </td>
                <td>
                    <button class="action edit"
                        onclick="editRecipient('${r.id}')">
                        Edit
                    </button>

                    <button class="action delete"
                        onclick="deleteRecipient('${r.id}')">
                        Delete
                    </button>
                </td>
            </tr>
            `;
    });
}

async function saveRecipient(e) {

    e.preventDefault();
    if (!validateForm()) return;

    try {

        const id = document.getElementById("recipientId").value;

        let recordingUrl = "";

        // Upload recording first (if selected)
        recordingUrl = await uploadRecording();

        // If editing and no new recording was uploaded,
        // keep the existing URL.
        if (id !== "" && recordingUrl === "") {
            recordingUrl = document.getElementById("existingRecordingUrl").value;
        }

        const body = {

            code: Number(document.getElementById("code").value),

            name: document.getElementById("name").value,

            nameRecordingUrl: recordingUrl,

            startDate: document.getElementById("startDate").value,

            endDate: document.getElementById("endDate").value || null

        };

        let response;

        if (id === "") {

            response = await fetch(RECIPIENT_API, {

                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify(body)

            });

            if (!response.ok)
                throw new Error("Unable to save recipient.");

            showToast("✓ Recipient saved successfully.");

        }
        else {

            response = await fetch(`${RECIPIENT_API}/${id}`, {

                method: "PUT",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify(body)

            });

            if (!response.ok)
                throw new Error("Unable to update recipient.");

            showToast("✓ Recipient updated successfully.");

        }

        await loadRecipients();

        clearForm();

    }
    catch (error) {

        console.error(error);

        showToast(error.message, "error");

    }

}

function resetForm() {
    editingId = null;
    document.getElementById("recipientForm").reset();
    document.getElementById("formTitle").textContent = "Add Recipient";
}

async function editRecipient(id) {

    const response = await fetch(`${RECIPIENT_API}/${id}`);

    const r = await response.json();

    document.getElementById("recipientId").value = r.id;

    document.getElementById("existingRecordingUrl").value = r.nameRecordingUrl ?? "";

    document.getElementById("code").value = r.code;

    document.getElementById("name").value = r.name;

    document.getElementById("startDate").value = r.startDate.substring(0, 10);

    document.getElementById("endDate").value = r.endDate?.substring(0, 10) ?? "";

    document.getElementById("formTitle").textContent = "Edit Recipient";

    document.getElementById("saveButton").textContent = "Update Recipient";

    cancelButton.style.display = "inline-block";

}

async function deleteRecipient(id) {
    if (!confirm("Delete recipient?"))
        return;

    await fetch(`${RECIPIENT_API}/${id}`, {
        method: "DELETE"
    });

    showToast("🗑 Recipient deleted successfully.", "error");
    loadRecipients();
}

function clearForm() {

    form.reset();

    document.getElementById("recipientId").value = "";

    document.getElementById("existingRecordingUrl").value = "";

    document.getElementById("formTitle").textContent = "Add Recipient";

    document.getElementById("saveButton").textContent = "Save Recipient";

    cancelButton.style.display = "none";
}

function formatDate(date) {
    if (date == null)
        return "";

    return date.substring(0, 10);
}

async function uploadRecording() {

    const fileInput = document.getElementById("recording");

    if (!fileInput.files || fileInput.files.length === 0) {

        return "";

    }

    const formData = new FormData();

    formData.append("file", fileInput.files[0]);

    const response = await fetch(RECORDING_API, {

        method: "POST",

        body: formData

    });

    if (!response.ok) {

        throw new Error("Unable to upload recording.");

    }

    const result = await response.json();

    return result.url;

}

function showToast(message, type = "success") {

    const toast = document.getElementById("toast");

    const toastMessage = document.getElementById("toastMessage");

    toast.className = "toast";

    toast.classList.add(type);

    toast.classList.add("show");

    toastMessage.innerHTML = message;

    setTimeout(() => {

        toast.classList.remove("show");

    }, 3000);

}

function validateForm() {

    const code = document.getElementById("code").value.trim();
    const name = document.getElementById("name").value.trim();
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;

    if (code === "") {
        showToast("Code is required.", "error");
        return false;
    }

    if (name === "") {
        showToast("Name is required.", "error");
        return false;
    }

    if (startDate === "") {
        showToast("Start Date is required.", "error");
        return false;
    }

    if (endDate !== "") {

        const start = new Date(startDate);
        const end = new Date(endDate);

        if (end < start) {

            showToast("End Date must be greater than or equal to Start Date.", "error");

            return false;
        }
    }

    return true;
}