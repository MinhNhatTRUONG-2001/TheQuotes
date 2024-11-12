using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteApi.Migrations
{
    public partial class QuotesDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TheQuote = table.Column<string>(type: "TEXT", nullable: false),
                    WhoSaid = table.Column<string>(type: "TEXT", nullable: false),
                    WhenWasSaid = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuoteCreator = table.Column<string>(type: "TEXT", nullable: false),
                    QuoteCreateDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
