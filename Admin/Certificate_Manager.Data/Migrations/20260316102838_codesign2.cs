using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Certificate_Manager.Data.Migrations
{
    /// <inheritdoc />
    public partial class codesign2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScriptContent",
                table: "SignedScript",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScriptContent",
                table: "SignedScript");
        }
    }
}
