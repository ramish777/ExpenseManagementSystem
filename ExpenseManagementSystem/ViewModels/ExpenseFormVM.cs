namespace ExpenseManagementSystem.ViewModels
{
    public class ExpenseFormVM
    {
        public int Id { get; set; }
        public string SelectedCurrency { get; set; }
        public List<ExpenseItemVM> Expenses { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Remarks  { get; set; }
        public string? Status { get; set; }

        // New property to track IDs of expenses to delete
        public List<int>? IdsToDelete { get; set; }

        public ExpenseFormVM()
        {
            SelectedCurrency = "N/A";
            Expenses = new List<ExpenseItemVM>();
            IdsToDelete = new List<int>(); 
        }
    }
}
