using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using NLog;
using QuintetLab.SQLiteHelperCore.Common.V1;

namespace QuintetLab.SQLiteHelper.Common.V1
{
    // Таблица БД
    public class SQLiteTable<TRec> where TRec: SQLiteRecord, new()
    {
        private SQLiteDB db;
        private SQLiteConnection connection;
        private ILogger Logger;
        public string TableName { get; set; }
        public SQLiteConnection GetConnection()
        {
            try
            {
                if (db != null) return db.GetConnection();
            }
            catch (Exception ex)
            {
                db?.Logger.Error($"Error: {ex}");
                return null;
            }
            return new SQLiteConnection(connection);
        }

        public SQLiteTable(SQLiteDB db, string name)
        {
            Logger = db.Logger;
            this.db = db;
            TableName = name;
        }

        public SQLiteTable(SQLiteConnection con, string name)
        {
            connection = con;
            TableName = name;
        }

        #region [Работа с записями таблицы]
        // Вставка (возвращает ID вставленной записи)
        public bool Insert(TRec rec)
        {
            try
            {
                return RunCommand((cmd)=>
                {
                    var query = rec.GetSQLForInsert();
                    cmd.CommandText = $"INSERT INTO '{TableName}' {query.SQLtext};";
                    if (query.Parameters != null && query.Parameters.Length > 0)
                        cmd.Parameters.AddRange(query.Parameters);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    cmd.ExecuteScalar();
                });
                
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot insert records: {ex}");
                return false;
            }
        }



        // Обновление
        public bool Update(TRec rec)
        {
            try
            {
                RunCommand((cmd) =>
                {
                    var query = rec.GetSQLForUpdate();
                    cmd.CommandText = $"UPDATE '{TableName}' {query.SQLtext};";
                    if (query.Parameters != null && query.Parameters.Length > 0)
                        cmd.Parameters.AddRange(query.Parameters);
                    cmd.ExecuteNonQuery();
                
                });
                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot update records: {ex}");
                return false;
                //throw;

            }
        }
        // Вставка (возвращает ID вставленной записи)
        public bool Upsert(TRec rec)
        {
            object res = null;

            try
            {
                RunCommand((cmd) =>
                {
                    var query = rec.GetSQLForUpsert();
                    cmd.CommandText = $"INSERT INTO '{TableName}' {query.SQLtext};";
                    if (query.Parameters != null && query.Parameters.Length > 0)
                        cmd.Parameters.AddRange(query.Parameters);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT last_insert_rowid()";
                    res = cmd.ExecuteScalar();
                });
                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot upsert records: {ex}");
                return false;
                //throw;

            }
        }



        public bool DeleteAll()
        {
            try
            {
                RunCommand((cmd) =>
                {
                    cmd.CommandText = $"DELETE FROM '{TableName}'";
                    cmd.ExecuteNonQuery();
                });
                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot delete records: {ex}");
                return false;
                //throw;
            }
        }


        // Удаление
        public bool Delete(long id)
        {
            try
            {
                RunCommand((cmd) =>
                {
                    cmd.CommandText = $"DELETE FROM '{TableName}' WHERE id = {id}";
                    cmd.ExecuteNonQuery();
                });
                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot delete records: {ex}");
                //throw;
                return false;
            }
        }

        public bool Delete(SqlExpression whereConditions)
        {
            try
            {

                RunCommand((cmd) =>
                {
                    cmd.CommandText = $"DELETE FROM '{TableName}' WHERE {whereConditions.SQLtext}";
                    if (whereConditions.Parameters != null && whereConditions.Parameters.Length > 0)
                        cmd.Parameters.AddRange(whereConditions.Parameters);
                    cmd.ExecuteNonQuery();
                });
                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Cannot delete records: {ex}");
                //throw;
                return false;
            }
        }

        // Поиск записи
        public TRec Find(SqlExpression whereConditions)
        {
            var res = GetAll(whereConditions, 1);
            if (res != null && res.Count > 0) return res[0];
            return null;
        }

