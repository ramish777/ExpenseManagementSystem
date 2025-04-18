using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnableIdentityInsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable IDENTITY_INSERT for ExpenseForms
            migrationBuilder.Sql("SET IDENTITY_INSERT ExpenseForms ON");
            // You can add specific insert logic here if necessary
            migrationBuilder.Sql("SET IDENTITY_INSERT ExpenseForms OFF");

            // Enable IDENTITY_INSERT for ExpenseItems
            migrationBuilder.Sql("SET IDENTITY_INSERT ExpenseItems ON");
            // You can add specific insert logic here if necessary
            migrationBuilder.Sql("SET IDENTITY_INSERT ExpenseItems OFF");

            // Enable IDENTITY_INSERT for Transactions
            migrationBuilder.Sql("SET IDENTITY_INSERT Transactions ON");
            // You can add specific insert logic here if necessary
            migrationBuilder.Sql("SET IDENTITY_INSERT Transactions OFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
