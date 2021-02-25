
            // Fill the database with demodata
            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "UID", "IP", "Name", "Email", "UpdatedDate" },
                values: new object[] { "583ab273-0193-425e-9de5-eec928cd8f90", "31.4.245.180", "Johan", "jool@me.com", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Event",
                columns: new[] { "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, "Survey 2021", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Class",
                columns: new[] { "EventId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 1, "Frågor på startsidan", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Class",
                columns: new[] { "EventId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 1, "Frågor på resultatsidan", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Cause",
                columns: new[] { "EventId", "ClassId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 1, 1, "Vad heter du?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Cause",
                columns: new[] { "EventId", "ClassId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 1, 2, "Hur gammal är du?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Cause",
                columns: new[] { "EventId", "ClassId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 2, 1, "Vet du vad Blazor är för något?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Cause",
                columns: new[] { "EventId", "ClassId", "Order", "Value", "UpdatedDate" },
                values: new object[] { 1, 2, 2, "Vad är det för skillnad på inject och insert?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Exclude",
                columns: new[] { "EventId", "CauseId", "UserId", "Value", "UpdatedDate" },
                values: new object[] { 1, 4, 1, "Visa ej denna för Johan", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Effect",
                columns: new[] { "EventId", "CauseId", "ClassId", "UserId", "Value", "UpdatedDate" },
                values: new object[] { 1, 1, 1, 1, "Johan", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Effect",
                columns: new[] { "EventId", "CauseId", "ClassId", "UserId", "Value", "UpdatedDate" },
                values: new object[] { 1, 2, 1, 1, "48 år", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            migrationBuilder.InsertData(
                table: "Effect",
                columns: new[] { "EventId", "CauseId", "ClassId", "UserId", "Value", "UpdatedDate" },
                values: new object[] { 1, 3, 2, 1, "Ja, det som rockar!", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });

            for (int i = 1; i < 4; i++)
            {
                migrationBuilder.InsertData(
                    table: "Meta",
                    columns: new[] { "Key", "Value", "UpdatedDate" },
                    values: new object[] { "Meta " + i.ToString(), i.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            }

            for (int i = 1; i < 4; i++)
            {
                migrationBuilder.InsertData(
                    table: "Meta",
                    columns: new[] { "UserId", "Key", "Value", "UpdatedDate" },
                    values: new object[] { 1, "Meta " + i.ToString(), i.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            }