        // Получение всех записей таблицы
        public List<TRec> GetAll(SqlExpression whereConditions = null, int? limit = null, SqlExpression orderByExpression = null)
        {

            try
            {
                List<TRec> res = new List<TRec>();
                RunCommand((cmd) =>
                {
                    cmd.CommandText =
                        $"SELECT * FROM '{TableName}'{(whereConditions == null ? string.Empty : $" WHERE {whereConditions.SQLtext}")}{(orderByExpression == null ? string.Empty : $" {orderByExpression.SQLtext}")}{(limit.HasValue ? $" LIMIT {limit.Value}" : string.Empty)};";
                    if (whereConditions != null &&
                        whereConditions.Parameters != null &&
                        whereConditions.Parameters.Length > 0)
                        cmd.Parameters.AddRange(whereConditions.Parameters);
                    if (orderByExpression != null &&
                        orderByExpression.Parameters != null &&
                        orderByExpression.Parameters.Length > 0)
                        cmd.Parameters.AddRange(orderByExpression.Parameters);
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            TRec rec = new TRec();
                            rec.LoadFromTableRow(r);
                            res.Add(rec);
                        }
                    }
                });
                return res;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Error in  getting records: {ex}");
                //throw;
                return null;
            }

        }

        #endregion

        private bool RunCommand(Action<SQLiteCommand> action)
        {
            try
            {
                using (SQLiteConnection con = GetConnection())
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    action(cmd);
                }

                return true;
            }
            catch (Exception ex)
            {
                db.Logger.Error($"Error in execute command: {ex}");
                return false;
                //throw;
            }

        }


        #region [Статические методы]
        // Проверяем, существует ли такая таблица в БД
        public bool Exists(SQLiteConnection con, string name)
        {
            try
            {
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM sqlite_master WHERE type = 'table' AND name = '{name}'";
                    SQLiteDataReader r = cmd.ExecuteReader();
                    if (r.HasRows) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in check tables for exist command: {ex}");
                throw;
            }
        }


        internal bool CreateNew(SQLiteConnection con, string name)
        {
            try
            {
                Type t = typeof(TRec);
                var props = t.GetProperties();
                List<SQLiteRecord.FieldCreationBuilder> fieldsSQL = new List<SQLiteRecord.FieldCreationBuilder>();
                foreach (var prop in props)
                {
                    var field = GetFieldCreationSQL(prop);
                    if (field != null) fieldsSQL.Add(field);
                }

                bool compoundPrimaryKey = fieldsSQL.Count(x => x.Attribute.IsPrimaryKey) > 1;
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText =
                        $"CREATE TABLE '{name}'\n(\n{string.Join(",\n", fieldsSQL.Select(x => $"  {x.GetSQL(!compoundPrimaryKey)}"))}{(compoundPrimaryKey ? $",\n  PRIMARY KEY ({string.Join(", ", fieldsSQL.Where(x => x.Attribute.IsPrimaryKey).Select(x => $"\"{x.FieldName}\""))})\t/* Primary key */" : string.Empty)}\n);";
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in create new table '{name}': {ex}");
            }

            return false;
        }
        internal SQLiteRecord.FieldCreationBuilder GetFieldCreationSQL(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes(false);
            var attr = attributes.FirstOrDefault(a => a is SQLiteFieldAttribute) as SQLiteFieldAttribute;
            if (attr == null) return null;

            return new SQLiteRecord.FieldCreationBuilder
            {
                FieldName = string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName,
                DataType = string.IsNullOrEmpty(attr.FieldType) ? SharpTypeToDBType(prop.PropertyType) : attr.FieldType,
                Attribute = attr
            };

        }
        internal string SharpTypeToDBType(Type t)
        {
            if (t == typeof(int) || t == typeof(int?) ||
                t == typeof(bool) || t == typeof(bool?)) return "INTEGER";
            if (t == typeof(string)) return "TEXT";
            if (t == typeof(double) || t == typeof(double?) ||
                t == typeof(float) || t == typeof(float?)) return "DOUBLE";
            if (t == typeof(DateTime) || t == typeof(DateTime?)) return "DATETIME";
            if (t == typeof(decimal) || t == typeof(decimal?)) return "NUMERIC";
            if (t == typeof(long) || t == typeof(long?)) return "BIGINT";
            if (t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?)) return "TIMESTAMP";
            throw new Exception($"Unknown Field Type {t.FullName}");
        }






        #endregion
    }
}
