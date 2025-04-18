using System.ComponentModel.DataAnnotations;

namespace ExpenseManagementSystem.Models
{
    public class ExpenseItem
    {
        [Key]
        public int Id { get; set; }

        public int ExpenseId { get; set; }

        public int FormId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        // Navigation Property
        public ExpenseForm ExpenseForm { get; set; }
    }
}
