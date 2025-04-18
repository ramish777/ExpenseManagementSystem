using Microsoft.AspNetCore.Identity;

namespace ExpenseManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string? ManagerUsername { get; set; }

        // Navigation property to link multiple expense forms
        public virtual ICollection<ExpenseForm> ExpenseForms { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
