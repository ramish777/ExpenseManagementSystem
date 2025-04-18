using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using ExpenseManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ExpenseManagementSystem.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //PRIVATE FUNCTIONS
        private async Task<ApplicationUser> GetSignedInUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Log.Warning("User not found during Index action.");
            }
            return user;
        }
        private List<string> GetEmployeesManaged(string managerUserName)
        {
            return _context.Users
                .Where(u => u.ManagerUsername == managerUserName)
                .Select(u => u.Id)
                .ToList();
        }
        private List<ManagerExpenseForm> GetExpenseForms(List<string> employeesManaged, string status, DateTime? date)
        {
            var completeExpenseForm = new List<ManagerExpenseForm>();

            foreach (var employeeId in employeesManaged)
            {
                // Retrieve the employee name for the current employeeId
                var employeeName = _context.Users
                    .Where(u => u.Id == employeeId)
                    .Select(u => u.Name)
                    .FirstOrDefault();

                // Get expense forms based on the status (if provided) or just default to pending
                var expenseFormsForEmployee = _context.ExpenseForms
                    .Where(e => e.EmployeeId == employeeId && (string.IsNullOrEmpty(status) ? e.Status == "Pending" : e.Status == status))
                    .ToList();

                // If a date filter is applied, filter the expense forms by the given date
                if (date.HasValue)
                {
                    Log.Information("Applying date filter: {Date}", date);
                    expenseFormsForEmployee = expenseFormsForEmployee
                        .Where(e => e.Date.Date == date.Value.Date)
                        .ToList();
                }

                // Create ManagerExpenseForm objects and populate them with data
                foreach (var expenseForm in expenseFormsForEmployee)
                {
                    completeExpenseForm.Add(new ManagerExpenseForm
                    {
                        employeeName = employeeName,
                        Id = expenseForm.Id,
                        ExpenseFormId = expenseForm.ExpenseFormId,
                        Amount = expenseForm.Amount,
                        Date = expenseForm.Date,
                        Status = expenseForm.Status,
                        Remarks = expenseForm.Remarks,
                        Currency = expenseForm.Currency,
                        EmployeeId = employeeId // Foreign key reference to the employee
                    });
                }
            }

            return completeExpenseForm;
        }



        //All ACTION RESULTS
        [HttpGet]
        public async Task<IActionResult> Index(string status, DateTime? date)
        {
            try
            {
                Log.Information("Starting Index action with parameters: status={Status}, date={Date}", status, date);

                // Retrieve the signed-in user
                var user = await GetSignedInUser();
                if (user == null)
                {
                    return RedirectToAction("Login", "Account"); // Handle the case where the user is not found
                }

                string userId = user.Id;
                string managerUserName = user.UserName;

                Log.Information("User retrieved successfully: {UserId}, {ManagerUserName}", userId, managerUserName);

                var employeesManaged = GetEmployeesManaged(managerUserName);
                var expenseForms = GetExpenseForms(employeesManaged, status, date);

                ViewData["CurrentStatus"] = status;
                ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");

                Log.Information("Returning Index view with {ExpenseFormsCount} expense forms.", expenseForms.Count);
                return View(expenseForms);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in Index action.");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                Log.Information("Starting Edit action with id={Id}", id);

                var expense = _context.ExpenseForms.Find(id);
                if (expense == null)
                {
                    Log.Warning("Expense form not found with id={Id}", id);
                    return NotFound();
                }

                var expenseList = _context.ExpenseItems.Where(e => e.FormId == id).ToList();

                var model = new ExpenseFormVM
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

                Log.Information("Returning Edit view for expense form id={Id}", id);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred in Edit action.");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                Log.Information("Starting Approve action with id={Id}", id);

                var expenseForm = _context.ExpenseForms.FirstOrDefault(e => e.Id == id);
                if (expenseForm == null)
                {
                    Log.Warning("Expense form not found for approval with id={Id}", id);
                    return NotFound();
                }

                expenseForm.Status = "Approved";
                _context.SaveChanges();

                var user = await _userManager.GetUserAsync(User);

                var transaction = new Transaction
                {
                    FormId = expenseForm.Id,
                    UserId = user.Id,
                    Role = "Manager",
                    ActionType = "Approved",
                    ActionDetail = $"Approved",
                    ActionDate = DateTime.Now,
                    Status = "Completed"
                };

                _context.Transactions.Add(transaction);
                _context.SaveChanges();

                TempData["Success"] = "Expense Form Approved Successfully";
                Log.Information("Expense form approved successfully with id={Id}", id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during the approval of expense form id={Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string remarks)
        {
            try
            {
                Log.Information("Starting Reject action with id={Id}, remarks={Remarks}", id, remarks);

                var expenseForm = _context.ExpenseForms.FirstOrDefault(e => e.Id == id);
                if (expenseForm == null)
                {
                    Log.Warning("Expense form not found for rejection with id={Id}", id);
                    return NotFound();
                }

                expenseForm.Status = "Rejected";
                expenseForm.Remarks = remarks;
                _context.SaveChanges();

                var user = await _userManager.GetUserAsync(User);

                var transaction = new Transaction
                {
                    FormId = expenseForm.Id,
                    UserId = user.Id,
                    Role = "Manager",
                    ActionType = "Rejected",
                    ActionDetail = $"Rejected",
                    ActionDate = DateTime.Now,
                    Status = "Completed"
                };

                _context.Transactions.Add(transaction);
                _context.SaveChanges();

                TempData["Reject"] = "Expense Form has been Rejected";
                Log.Information("Expense form rejected with id={Id}, remarks={Remarks}", id, remarks);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during the rejection of expense form id={Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
