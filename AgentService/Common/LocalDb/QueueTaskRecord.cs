using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Service.LocalDb;
using System;
using System.Data.SQLite;
using Service.Enums;
using NLog;
using AgentService.References;

namespace Service.Dtos {
    public class QueueTaskRecord {
        public int Id { get; set; }
        public string PackageId { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateDateOnly => CreateDate.ToShortDateString();
        public string CreateTime => CreateDate.ToShortTimeString();
        public QueueTaskType Type { get; set; }
        public QueueTaskStatus Status { get; set; }
        public string Items { get; set; }
        public string Error { get; set; }

        public void AddToDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string insertQuery = $@"
                INSERT INTO {nameof(QueueTaskRecord)} (
                    {nameof(QueueTaskRecord.PackageId)},
                    {nameof(QueueTaskRecord.CreateDate)},
                    {nameof(QueueTaskRecord.Type)}, 
                    {nameof(QueueTaskRecord.Status)}, 
                    {nameof(QueueTaskRecord.Items)}, 
                    {nameof(QueueTaskRecord.Error)}
                ) VALUES (
                    @{nameof(QueueTaskRecord.PackageId)}, 
                    @{nameof(QueueTaskRecord.CreateDate)}, 
                    @{nameof(QueueTaskRecord.Type)},
                    @{nameof(QueueTaskRecord.Status)},
                    @{nameof(QueueTaskRecord.Items)}, 
                    @{nameof(QueueTaskRecord.Error)}
                )";

                using (var command = new SQLiteCommand(insertQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.PackageId)}", PackageId);
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.CreateDate)}", CreateDate.ToDbString());
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.Type)}", Type);
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.Status)}", Status);
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.Items)}", Items);
                    command.Parameters.AddWithValue($"@{nameof(QueueTaskRecord.Error)}", Error);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static QueueTaskRecord GetFirstTaskFromDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string selectQuery = $@"
                    SELECT * FROM {nameof(QueueTaskRecord)} 
                    WHERE {nameof(Status)} != @{nameof(Status)}
                    ORDER BY {nameof(Id)}
                    LIMIT 1";

                using (var command = new SQLiteCommand(selectQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(Status)}", QueueTaskStatus.Processed);
                    using (var reader = command.ExecuteReader()) {
                        if (reader.Read()) {
                            Enum.TryParse<QueueTaskType>(reader[$"{nameof(Type)}"].ToString(), out var type);
                            Enum.TryParse<QueueTaskStatus>(reader[$"{nameof(Status)}"].ToString(), out var status);
                            return new QueueTaskRecord
                            {
                                Id = int.Parse(reader[$"{nameof(Id)}"].ToString()),
                                PackageId = reader[$"{nameof(PackageId)}"].ToString(),
                                CreateDate = DateTime.Parse(reader[$"{nameof(CreateDate)}"].ToString()),
                                Type = type,
                                Status = status,
                                Items = reader[$"{nameof(Items)}"].ToString(),
                                Error = reader[$"{nameof(Error)}"].ToString()
                            };
                        } else {
                            return null;
                        }
                    }
                }
            }
        }

        public void UpdateInDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string updateQuery = $@"
                UPDATE {nameof(QueueTaskRecord)} SET 
                    {nameof(CreateDate)} = @{nameof(CreateDate)}, 
                    {nameof(Type)} = @{nameof(Type)}, 
                    {nameof(Status)} = @{nameof(Status)}, 
                    {nameof(Items)} = @{nameof(Items)}, 
                    {nameof(Error)} = @{nameof(Error)}
                WHERE Id = @Id";

                using (var command = new SQLiteCommand(updateQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
                    command.Parameters.AddWithValue($"@{nameof(CreateDate)}", CreateDate.ToDbString());
                    command.Parameters.AddWithValue($"@{nameof(Type)}", Type);
                    command.Parameters.AddWithValue($"@{nameof(Status)}", Status);
                    command.Parameters.AddWithValue($"@{nameof(Items)}", Items);
                    command.Parameters.AddWithValue($"@{nameof(Error)}", Error);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteFromDb(Logger logger)
        {
            using (var connection = new SQLiteConnection(DatabaseManager.ConnectionString)) {
                connection.Open();
                string deleteQuery = $"DELETE FROM {nameof(QueueTaskRecord)} WHERE {nameof(Id)} = @{nameof(Id)}";

                using (var command = new SQLiteCommand(deleteQuery, connection)) {
                    command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
