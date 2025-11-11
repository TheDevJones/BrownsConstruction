using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCSApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetRisk",
                table: "AIAnalyses",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ContingencyAmount",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostBreakdown",
                table: "AIAnalyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DirectCosts",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EquipmentCost",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IndirectCosts",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyFindings",
                table: "AIAnalyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborCost",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialsCost",
                table: "AIAnalyses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectDurationDays",
                table: "AIAnalyses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QualityRisk",
                table: "AIAnalyses",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskFactors",
                table: "AIAnalyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SafetyRisk",
                table: "AIAnalyses",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScheduleRisk",
                table: "AIAnalyses",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuredData",
                table: "AIAnalyses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetRisk",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "ContingencyAmount",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "CostBreakdown",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "DirectCosts",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "EquipmentCost",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "IndirectCosts",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "KeyFindings",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "LaborCost",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "MaterialsCost",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "ProjectDurationDays",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "QualityRisk",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "RiskFactors",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "SafetyRisk",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "ScheduleRisk",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "StructuredData",
                table: "AIAnalyses");
        }
    }
}
