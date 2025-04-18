$(document).ready(function () {
    var expenseCount = $("#expensesContainer .expense-row").length;
    var currencySelector = document.getElementById('currencySelector');
    var createExpenseBtn = document.getElementById('createExpenseBtn');
    var selectCurrencyMessage = document.getElementById('selectCurrencyMessage');
    var amountLimitMessage = document.getElementById('amountLimitMessage');
    var oneExpenseValidationMessage = document.getElementById('oneExpenseValidationMessage');

    currencySelector.addEventListener('change', function () {
        updateSubmitButtonState();
    });

    $('#addExpenseBtn').click(function () {
        $('#expensesContainer').append(`
                <div class="d-flex flex-wrap p-2 me-2 border expense-row">
                    <div class="mb-2 me-2">
                        <label for="Expenses_${expenseCount}__Description">Description:</label>
                        <input type="text" id="Expenses_${expenseCount}__Description"
                               name="Expenses[${expenseCount}].Description"
                               class="form-control input-margin" required />
                        <span class="text-danger" asp-validation-for="Expenses[${expenseCount}].Description" style="display:none;"></span>
                    </div>
                    <div class="mb-2 me-2">
                        <label for="Expenses_${expenseCount}__Amount">Amount:</label>
                        <input type="number" id="Expenses_${expenseCount}__Amount"
                               name="Expenses[${expenseCount}].Amount"
                               class="form-control amount-input input-margin" required />
                        <span class="text-danger" asp-validation-for="Expenses[${expenseCount}].Amount" style="display:none;"></span>
                    </div>
                    <div class="mb-2 me-2">
                        <label for="Expenses_${expenseCount}__Date">Date:</label>
                        <input type="date" id="Expenses_${expenseCount}__Date"
                               name="Expenses[${expenseCount}].Date"
                               class="form-control date-input input-margin" required />
                        <span class="text-danger" asp-validation-for="Expenses[${expenseCount}].Date" style="display:none;"></span>
                    </div>
                    <button type="button" class="btn btn-danger remove-expense-btn">Remove</button>
                </div>
        `);
        validateExpensesCount();  // Check if there is at least one expense after adding
        expenseCount++;
        updateTotalAmount();
    });

    function updateSubmitButtonState() {
        var total = parseFloat($('#totalAmount').text());
        var selectedCurrency = currencySelector.value;
        var hasExpenses = $('#expensesContainer .expense-row').length > 0;

        if (!hasExpenses) {
            createExpenseBtn.disabled = true;
            oneExpenseValidationMessage.style.display = 'block';
        } else if (selectedCurrency === "N/A") {
            createExpenseBtn.disabled = true;
            selectCurrencyMessage.style.display = 'block';
        } else if (total > 5000) {
            createExpenseBtn.disabled = true;
            amountLimitMessage.style.display = 'block';
            selectCurrencyMessage.style.display = 'none';
        } else {
            createExpenseBtn.disabled = false;
            selectCurrencyMessage.style.display = 'none';
            amountLimitMessage.style.display = 'none';
        }
    }

    function updateTotalAmount() {
        var total = 0;
        $('#expensesContainer .expense-row').each(function () {
            var amount = $(this).find('.amount-input').val();
            if ($.isNumeric(amount)) {
                total += parseFloat(amount);
            }
        });
        $('#totalAmount').text(total.toFixed(2));
        updateSubmitButtonState();
    }

    function validateExpensesCount() {
        var hasExpenses = $('#expensesContainer .expense-row').length > 0;
        if (!hasExpenses) {
            oneExpenseValidationMessage.style.display = 'block';
            createExpenseBtn.disabled = true;
        } else {
            oneExpenseValidationMessage.style.display = 'none';
            createExpenseBtn.disabled = false;
        }
    }

    $('#expensesContainer').on('input', '.amount-input', function () {
        updateTotalAmount();
    });

    $('#expensesContainer').on('click', '.remove-expense-btn', function () {
        $(this).closest('.expense-row').remove();
        updateTotalAmount();
        validateExpensesCount();  // Check if there are still any expenses after removing
    });

    validateExpensesCount();  // Initial check on page load
});
