namespace ExpenseManagementSystem.ViewModels
{
    public class TransactionVM
    {
        public int TransactionId { get; set; }
        public int FormId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string ActionType { get; set; }
        public string ActionDetail { get; set; }
        public DateTime ActionDate { get; set; }
        public string Status { get; set; }
    }
}
