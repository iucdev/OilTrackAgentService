using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Service.LocalDb;
using System;
using System.Data.SQLite;
using Service.Enums;

namespace Service.Dtos {
    public class QueueTaskRecord {
        public int Id { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public QueueTaskType Type { get; set; }
        public QueueTaskStatus Status { get; set; }
        public string Items { get; set; }
        public string Error { get; set; }

        public void AddToDb() {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string insertQuery = @"
                INSERT INTO QueueTaskRecords (
                    CreateDate,
                    UpdateDate, 
                    Type, 
                    Status, 
                    Items, 
                    Error
                ) VALUES (
                    @CreateDate, 
                    @UpdateDate, 
                    @Type,
                    @Status,
                    @Items, 
                    @Error
                )";

                using (var command = new SQLiteCommand(insertQuery, connection)) {
                    command.Parameters.AddWithValue("@CreateDate", CreateDate.ToDbString());
                    command.Parameters.AddWithValue("@UpdateDate", UpdateDate.ToDbString());
                    command.Parameters.AddWithValue("@Type", Type);
                    command.Parameters.AddWithValue("@Status", Status);
                    command.Parameters.AddWithValue("@Items", Items);
                    command.Parameters.AddWithValue("@Error", Error);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static QueueTaskRecord GetFirstTaskFromDb() {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string selectQuery = @"
                    SELECT * FROM QueueTaskRecords 
                    ORDER BY Id
                    LIMIT 1";

                using (var command = new SQLiteCommand(selectQuery, connection)) {
                    using (var reader = command.ExecuteReader()) {
                        if (reader.Read()) {
                            Enum.TryParse<QueueTaskType>(reader["Type"].ToString(), out var type);
                            Enum.TryParse<QueueTaskStatus>(reader["Status"].ToString(), out var status);
                            return new QueueTaskRecord {
                                Id = int.Parse(reader["Id"].ToString()),
                                CreateDate = DateTime.Parse(reader["CreateDate"].ToString()),
                                UpdateDate = DateTime.Parse(reader["UpdateDate"].ToString()),
                                Type = type,
                                Status = status,
                                Items = reader["Items"].ToString(),
                                Error = reader["Error"].ToString()
                            };
                        } else {
                            return null;
                        }
                    }
                }
            }
        }

        public void UpdateInDb() {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string updateQuery = @"
                UPDATE QueueTaskRecords SET 
                    CreateDate = @CreateDate, 
                    UpdateDate = @UpdateDate, 
                    Type = @Type, 
                    Status = @Status, 
                    Items = @Items, 
                    RemainingRetryCount = @RemainingRetryCount, 
                    Error = @Error
                WHERE Id = @Id";

                using (var command = new SQLiteCommand(updateQuery, connection)) {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.Parameters.AddWithValue("@CreateDate", CreateDate.ToDbString());
                    command.Parameters.AddWithValue("@UpdateDate", UpdateDate.ToDbString());
                    command.Parameters.AddWithValue("@Type", Type);
                    command.Parameters.AddWithValue("@Status", Status);
                    command.Parameters.AddWithValue("@Items", Items);
                    command.Parameters.AddWithValue("@Error", Error);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteFromDb() {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string deleteQuery = "DELETE FROM QueueTaskRecords WHERE Id = @Id";

                using (var command = new SQLiteCommand(deleteQuery, connection)) {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
