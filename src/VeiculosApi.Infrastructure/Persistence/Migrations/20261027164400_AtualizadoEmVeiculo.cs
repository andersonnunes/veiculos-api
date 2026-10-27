using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeiculosApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AtualizadoEmVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AtualizadoEm",
                table: "veiculos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql("UPDATE veiculos SET \"AtualizadoEm\" = \"CriadoEm\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "veiculos");
        }
    }
}
