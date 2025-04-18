using System.ComponentModel.DataAnnotations;

namespace ExpenseManagementSystem.ViewModels
{
    public class LoginVM
    {
        // Username property with required validation
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, ErrorMessage = "Username must be less than 100 characters.")]
        public string Username { get; set; }

        // Password property with required and length validation
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password, ErrorMessage = "Password have a capital letter, special character, number")] // Indicates this is a password field
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; }

        // Optional: Add validation logic
        public string ErrorMessage { get; set; }
    }
}
