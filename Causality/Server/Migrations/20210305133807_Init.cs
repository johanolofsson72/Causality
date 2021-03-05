﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

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
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Meta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    CauseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExcludeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessId = table.Column<int>(type: "INTEGER", nullable: false),
                    StateId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meta", x => x.Id);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Cause_CauseId",
                    //    column: x => x.CauseId,
                    //    principalTable: "Cause",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Class_ClassId",
                    //    column: x => x.ClassId,
                    //    principalTable: "Class",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Effect_EffectId",
                    //    column: x => x.EffectId,
                    //    principalTable: "Effect",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Event_EventId",
                    //    column: x => x.EventId,
                    //    principalTable: "Event",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Exclude_ExcludeId",
                    //    column: x => x.ExcludeId,
                    //    principalTable: "Exclude",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Process_ProcessId",
                    //    column: x => x.ProcessId,
                    //    principalTable: "Process",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_Result_ResultId",
                    //    column: x => x.ResultId,
                    //    principalTable: "Result",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_State_StateId",
                    //    column: x => x.StateId,
                    //    principalTable: "State",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
                    //table.ForeignKey(
                    //    name: "FK_Meta_User_UserId",
                    //    column: x => x.UserId,
                    //    principalTable: "User",
                    //    principalColumn: "Id",
                    //    onDelete: ReferentialAction.Cascade);
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
                name: "IX_Meta_CauseId",
                table: "Meta",
                column: "CauseId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_ClassId",
                table: "Meta",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_EffectId",
                table: "Meta",
                column: "EffectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_EventId",
                table: "Meta",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_ExcludeId",
                table: "Meta",
                column: "ExcludeId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id",
                table: "Meta",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_CauseId",
                table: "Meta",
                columns: new[] { "Id", "CauseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_ClassId",
                table: "Meta",
                columns: new[] { "Id", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_EffectId",
                table: "Meta",
                columns: new[] { "Id", "EffectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_EventId",
                table: "Meta",
                columns: new[] { "Id", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_ExcludeId",
                table: "Meta",
                columns: new[] { "Id", "ExcludeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_ProcessId",
                table: "Meta",
                columns: new[] { "Id", "ProcessId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_ResultId",
                table: "Meta",
                columns: new[] { "Id", "ResultId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_StateId",
                table: "Meta",
                columns: new[] { "Id", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_Id_UserId",
                table: "Meta",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meta_ProcessId",
                table: "Meta",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_ResultId",
                table: "Meta",
                column: "ResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Meta_StateId",
                table: "Meta",
                column: "StateId");

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
                name: "IX_State_Id_EventId_ProcessId",
                table: "State",
                columns: new[] { "Id", "EventId", "ProcessId" });

            migrationBuilder.CreateIndex(
                name: "IX_User_Id",
                table: "User",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Event",
                columns: new[] { "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, "Ronneby Motorbåtsklubb", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meta");

            migrationBuilder.DropTable(
                name: "Effect");

            migrationBuilder.DropTable(
                name: "Exclude");

            migrationBuilder.DropTable(
                name: "Process");

            migrationBuilder.DropTable(
                name: "Result");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropTable(
                name: "Cause");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Event");
        }
    }
}