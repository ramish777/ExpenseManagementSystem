﻿namespace ExpenseManagementSystem.ViewModels
{
    public class ExpenseItemVM
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
