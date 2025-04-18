$(document).ready(function () {
    let expenseCount = expenseModelCount; //Initialize with the current number of expenses

    $('#addExpenseBtn').click(function () {
        $('#expensesContainer').append(`
                <div class="d-flex flex-wrap p-2 me-2 border expense-row" data-index="${expenseCount}">
                    <input type="hidden" name="Expenses[${expenseCount}].Id" value="0" />
                    <div style="font-weight: bold; margin-right: 15px;" class="mb-2">
                        <label for="Expenses_${expenseCount}__Description">Description:</label>
                        <input type="text" id="Expenses_${expenseCount}__Description"
                               name="Expenses[${expenseCount}].Description"
                               class="form-control" required />
                        <span class="text-danger" style="display:none;"></span>
                    </div>
                    <div style="font-weight: bold; margin-right: 15px;" class="mb-2">
                        <label for="Expenses_${expenseCount}__Amount">Amount:</label>
                        <input type="number" id="Expenses_${expenseCount}__Amount"
                               name="Expenses[${expenseCount}].Amount"
                               class="form-control" required oninput="updateTotalAmount()" />
                        <span class="text-danger" style="display:none;"></span>
                    </div>
                    <div style="font-weight: bold; margin-right: 15px;" class="mb-2">
                        <label for="Expenses_${expenseCount}__Date">Date:</label>
                        <input type="date" id="Expenses_${expenseCount}__Date"
                               name="Expenses[${expenseCount}].Date"
                               class="form-control" required />
                        <span class="text-danger" style="display:none;"></span>
                    </div>
                    <div class="mt-4 remove-expense-container">
                        <button type="button" class="btn btn-danger remove-expense-btn">Remove</button>
                    </div>
                </div>
            `);
        expenseCount++; //Increment the count after adding a new expense
        validateExpensesCount();  //Check if there is at least one expense after adding
        updateTotalAmount(); // Ensure the total amount is updated
    });

    //Event delegation for remove buttons to handle dynamically added elements
    $('#expensesContainer').on('click', '.remove-expense-btn', function () {
        var row = $(this).closest('.expense-row');
        var expenseId = $(this).data('id');
        removeExpenseRow(row[0], expenseId); //Pass the raw DOM element to the function
    });
});

let idsToDelete = [];

function updateTotalAmount() {
    var amountInputs = document.querySelectorAll('#expensesContainer input[name$=".Amount"]');
    var total = 0;

    amountInputs.forEach(function (input) {
        var value = parseFloat(input.value) || 0;
        total += value;
    });

    document.getElementById("totalAmount").textContent = total.toFixed(2);
    document.getElementById("totalAmountInput").value = total.toFixed(2);

    if (total > 5000) {
        document.getElementById("amountLimitMessage").style.display = "block";
        document.getElementById("updateExpenseBtn").disabled = true;
    } else {
        document.getElementById("amountLimitMessage").style.display = "none";
        document.getElementById("updateExpenseBtn").disabled = false;
    }
}

function checkExpensesCount() {
    const expenseRows = document.querySelectorAll('.expense-row');
    const oneExpenseMessage = document.getElementById('oneExpenseValidationMessage');

    if (expenseRows.length === 1) {
        expenseRows[0].querySelector('.remove-expense-container').style.display = 'none';
        oneExpenseMessage.style.display = 'block';
    } else {
        document.querySelectorAll('.remove-expense-container').forEach(container => {
            container.style.display = 'block';
        });
        oneExpenseMessage.style.display = 'none';
    }
}

function removeExpenseRow(rowElement, expenseId) {
    if (!idsToDelete.includes(expenseId)) {
        idsToDelete.push(expenseId);
    }

    document.getElementById('IdsToDelete').value = idsToDelete.join(',');

    rowElement.remove();
    updateTotalAmount();
    checkExpensesCount();
}

document.addEventListener("DOMContentLoaded", function () {
    updateTotalAmount();
    checkExpensesCount();
});