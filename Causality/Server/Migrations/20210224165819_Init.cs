using Microsoft.EntityFrameworkCore.Migrations;

namespace Causality.Server.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Process",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Process", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Result", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "State",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UID = table.Column<string>(type: "TEXT", nullable: false),
                    IP = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Class", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Class_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meta_Process_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Process",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meta_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cause",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cause", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cause_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cause_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Effect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Effect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Effect_Cause_CauseId",
                        column: x => x.CauseId,
                        principalTable: "Cause",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Effect_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Effect_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exclude",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exclude", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exclude_Cause_CauseId",
                        column: x => x.CauseId,
                        principalTable: "Cause",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exclude_Event_EventId",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exclude_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cause_ClassId",
                table: "Cause",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Cause_EventId",
                table: "Cause",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Cause_Id",
                table: "Cause",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cause_Id_EventId_ClassId",
                table: "Cause",
                columns: new[] { "Id", "EventId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Class_EventId",
                table: "Class",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Class_Id",
                table: "Class",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Class_Id_EventId",
                table: "Class",
                columns: new[] { "Id", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Effect_CauseId",
                table: "Effect",
                column: "CauseId");

            migrationBuilder.CreateIndex(
                name: "IX_Effect_ClassId",
                table: "Effect",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Effect_EventId",
                table: "Effect",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Effect_Id",
                table: "Effect",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Effect_Id_EventId_CauseId_ClassId_UserId",
                table: "Effect",
                columns: new[] { "Id", "EventId", "CauseId", "ClassId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_Id",
                table: "Event",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Exclude_CauseId",
                table: "Exclude",
                column: "CauseId");

            migrationBuilder.CreateIndex(
                name: "IX_Exclude_EventId",
                table: "Exclude",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Exclude_Id",
                table: "Exclude",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Exclude_Id_EventId_CauseId_UserId",
                table: "Exclude",
                columns: new[] { "Id", "EventId", "CauseId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exclude_UserId",
                table: "Exclude",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id",
                table: "Meta",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_ProcessId",
                table: "Meta",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_UserId",
                table: "Meta",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Process_Id",
                table: "Process",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Process_Id_EventId",
                table: "Process",
                columns: new[] { "Id", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Result_Id",
                table: "Result",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Result_Id_ProcessId_EventId_CauseId_ClassId_UserId",
                table: "Result",
                columns: new[] { "Id", "ProcessId", "EventId", "CauseId", "ClassId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_State_Id",
                table: "State",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_State_Id_EventId_CauseId_ClassId_UserId",
                table: "State",
                columns: new[] { "Id", "EventId", "CauseId", "ClassId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_User_Id",
                table: "User",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Effect");

            migrationBuilder.DropTable(
                name: "Exclude");

            migrationBuilder.DropTable(
                name: "Meta");

            migrationBuilder.DropTable(
                name: "Result");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropTable(
                name: "Cause");

            migrationBuilder.DropTable(
                name: "Process");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Event");
        }
    }
}
