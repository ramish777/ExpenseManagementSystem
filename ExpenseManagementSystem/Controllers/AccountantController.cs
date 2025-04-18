using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using ExpenseManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ExpenseManagementSystem.Controllers
{
    [Authorize(Roles = "Accountant")]
    public class AccountantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //ALL MY PRIVATE FUNCTIONS
        private async Task<ApplicationUser> GetUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Log.Warning("User not found during Index action.");
            }
            return user;
        }
        private List<ExpenseForm> GetExpenseForms(string status)
        {
            var expenseForms = _context.ExpenseForms
                .Where(e => e.Status == "Approved" || e.Status == "Paid")
                .ToList();

            if (!string.IsNullOrEmpty(status))
            {
                expenseForms = _context.ExpenseForms
                    .Where(e => e.Status == status)
                    .ToList();
            }

            if (status == "All")
            {
                expenseForms = _context.ExpenseForms
                    .Where(e => e.Status == "Approved" || e.Status == "Paid")
                    .ToList();
            }

            return expenseForms;
        }
        private List<ExpenseForm> FilterExpenseFormsByDate(List<ExpenseForm> expenseForms, DateTime date)
        {
            return expenseForms.Where(e => e.Date.Date == date.Date).ToList();
        }
        private ExpenseForm GetExpenseFormById(int id)
        {
            var expense = _context.ExpenseForms.Find(id);
            if (expense == null)
            {
                Log.Warning("Expense form not found for id: {Id}", id);
            }
            return expense;
        }
        private List<ExpenseItem> GetExpenseItemsByFormId(int id)
        {
            return _context.ExpenseItems.Where(e => e.FormId == id).ToList();
        }
        private ExpenseFormVM CreateExpenseFormViewModel(ExpenseForm expense, List<ExpenseItem> expenseList)
        {
            return new ExpenseFormVM
            {
                Id = expense.Id,
                SelectedCurrency = expense.Currency,
                Remarks = expense.Remarks,
                Status = expense.Status,
                Expenses = expenseList.Select(ei => new ExpenseItemVM
                {
                    Id = ei.Id,
                    Description = ei.Description,
                    Amount = ei.Amount,
                    Date = ei.Date
                }).ToList(),
                TotalAmount = expense.Amount
            };
        }
        private void UpdateExpenseFormStatusToPaid(ExpenseForm expenseForm)
        {
            expenseForm.Status = "Paid";
            _context.SaveChanges();
        }
        private Transaction CreateTransactionRecord(ExpenseForm expenseForm, ApplicationUser user)
        {
            return new Transaction
            {
                FormId = expenseForm.Id, // Foreign key for ExpenseForm
                UserId = user.Id, // Foreign key for ApplicationUser
                Role = "Accountant", // Set the role as needed
                ActionType = "Paid", // Type of action
                ActionDetail = "Paid", // Detailed description
                ActionDate = DateTime.Now, // Current date
                Status = "Completed" // Set the status as needed
            };
        }
        private void AddTransactionToContext(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            _context.SaveChanges(); // Save the transaction
        }


        //ACTION RESULTS
        [HttpGet]
        public async Task<IActionResult> Index(string status, DateTime? date)
        {
            try
            {
                var user = await GetUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                string userId = user.Id;

                var expenseForms = GetExpenseForms(status);

                // Apply date filter if provided
                if (date.HasValue)
                {
                    expenseForms = FilterExpenseFormsByDate(expenseForms, date.Value);
                }

                // Store filter values in ViewData for maintaining state in the UI
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");

                Log.Information("Fetched expense forms for user: {UserId}", userId);
                return View(expenseForms);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching expense forms for user.");
                return RedirectToAction("Error");
            }
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var expense = GetExpenseFormById(id);
                if (expense == null)
                {
                    return NotFound();
                }

                var expenseList = GetExpenseItemsByFormId(id);

                var model = CreateExpenseFormViewModel(expense, expenseList);

                Log.Information("Fetched expense form for editing with id: {Id}", id);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Edit GET action for id: {Id}", id);
                return RedirectToAction("Error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var expenseForm = GetExpenseFormById(id);
                if (expenseForm == null)
                {
                    return NotFound();
                }

                UpdateExpenseFormStatusToPaid(expenseForm);

                var user = await _userManager.GetUserAsync(User);
                var transaction = CreateTransactionRecord(expenseForm, user);

                AddTransactionToContext(transaction);

                TempData["Success"] = "Expense Form Paid Successfully";
                Log.Information("Approved expense form with id: {Id} by user: {UserId}", id, user.Id);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Approve POST action for id: {Id}", id);
                return RedirectToAction("Error");
            }
        }
    }
}
