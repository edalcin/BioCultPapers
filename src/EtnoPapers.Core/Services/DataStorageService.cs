using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EtnoPapers.Core.Models;
using EtnoPapers.Core.Utils;
using Microsoft.Data.Sqlite;

namespace EtnoPapers.Core.Services
{
    /// <summary>
    /// Local persistence for ArticleRecord data using SQLite+JSON (DA6, ADR-005 of
    /// Arquitetura-BioCultural). Each record is stored as one row with its full
    /// JSON serialization in the `data` column — writes touch only the affected
    /// row instead of rewriting the entire dataset, and there is no artificial
    /// record cap (the previous data.json format capped at 1000 records).
    /// </summary>
    public class DataStorageService
    {
        private readonly string _dataPath;
        private readonly object _lockObject = new();
        private SqliteConnection _connection;

        public DataStorageService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "EtnoPapers"
            );

            Directory.CreateDirectory(appDataPath);
            _dataPath = Path.Combine(appDataPath, "biocultpapers.sqlite");
        }

        /// <summary>
        /// Initializes the storage service: opens the SQLite connection and
        /// ensures the schema exists (idempotent).
        /// </summary>
        public void Initialize()
        {
            lock (_lockObject)
            {
                if (_connection != null)
                    return;

                _connection = new SqliteConnection($"Data Source={_dataPath}");
                _connection.Open();

                using var pragmaCmd = _connection.CreateCommand();
                pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
                pragmaCmd.ExecuteNonQuery();

                using var schemaCmd = _connection.CreateCommand();
                schemaCmd.CommandText = "CREATE TABLE IF NOT EXISTS records (id TEXT PRIMARY KEY, data TEXT NOT NULL);";
                schemaCmd.ExecuteNonQuery();
            }
        }

        private SqliteConnection Connection
        {
            get
            {
                if (_connection == null)
                    Initialize();
                return _connection;
            }
        }

        /// <summary>
        /// Loads all records from storage.
        /// </summary>
        public List<ArticleRecord> LoadAll()
        {
            lock (_lockObject)
            {
                try
                {
                    var records = new List<ArticleRecord>();
                    using var cmd = Connection.CreateCommand();
                    cmd.CommandText = "SELECT data FROM records";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var record = JsonSerializationHelper.DeserializeFromJson(reader.GetString(0));
                        if (record != null)
                            records.Add(record);
                    }
                    return records;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading records: {ex.Message}");
                    return new List<ArticleRecord>();
                }
            }
        }

        /// <summary>
        /// Gets a record by ID.
        /// </summary>
        public ArticleRecord GetById(string id)
        {
            lock (_lockObject)
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT data FROM records WHERE id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                var data = cmd.ExecuteScalar() as string;
                return data != null ? JsonSerializationHelper.DeserializeFromJson(data) : null;
            }
        }

        /// <summary>
        /// Creates a new record and persists to storage.
        /// </summary>
        public bool Create(ArticleRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(record.Id))
                    record.Id = Guid.NewGuid().ToString();

                record.CreatedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;

                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "INSERT INTO records (id, data) VALUES ($id, $data)";
                cmd.Parameters.AddWithValue("$id", record.Id);
                cmd.Parameters.AddWithValue("$data", JsonSerializationHelper.SerializeToJson(record));
                cmd.ExecuteNonQuery();
                return true;
            }
        }

        /// <summary>
        /// Updates an existing record.
        /// </summary>
        public bool Update(ArticleRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            lock (_lockObject)
            {
                var existing = GetById(record.Id);
                if (existing == null)
                    return false;

                record.CreatedAt = existing.CreatedAt;
                record.UpdatedAt = DateTime.UtcNow;

                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "UPDATE records SET data = $data WHERE id = $id";
                cmd.Parameters.AddWithValue("$id", record.Id);
                cmd.Parameters.AddWithValue("$data", JsonSerializationHelper.SerializeToJson(record));
                var affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
        }

        /// <summary>
        /// Deletes a record by ID.
        /// </summary>
        public bool Delete(string id)
        {
            lock (_lockObject)
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "DELETE FROM records WHERE id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                var affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
        }

        /// <summary>
        /// Deletes multiple records by IDs.
        /// </summary>
        public int DeleteMultiple(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return 0;

            lock (_lockObject)
            {
                using var transaction = Connection.BeginTransaction();
                var count = 0;
                foreach (var id in ids)
                {
                    using var cmd = Connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = "DELETE FROM records WHERE id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    count += cmd.ExecuteNonQuery();
                }
                transaction.Commit();
                return count;
            }
        }

        /// <summary>
        /// Gets total record count.
        /// </summary>
        public int Count()
        {
            lock (_lockObject)
            {
                using var cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM records";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Whether storage is at capacity. SQLite has no artificial record cap
        /// (unlike the previous data.json format's 1000-record limit) — always false.
        /// </summary>
        [Obsolete("SQLite storage has no record limit; this always returns false.")]
        public bool IsAtLimit() => false;

        /// <summary>
        /// Remaining capacity. SQLite has no artificial record cap — always
        /// int.MaxValue.
        /// </summary>
        [Obsolete("SQLite storage has no record limit; this always returns int.MaxValue.")]
        public int GetRemainingCapacity() => int.MaxValue;

        /// <summary>
        /// Gets the data file path.
        /// </summary>
        public string GetDataFilePath()
        {
            return _dataPath;
        }
    }
}
