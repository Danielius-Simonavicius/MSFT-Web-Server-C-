
document.addEventListener('DOMContentLoaded', function () {
    fetch('http://localhost:8080/getAll')
        .then(response => response.json())
        .then(data => loadHTMLTable(data['data']));
});

document.querySelector('table tbody').addEventListener
('click', function(event){
    if(event.target.className === "delete-row-btn") {
        deleteRowById(event.target.dataset.id);
    }

    if(event.target.className ==="edit-row-btn"){
        handleEditRow(event.target.dataset.id);
    }
});

const updateBtn = document.querySelector('#update-row-btn');

function deleteRowById(id) {
    fetch('http://localhost:8080/delete/' + id, {
        method: 'DELETE'
    })
    .then(response => response.json)
    .then( data => {
        if(data.success){
            location.reload();
        }
    });
}

function handleEditRow(id){
    const updateSection = document.querySelector('#update-row');
    updateSection.hidden = false;
    document.querySelector('#update-row-btn').dataset.id = id;
}

updateBtn.onclick = function(){
    const updateNameInput = document.querySelector('#update-name-input');
    const updateNameId = document.querySelector('#update-row-btn').dataset.id;
    
    console.log(updateNameInput.dataset.id);
    fetch(`http://localhost:8080/update`, {
        method : 'PATCH',
        headers : {
            'Content-type' : 'application/json'
        },
        body: JSON.stringify({
            id: updateNameId,
            name: updateNameInput.value
        })
    })
    .then(response => response.json())
    .then(data => {
        if(data.success){
            location.reload();
        }
    })
}

const addBtn = document.querySelector('#add-name-btn');
const deletebtn = document.querySelector('#delete-row-btn');

addBtn.onclick = function () {
    const nameInput = document.querySelector('#name-input');
    const name = nameInput.value;
    nameInput.value = "";

    fetch('http://localhost:8080/insert', {
        headers: {
            'Content-type': 'application/json'
        },
        method: 'POST',
        body: JSON.stringify({ name: name })
    })
        .then(response => response.json())
        .then(data => insertRowIntoTable(data['data']));
}


function insertRowIntoTable(data) {
    const table = document.querySelector('table tbody');
    const isTableData = table.querySelector('.no-data');

    let tableHtml = "<tr>";

    for (var key in data) {
        if (keydata.hasOwnProperty(key)) {
            if (key === 'dateAdded') {
                data[key] = new Date(date[key])
                .toLocaleString();
            }
            tableHtml += `<td>${data[key]}</td>`;
        }
    }
    tableHtml += `<td><button class = "delete-row-btn"
    data-id = ${data.id}>Delete</td>`
    tableHtml += `<td><button class = "edit-row-btn"
    data-id = ${data.id}>Edit</td>`
    tableHtml += "</tr>";

    if (isTableData) {
        table.innerHTML = tableHtml;
    }
    else {
        const newRow = table.insertRow();
        newRow.innerHTML = tableHtml;
    }
}

function loadHTMLTable(data) {
    const table = document.querySelector('table tbody');

    console.log(data);

    if (data.length === 0) {
        table.innerHTML = "<tr><td class='no-data' colspan='6'>No Data</td></tr>";
        return;
    }

    let tableHtml = "";
    data.forEach(function ({ id, name, date_added, phone_number, email, address }) {
        tableHtml += "<tr>";
        tableHtml += `<td>${id}</td>`
        tableHtml += `<td>${name}</td>`
        tableHtml += `<td>${new Date(date_added).toLocaleDateString()}</td>`
        tableHtml += `<td>${phone_number}</td>`
        tableHtml += `<td>${email}</td>`
        tableHtml += `<td>${address}</td>`
        tableHtml += `<td><button class = "delete-row-btn"
        data-id = ${id}>Delete</td>`
        tableHtml += `<td><button class = "edit-row-btn"
        data-id = ${id}>Edit</td>`
        tableHtml += "</tr>";
    });

    table.innerHTML = tableHtml;
}