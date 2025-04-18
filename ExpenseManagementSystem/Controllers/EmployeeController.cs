using ExpenseManagementSystem.Data;
using ExpenseManagementSystem.Models;
using ExpenseManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ExpenseManagementSystem.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //PRIVATE FUNCTIONS
        private List<ExpenseForm> GetExpenseForms(string userId, string status)
        {
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                return _context.ExpenseForms
                    .Where(e => e.Status == status && e.EmployeeId == userId)
                    .ToList();
            }

            return _context.ExpenseForms
                .Where(e => e.EmployeeId == userId)
                .ToList();
        }
        private List<ExpenseForm> FilterExpenseFormsByDate(List<ExpenseForm> expenseForms, DateTime date)
        {
            return expenseForms
                .Where(e => e.Date.Date == date.Date)
                .ToList();
        }
        private ExpenseForm CreateExpenseForm(ExpenseFormVM model, ApplicationUser user)
        {
            var expenseFormId = _context.ExpenseForms.Count(); // Assuming this generates a unique ID
            return new ExpenseForm
            {
                ExpenseFormId = expenseFormId,
                EmployeeId = user.Id,
                Date = DateTime.Now, // or use a date from your model if applicable
                Amount = model.TotalAmount,
                Status = "Pending", // or any other status you want to set
                Remarks = null, // or set remarks if applicable
                Currency = model.SelectedCurrency // Assuming you have this in your VM
            };
        }
        private async Task AddExpenseItems(List<ExpenseItemVM> expenses, int expenseFormId)
        {
            foreach (var expense in expenses)
            {
                var expenseId = _context.ExpenseItems.Count(); // Assuming this generates a unique ID
                var expenseItem = new ExpenseItem
                {
                    ExpenseId = expenseId,
                    FormId = expenseFormId, // assuming you have a foreign key to ExpenseForm
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Date = expense.Date
                };

                _context.ExpenseItems.Add(expenseItem);
                // Save changes for the expense items
                await _context.SaveChangesAsync();
            }
        }
        private Transaction CreateTransaction(int expenseFormId, ApplicationUser user)
        {
            return new Transaction
            {
                FormId = expenseFormId, // Foreign key for ExpenseForm
                UserId = user.Id, // Foreign key for ApplicationUser
                Role = "Employee", // Set the role as needed
                ActionType = "Create", // Type of action
                ActionDetail = $"Created", // Detailed description
                ActionDate = DateTime.Now, // Current date
                Status = "Completed" // Set the status as needed
            };
        }
        private Transaction UpdateTransaction(int expenseFormId)
        {
            var expenseForm = _context.ExpenseForms
                                 .FirstOrDefault(e => e.Id == expenseFormId);
            return new Transaction
            {
                FormId = expenseFormId, // Foreign key for ExpenseForm
                UserId = expenseForm.EmployeeId, // Foreign key for ApplicationUser
                Role = "Employee", // Set the role as needed
                ActionType = "Update", // Type of action
                ActionDetail = $"Updated", // Detailed description
                ActionDate = DateTime.Now, // Current date
                Status = "Completed" // Set the status as needed
            };
        }
        private void HandleDeletion(List<int> idsToDelete, int expenseFormId)
        {
            foreach (var num in idsToDelete)
            {
                var expenseToRemove = _context.ExpenseItems.Find(num);
                if (expenseToRemove != null)
                {
                    _context.ExpenseItems.Remove(expenseToRemove);
                }
            }
            _context.SaveChanges();
            Log.Information("Deleted expenses for form id: {Id}", expenseFormId);
        }
        private ExpenseForm UpdateExpenseForm(int id, ExpenseFormVM model)
        {
            //var user = await _userManager.GetUserAsync(User);
            var expense = _context.ExpenseForms.Find(id);
            if (expense == null)
            {
                Log.Warning("Expense form not found for id: {Id}", id);
                return null;
            }

            // Update the expense form details
            expense.Currency = model.SelectedCurrency;
            expense.Amount = model.TotalAmount;
            expense.Remarks = null;
            expense.Status = "Pending";
            var transaction = UpdateTransaction(expense.Id);
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            Log.Information("Updated expense form for id: {Id}", id);
            return expense;
        }
        private void UpdateExpenseItems(List<ExpenseItemVM> expenses, int expenseFormId)
        {
            foreach (var expenseItem in expenses)
            {
                var existingItem = _context.ExpenseItems.FirstOrDefault(e => e.Id == expenseItem.Id);
                if (existingItem != null)
                {
                    existingItem.Description = expenseItem.Description;
                    existingItem.Amount = expenseItem.Amount;
                    existingItem.Date = expenseItem.Date;
                }
                else
                {
                    var newExpenseItem = new ExpenseItem
                    {
                        ExpenseId = _context.ExpenseItems.Count(), // Generate a new ID
                        FormId = expenseFormId,
                        Description = expenseItem.Description,
                        Amount = expenseItem.Amount,
                        Date = expenseItem.Date
                    };

                    _context.ExpenseItems.Add(newExpenseItem);
                }
            }
            _context.SaveChanges();
        }


        //ACTION RESULTS
        [HttpGet]
        public async Task<IActionResult> Index(string status, DateTime? date)
        {
            try
            {
                // Retrieve the signed-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    Log.Warning("User not found, redirecting to login.");
                    return RedirectToAction("Login", "Account");
                }

                string userId = user.Id;

                var expenseForms = GetExpenseForms(userId, status);

                // Apply date filter if provided
                if (date.HasValue)
                {
                    expenseForms = FilterExpenseFormsByDate(expenseForms, date.Value);
                }

                // Store filter values in ViewBag for maintaining state in the UI
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd"); // Format the date for input field

                Log.Information("Fetched {Count} expense forms for user {UserId}", expenseForms.Count, userId);
                return View(expenseForms);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching expense forms for user {UserId}", User.Identity.Name);
                return View("Error"); // Show an error view or return an error response
            }
        }
        [HttpGet]
        public IActionResult Create()
        {
            var model = new ExpenseFormVM();
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(ExpenseFormVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            try
            {
                // Calculate total amount
                model.TotalAmount = model.Expenses.Sum(e => e.Amount);

                // Retrieve the signed-in user
                if (user == null)
                {
                    Log.Warning("User not found during Create operation.");
                    return RedirectToAction("Login", "Account");
                }

                if (ModelState.IsValid)
                {
                    // Create a new ExpenseForm entity
                    var expenseForm = CreateExpenseForm(model, user);

                    // Add the ExpenseForm to the context and save changes
                    _context.ExpenseForms.Add(expenseForm);
                    await _context.SaveChangesAsync();
                    Log.Information("ExpenseForm created with ID: {ExpenseFormId} by User: {UserId}", expenseForm.ExpenseFormId, user.Id);

                    // Now add each expense item to the database
                    await AddExpenseItems(model.Expenses, expenseForm.Id);

                    // Now create a transaction record
                    var transaction = CreateTransaction(expenseForm.Id, user);
                    _context.Transactions.Add(transaction);
                    await _context.SaveChangesAsync(); // Save the transaction
                    Log.Information("Transaction created for ExpenseForm ID: {ExpenseFormId} by User: {UserId}", expenseForm.Id, user.Id);

                    // Notifications 
                    TempData["Success"] = "Expense Form created Successfully";
                    return RedirectToAction("Index");
                }

                Log.Warning("ModelState is not valid for ExpenseForm creation. Errors: {ModelStateErrors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while creating ExpenseForm. User ID: {UserId}", user?.Id);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the expense form. Please try again.");
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var expense = _context.ExpenseForms.Find(id);
                // Fetch the expense from the database
                var expenseList = _context.ExpenseItems.Where(e => e.FormId == id).ToList();

                var model = new ExpenseFormVM
                {
                    SelectedCurrency = expense.Currency,
                    Expenses = expenseList.Select(ei => new ExpenseItemVM
                    {
                        Id = ei.Id,
                        Description = ei.Description,
                        Amount = ei.Amount,
                        Date = ei.Date
                    }).ToList(),
                    TotalAmount = expense.Amount
                };

                Log.Information("Fetched expense form for id: {Id}", id);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Edit GET action for id: {Id}", id);
                return RedirectToAction("Error");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ExpenseFormVM model)
        {

            try
            {
                // Notifications 
                TempData["Success"] = "Expense Form updated Successfully";

                if (model.Expenses == null || model.Expenses.Count == 0)
                {
                    HandleDeletion(model.IdsToDelete, id);
                    var expense = UpdateExpenseForm(id, model);
                    return RedirectToAction("Index");
                }
                else if (model.IdsToDelete.Count == 0)
                {
                    var expense = UpdateExpenseForm(id, model);
                    if (expense == null) return NotFound();
                    UpdateExpenseItems(model.Expenses, expense.Id);
                    return RedirectToAction("Index");
                }
                else
                {
                    HandleDeletion(model.IdsToDelete, id);
                    var expense = UpdateExpenseForm(id, model);
                    expense.Currency = model.SelectedCurrency;
                    expense.Amount = model.TotalAmount;
                    if (expense == null) return NotFound();
                    UpdateExpenseItems(model.Expenses, expense.Id);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Edit POST action for id: {Id}", id);
                return RedirectToAction("Error");
            }
        }
        [HttpGet]
        public IActionResult View(int id)
        {
            try
            {
                var expense = _context.ExpenseForms.Find(id);
                // Fetch the expense from the database
                var expenseList = _context.ExpenseItems.Where(e => e.FormId == id).ToList();

                var model = new ExpenseFormVM
                {
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

                Log.Information("Fetched view for expense form id: {Id}", id);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in View GET action for id: {Id}", id);
                return RedirectToAction("Error");
            }
        }
    }
}
