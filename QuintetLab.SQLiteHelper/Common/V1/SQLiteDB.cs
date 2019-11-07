using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace QuintetLab.SQLiteHelperCore.Common.V1
{
    public class SQLiteDB : IDisposable
    {
        #region [Поля, переопределяемые в наследниках класса]
        // Обязательные таблицы, которые должна содержать БД
        protected virtual string[] RequiredTables { get; set; }
        // Версия БД, которую реализует этот класс
        protected virtual Version CurrentVersion
        {
            get { return new Version(); }
        }
        // Обновления структуры БД
        protected virtual Dictionary<Version, Func<SQLiteConnection, Version>> DbUpdates
        {
            get { return new Dictionary<Version, Func<SQLiteConnection, Version>>(); }
        }
        #endregion

        #region [Создание, диагностика и обновление БД]
        private string ConnectionString;
        public ILogger Logger;
        public SQLiteDB(string fileName,string scriptPath, Assembly assembly, ref bool result, bool createIfNotExists = false, Guid? dbGuid = null)
        {
            ConnectionString = GetConnectionStringFromFileName(fileName);
            Logger = LogManager.GetCurrentClassLogger();
            // Проверяем наличие файла БД
            if (!File.Exists(fileName))
            {
                if (createIfNotExists) CreateNewDb(fileName,scriptPath, assembly, dbGuid);
                else
                {
                    Logger.Error($"File of database {fileName} not found");
                    //throw new Exception(String.Format("Файл базы данных '{0}' не найден", fileName));
                    result = false;
                }
            }

            // Проверка Guid и версии БД и обновление при необходимости
            if (dbGuid != null && dbGuid != Guid)
            {
                Logger.Error($"Database identifer {Guid} doesn't match required {dbGuid}");
                //throw new Exception(String.Format("Идентификатор базы данных '{0}' не соответствует требуемому значению '{1}'", Guid, dbGuid));
                //return false;
                result = false;
            }
            using (SQLiteConnection con = GetConnection())
            {
                while (Version != CurrentVersion)
                {
                    if (!DbUpdates.ContainsKey(Version))
                    {
                        Logger.Error($"Not found the database update from v. {Version} to v. {CurrentVersion}");
                        result = false;
                        //throw new Exception(String.Format("Не найдено обновление структуры БД с версии '{0}' до версии '{1}'", Version, CurrentVersion));
                    }
                    Version oldV = Version;
                    Version = DbUpdates[oldV](con);
                    if (Version == oldV)
                    {
                        Logger.Info($"After database update from v. {oldV} number is not changed ");
                        //throw new Exception(String.Format("После обновления с версии '{0}' номер версии не изменился",oldV));
                        result = false;
                    }
                }
            }

            // Проверка наличия необходимых объектов БД
            string msg = String.Empty;
            if (!TestDBStructure("table", RequiredTables))
            {
                result = false;
                //throw new Exception(msg);
            }
            result = true;
        }

        private string GetConnectionStringFromFileName(string fileName)
        {
            return String.Format("Data Source={0};Version=3;", fileName);
        }

        internal void CreateNewDb(string fileName,string scriptPath,Assembly assembly, Guid? dbGuid)
        {
            try
            {
                string creationScript = GetSciptFromEmbeddedRes(scriptPath, assembly);
                using (SQLiteConnection con = GetConnection())
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = creationScript;
                    cmd.ExecuteNonQuery();
                }

                Version = new Version();
                Guid = dbGuid.HasValue ? dbGuid.Value : Guid.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error($"Creating settings database error: {ex}");
            }
        }

        public void CreateNewTable(SQLiteConnection con, string name, Type type)
        {
            try
            {
                //Type t = typeof(type);
                var props = type.GetProperties();
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
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in create new table '{name}': {ex}");
            }

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

        private bool TestDBStructure(string objectType, string[] requiredObjects)
        {
            if (requiredObjects == null || requiredObjects.Length == 0) return true;
           // msg = String.Empty;
            try
            {
                using (SQLiteConnection con = GetConnection())
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandText = String.Format("SELECT * FROM sqlite_master WHERE type = '{0}'", objectType);
                    SQLiteDataReader r = cmd.ExecuteReader();
                    List<string> objects = new List<string>();
                    foreach (DbDataRecord record in r) objects.Add((r["name"] as String).ToLower());
                    List<string> NotFoundTables = RequiredTables.Where(x => !objects.Contains(x)).ToList();
                    if (NotFoundTables.Count == 0) return true;
                    //msg = String.Format("Структура базы данных не корретна. Не найдены следующие объекты типа '{0}': {1}", objectType, String.Join(", ", NotFoundTables));
                    Logger.Error(String.Format("Database structure is not correct. Not found next object types '{0}': {1}", objectType, String.Join(", ", NotFoundTables)));
                }
            }
            catch (Exception ex)
            {
                //msg = String.Format("Ошибка при запросе списка объектов типа '{0}': {1}", objectType, ex.Message);
                Logger.Error(String.Format("Error in query to get list of object types '{0}': {1}", objectType, ex.Message));
            }
            return false;
        }
        #endregion

        #region [Настройки]
        public Version Version
        {
            get
            {
                string s = Settings["version"];
                Version ver;
                if (!Version.TryParse(s, out ver)) ver = new Version();
                return ver;
            }

            protected set
            {
                Settings["version"] = value.ToString();
            }
        }

        public Guid Guid
        {
            get
            {
                string s = Settings["guid"];
                Guid guid;
                if (!Guid.TryParse(s, out guid)) guid = Guid.Empty;
                return guid;
            }

            protected set
            {
                Settings["guid"] = value.ToString();
            }
        }

        public class SettingsLst
        {
            private SQLiteDB db;
            public SettingsLst(SQLiteDB db)
            {
                this.db = db;
            }

            public string this[string key]
            {
                get
                {
                    return ReadParam(key);
                }

                set
                {
                    WriteParam(key, value);
                }
            }

            public string ReadParam(string Key)
            {
                try
                {
                    string res = null;
                    using (SQLiteConnection con = db.GetConnection())
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = String.Format(@"SELECT val FROM settings where key='{0}';", Key);
                        using (SQLiteDataReader r = cmd.ExecuteReader())
                        {
                            if (r.HasRows)
                            {
                                r.Read();
                                res = r["val"].ToString();
                            }
                            r.Close();
                        }
                    }
                    return res;
                }
                catch (Exception) { return null; }
            }

            public int ReadParam(string Key, int defValue)
            {
                string resStr = ReadParam(Key);
                int resInt;
                if (String.IsNullOrWhiteSpace(resStr) || !int.TryParse(resStr, out resInt)) return defValue;
                return resInt;
            }

            //Запись параметра в таблицу
            public void WriteParam(string Key, string Val)
            {
                try
                {
                    using (SQLiteConnection con = db.GetConnection())
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        cmd.CommandText = String.Format(@"INSERT OR REPLACE into settings (key, val) VALUES ('{0}', '{1}');", Key, Val ?? string.Empty);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }
            }
        }

        private SettingsLst settings = null;
        public SettingsLst Settings
        {
            get
            {
                if (settings == null) settings = new SettingsLst(this);
                return settings;
            }
        }

        #endregion

        #region [Работа с данными БД]
        public SQLiteConnection GetConnection()
        {
            SQLiteConnection connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        // Выполнение произвольного SQL
        public void RunCustomNonQuery(SqlExpression expression)
        {
            RunCommand((cmd) =>
            {
                cmd.CommandText = expression.SQLtext;
                if (expression.Parameters != null && expression.Parameters.Length > 0)
                    cmd.Parameters.AddRange(expression.Parameters);
                cmd.ExecuteNonQuery();
            });
        }

        private void RunCommand(Action<SQLiteCommand> action)
        {
            using (SQLiteConnection con = GetConnection())
            using (SQLiteCommand cmd = con.CreateCommand())
            {
                action(cmd);
            }
        }
        #endregion

        #region [Общие процедуры]
        protected string GetSciptFromEmbeddedRes(string resName, Assembly assembly)
        {
           // Assembly As = Assembly.GetAssembly(type);//GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
            // TODO: Освобождение занятых ресурсов

        }
        #endregion
    }
}
