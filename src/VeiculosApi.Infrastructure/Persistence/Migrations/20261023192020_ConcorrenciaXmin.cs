using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeiculosApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConcorrenciaXmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin é coluna de sistema do PostgreSQL, presente em toda tabela: não criar coluna.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nada a desfazer: a migration não criou coluna, então não há o que remover.
        }
    }
}
