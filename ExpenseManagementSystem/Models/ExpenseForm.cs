using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace ExpenseManagementSystem.Models
{
    public class ExpenseForm
    {
        [Key]
        public int Id { get; set; }
        public int ExpenseFormId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public string Currency { get; set; }

        // Foreign Key to ApplicationUser
        public string EmployeeId { get; set; }
        public virtual ApplicationUser Employee { get; set; }

        // Navigation properties
        public virtual ICollection<ExpenseItem> ExpenseItems { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
