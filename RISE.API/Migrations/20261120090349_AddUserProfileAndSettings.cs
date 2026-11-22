using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RISE.API.Migrations;

/// <inheritdoc />
public partial class _20261120090349_AddUserProfileAndSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastLoginAtUtc",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyFarmerRecordUpdates",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyImportantSystemAlerts",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyNewFarmerRegistration",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NotifyReportGenerationUpdates",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "NotifySystemAnnouncements",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ProfilePicturePath",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastLoginAtUtc",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "NotifyFarmerRecordUpdates",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "NotifyImportantSystemAlerts",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "NotifyNewFarmerRegistration",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "NotifyReportGenerationUpdates",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "NotifySystemAnnouncements",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "ProfilePicturePath",
            table: "Users");
    }
}
