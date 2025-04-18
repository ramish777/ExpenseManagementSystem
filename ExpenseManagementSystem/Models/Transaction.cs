using System.ComponentModel.DataAnnotations;

namespace ExpenseManagementSystem.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public int FormId { get; set; }// Foreign key for ExpenseForm
        public string UserId { get; set; }// Foreign key for ApplicationUser
        public string Role { get; set; }
        public string ActionType { get; set; }
        public string ActionDetail { get; set; }
        public DateTime ActionDate { get; set; }
        public string Status { get; set; }

        // Navigation Properties
        public ExpenseForm ExpenseForm { get; set; }
        public ApplicationUser User { get; set; }
    }
}
