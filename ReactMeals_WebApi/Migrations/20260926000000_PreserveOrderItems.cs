using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReactMeals_WebApi.Contexts;

#nullable disable

namespace ReactMeals_WebApi.Migrations;

[DbContext(typeof(MainDbContext))]
[Migration("20260926000000_PreserveOrderItems")]
public class PreserveOrderItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Dish_name", table: "OrderItems", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Dish_description", table: "OrderItems", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "Price", table: "OrderItems", type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);

        migrationBuilder.Sql("""
            UPDATE oi SET oi.Dish_name = d.Dish_name,
                          oi.Dish_description = d.Dish_description,
                          oi.Price = d.Price
            FROM OrderItems AS oi
            INNER JOIN Dishes AS d ON oi.DishId = d.DishId;
            """);

        migrationBuilder.DropForeignKey(name: "FK_OrderItems_Dishes_DishId", table: "OrderItems");
        migrationBuilder.AddForeignKey(
            name: "FK_OrderItems_Dishes_DishId",
            table: "OrderItems",
            column: "DishId",
            principalTable: "Dishes",
            principalColumn: "DishId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql("DELETE FROM Tokens WHERE TokenType = 'M_API';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_OrderItems_Dishes_DishId", table: "OrderItems");
        migrationBuilder.DropColumn(name: "Dish_name", table: "OrderItems");
        migrationBuilder.DropColumn(name: "Dish_description", table: "OrderItems");
        migrationBuilder.DropColumn(name: "Price", table: "OrderItems");
        migrationBuilder.AddForeignKey(
            name: "FK_OrderItems_Dishes_DishId",
            table: "OrderItems",
            column: "DishId",
            principalTable: "Dishes",
            principalColumn: "DishId",
            onDelete: ReferentialAction.Cascade);
    }
}
