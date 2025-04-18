using ExpenseManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-many relationship between ApplicationUser and ExpenseForm
            modelBuilder.Entity<ExpenseForm>()
                .HasOne(e => e.Employee)
                .WithMany(u => u.ExpenseForms)
                .HasForeignKey(e => e.EmployeeId);

            // Configure one-to-many relationship between ExpenseForm and ExpenseItem
            modelBuilder.Entity<ExpenseItem>()
                .HasOne(ei => ei.ExpenseForm)
                .WithMany(e => e.ExpenseItems)
                .HasForeignKey(ei => ei.FormId);

            // Configure one-to-many relationship between ExpenseForm and Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.ExpenseForm)
                .WithMany(e => e.Transactions)
                .HasForeignKey(t => t.FormId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure one-to-many relationship between ApplicationUser and Transaction
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<ExpenseForm> ExpenseForms { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
    }
}
