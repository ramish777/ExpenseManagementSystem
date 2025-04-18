using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using ExpenseManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ExpenseManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //ALL MY PRIVATE FUNCTIONS
        private ExpenseForm GetExpenseFormById(int id)
        {
            return _context.ExpenseForms.Find(id);
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
        private ApplicationUser GetUserAsync()
        {
            return _userManager.GetUserAsync(User).Result; // Ensure async context
        }
        private List<string> GetManagedEmployees(string managerUserName)
        {
            return _context.Users
                .Where(u => u.ManagerUsername == managerUserName)
                .Select(u => u.Id)
                .ToList();
        }
        private List<ExpenseForm> GetFilteredExpenseForms(string status, DateTime? date)
        {
            var expenseForms = _context.ExpenseForms.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                expenseForms = expenseForms.Where(e => e.Status == status);
            }

            if (date.HasValue)
            {
                expenseForms = expenseForms.Where(e => e.Date.Date == date.Value.Date);
            }

            return expenseForms.ToList();
        }
        private List<Transaction> GetTransactions(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return _context.Transactions
                    .Where(t => t.ActionDate.Date >= startDate.Value.Date && t.ActionDate.Date <= endDate.Value.Date)
                    .ToList();
            }
            else if (startDate.HasValue)
            {
                return _context.Transactions
                    .Where(t => t.ActionDate.Date >= startDate.Value.Date)
                    .ToList();
            }
            else if (endDate.HasValue)
            {
                return _context.Transactions
                    .Where(t => t.ActionDate.Date <= endDate.Value.Date)
                    .ToList();
            }

            return new List<Transaction>(); // Return an empty list if no dates are provided
        }
        private List<TransactionVM> CreateTransactionViewModels(List<Transaction> transactions)
        {
            var transactionVMs = new List<TransactionVM>();

            foreach (var transaction in transactions)
            {
                var user1 = _context.Users.FirstOrDefault(u => u.Id == transaction.UserId);

                var transactionVM = new TransactionVM
                {
                    TransactionId = transaction.Id,
                    FormId = transaction.FormId,
                    UserId = transaction.UserId,
                    UserName = user1 != null ? user1.Name : "Unknown",
                    Role = transaction.Role,
                    ActionType = transaction.ActionType,
                    ActionDetail = transaction.ActionDetail,
                    ActionDate = transaction.ActionDate,
                    Status = transaction.Status
                };

                transactionVMs.Add(transactionVM);
            }

            return transactionVMs;
        }


        //ALL MY ACTION RESULT
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                Log.Information("Fetching transactions for the period from {StartDate} to {EndDate}", startDate, endDate);

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    Log.Warning("User not logged in while attempting to view transactions");
                    return RedirectToAction("Login", "Account");
                }

                string userId = user.Id;

                var transactions = GetTransactions(startDate, endDate);

                var transactionVMs = CreateTransactionViewModels(transactions);

                ViewData["CurrentStartDate"] = startDate?.ToString("yyyy-MM-dd");
                ViewData["CurrentEndDate"] = endDate?.ToString("yyyy-MM-dd");

                Log.Information("Transactions successfully retrieved");
                return View(transactionVMs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching transactions");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public IActionResult View(int id)
        {
            try
            {
                Log.Information("Fetching expense and expense items for Form ID: {Id}", id);

                var expense = GetExpenseFormById(id);
                if (expense == null)
                {
                    Log.Warning("Expense form with ID {Id} not found", id);
                    return NotFound();
                }

                var expenseList = GetExpenseItemsByFormId(id);
                var model = CreateExpenseFormViewModel(expense, expenseList);

                Log.Information("Expense form with ID {Id} successfully retrieved", id);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching expense form with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public IActionResult ViewExpenseForm(string status, DateTime? date)
        {
            try
            {
                Log.Information("Fetching expense forms for status: {Status} and date: {Date}", status, date);

                var user = GetUserAsync();
                if (user == null)
                {
                    Log.Warning("User not logged in while attempting to view expense forms");
                    return RedirectToAction("Login", "Account");
                }

                var employeesManaged = GetManagedEmployees(user.UserName);
                var expenseForms = GetFilteredExpenseForms(status, date);

                ViewData["CurrentStatus"] = status;
                ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");

                Log.Information("Expense forms successfully fetched");
                return View(expenseForms);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching expense forms");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
