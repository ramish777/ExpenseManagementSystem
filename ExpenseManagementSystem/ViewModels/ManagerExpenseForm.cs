using ExpenseManagementSystem.Models;

namespace ExpenseManagementSystem.ViewModels
{
    public class ManagerExpenseForm
    {
        public string employeeName { get; set; }
        public int Id { get; set; }
        public int ExpenseFormId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public string Currency { get; set; }

        // Foreign Key to ApplicationUser
        public string EmployeeId { get; set; }
    }
}
