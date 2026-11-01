using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeiculosApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StatusAtualizadoEmVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StatusAtualizadoEm",
                table: "veiculos",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusAtualizadoEm",
                table: "veiculos");
        }
    }
}
