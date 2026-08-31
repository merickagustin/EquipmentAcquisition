using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentAcquisition.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CacheRefreshQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcquisitionRequestId = table.Column<int>(type: "int", nullable: false),
                    EnqueuedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheRefreshQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAcquisitionDetailCaches",
                columns: table => new
                {
                    AcquisitionRequestId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EquipmentCategoryId = table.Column<int>(type: "int", nullable: false),
                    EquipmentCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestedByJobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: true),
                    PoNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    VendorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RefreshedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAcquisitionDetailCaches", x => x.AcquisitionRequestId);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    EquipmentCategoryId = table.Column<int>(type: "int", nullable: false),
                    RequestedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionRequests", x => x.Id);
                    table.CheckConstraint("CK_AcquisitionRequest_MutuallyExclusiveDates", "[ApprovedDate] IS NULL OR [RejectedDate] IS NULL");
                    table.ForeignKey(
                        name: "FK_AcquisitionRequests_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcquisitionRequests_Employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcquisitionRequests_Employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcquisitionRequests_EquipmentCategories_EquipmentCategoryId",
                        column: x => x.EquipmentCategoryId,
                        principalTable: "EquipmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditTrail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableAffected = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AffectedId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DateApplied = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrail", x => x.Id);
                    table.CheckConstraint("CK_AuditTrail_Action", "[Action] IN ('Insert', 'Update', 'Delete')");
                    table.ForeignKey(
                        name: "FK_AuditTrail_Employees_ChangedByEmployeeId",
                        column: x => x.ChangedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcquisitionRequestId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    PoNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_AcquisitionRequests_AcquisitionRequestId",
                        column: x => x.AcquisitionRequestId,
                        principalTable: "AcquisitionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrderId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AcquiredDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRequests_ApprovedByEmployeeId",
                table: "AcquisitionRequests",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRequests_DepartmentId_EquipmentCategoryId_RequestDate",
                table: "AcquisitionRequests",
                columns: new[] { "DepartmentId", "EquipmentCategoryId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRequests_EquipmentCategoryId",
                table: "AcquisitionRequests",
                column: "EquipmentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRequests_RequestedByEmployeeId",
                table: "AcquisitionRequests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTag",
                table: "Assets",
                column: "AssetTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_DepartmentId_Status",
                table: "Assets",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_PurchaseOrderId",
                table: "Assets",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_ChangedByEmployeeId",
                table: "AuditTrail",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditTrail_TableAffected_AffectedId_DateApplied",
                table: "AuditTrail",
                columns: new[] { "TableAffected", "AffectedId", "DateApplied" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAcquisitionDetailCaches_ApprovedByEmployeeId",
                table: "EquipmentAcquisitionDetailCaches",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAcquisitionDetailCaches_DepartmentId_Status_RequestDate",
                table: "EquipmentAcquisitionDetailCaches",
                columns: new[] { "DepartmentId", "Status", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAcquisitionDetailCaches_EquipmentCategoryId",
                table: "EquipmentAcquisitionDetailCaches",
                column: "EquipmentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAcquisitionDetailCaches_RequestedByEmployeeId",
                table: "EquipmentAcquisitionDetailCaches",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAcquisitionDetailCaches_VendorId",
                table: "EquipmentAcquisitionDetailCaches",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCategories_Name",
                table: "EquipmentCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ParentId_DisplayOrder",
                table: "MenuItems",
                columns: new[] { "ParentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_AcquisitionRequestId",
                table: "PurchaseOrders",
                column: "AcquisitionRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PoNumber",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_VendorId",
                table: "PurchaseOrders",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "AuditTrail");

            migrationBuilder.DropTable(
                name: "CacheRefreshQueue");

            migrationBuilder.DropTable(
                name: "EquipmentAcquisitionDetailCaches");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "AcquisitionRequests");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "EquipmentCategories");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
