const API = "https://localhost:7218/api/Recipients";

const table = document.getElementById("recipientTable");

const form = document.getElementById("recipientForm");

const cancelButton = document.getElementById("cancelEdit");

let editingId = null;

loadRecipients();

cancelButton.style.display = "none";

form.addEventListener("submit", saveRecipient);

cancelButton.addEventListener("click", clearForm);

async function loadRecipients() {

    const response = await fetch(API);

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
                <td>${r.nameRecordingUrl ?? ""}</td>
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

    const id = document.getElementById("recipientId").value;

    const body = {

        code: Number(document.getElementById("code").value),

        name: document.getElementById("name").value,

        nameRecordingUrl: "",

        startDate: document.getElementById("startDate").value,

        endDate: document.getElementById("endDate").value || null

    };

    if (id === "") {
        await fetch(API, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(body)
        });

        showToast("✓ Recipient saved successfully.");
    }
    else {
        await fetch(`${API}/${id}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(body)
        });

        showToast("✎ Recipient updated successfully.", "warning");
    }

    await loadRecipients();
    clearForm();
}

function resetForm() {
    editingId = null;
    document.getElementById("recipientForm").reset();
    document.getElementById("formTitle").textContent = "Add Recipient";
}

async function editRecipient(id) {
    const response = await fetch(`${API}/${id}`);
    const r = await response.json();
    document.getElementById("recipientId").value = r.id;
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

    await fetch(`${API}/${id}`, {
        method: "DELETE"
    });

    showToast("🗑 Recipient deleted successfully.", "error");
    loadRecipients();
}

function clearForm() {

    form.reset();
    document.getElementById("recipientId").value = "";
    document.getElementById("formTitle").textContent = "Add Recipient";
    document.getElementById("saveButton").textContent = "Save Recipient";
    cancelButton.style.display = "none";
}

function formatDate(date) {
    if (date == null)
        return "";

    return date.substring(0, 10);
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