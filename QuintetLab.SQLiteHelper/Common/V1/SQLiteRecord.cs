using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace QuintetLab.SQLiteHelperCore.Common.V1
{
    public class SQLiteRecord
    {
        private  readonly string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ssZ";
        private  readonly string DATETIMEOFFSET_FORMAT = "dd.MM.yyyy H:mm:ss zzz";


        internal class FieldCreationBuilder
        {
            public string FieldName { get; set; }
            public string DataType { get; set; }
            public SQLiteFieldAttribute Attribute { get; set; }

            public string GetSQL(bool includePrimaryKeyDef)
            {
                string res = $"{FieldName} {DataType}";
                if (Attribute.IsPrimaryKey && includePrimaryKeyDef) res += " PRIMARY KEY";
                if (Attribute.IsAutoIncrement) res += " AUTOINCREMENT";
                if (Attribute.IsNotNull) res += " NOT NULL";
                if (Attribute.IsUnique) res += " UNIQUE";
                if (!string.IsNullOrEmpty(Attribute.Default)) res += $" DEFAULT {Attribute.Default}";
                if (!string.IsNullOrWhiteSpace(Attribute.Comment)) res += $"\t /* {Attribute.Comment} */";
                return res;
            }
        }

    

        private Dictionary<Type, Dictionary<string, PropertyInfo>> dbFields;
        private readonly object syncRoot = new Object();
        public Dictionary<string, PropertyInfo> GetDbFields(Type t)
        {
            if (dbFields == null || !dbFields.ContainsKey(t))
            {
                lock (syncRoot)
                {
                    if (dbFields == null) dbFields = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
                    if (!dbFields.ContainsKey(t))
                    {
                        dbFields[t] = new Dictionary<string, PropertyInfo>();
                        var props = t.GetProperties();
                        foreach (var prop in props)
                        {
                            var attributes = prop.GetCustomAttributes(false);
                            var columnMapping = attributes.FirstOrDefault(a => a is SQLiteFieldAttribute);
                            if (columnMapping != null)
                            {
                                var attr = columnMapping as SQLiteFieldAttribute;
                                dbFields[t][string.IsNullOrEmpty(attr?.FieldName) ? prop.Name : attr.FieldName] = prop;
                            }
                        }
                    }
                }
            }
            return dbFields[t];
        }


        internal virtual SqlExpression GetSQLForUpsert()
        {
            var props = GetType().GetProperties();
            List<Tuple<string, SQLiteParameter>> fieldsToInsertSQL = new List<Tuple<string, SQLiteParameter>>();
            List<Tuple<string, SQLiteParameter>> fieldsToSetSQL = new List<Tuple<string, SQLiteParameter>>();
            List<Tuple<string, SQLiteParameter>> fieldsToWhereSQL = new List<Tuple<string, SQLiteParameter>>();

            foreach (var prop in props)
            {
                var attributes = prop.GetCustomAttributes(false);
                var attr = attributes.FirstOrDefault(a => a is SQLiteFieldAttribute) as SQLiteFieldAttribute;

                if (attr != null)
                {

                    if ( !attr.IsAutoIncrement)
                    {
                        fieldsToInsertSQL.Add(new Tuple<string, SQLiteParameter>(
                            string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName,
                            GetPropValueAsQueryParam(prop)));
                    }

                    if (attr.IsPrimaryKey)
                    {
                        fieldsToWhereSQL.Add(new Tuple<string, SQLiteParameter>(string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName, GetPropValueAsQueryParam(prop)));
                    }
                    else
                    {
                        fieldsToSetSQL.Add(new Tuple<string, SQLiteParameter>(string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName, GetPropValueAsQueryParam(prop)));
                    }

                }
            }

            return new SqlExpression
            {
                SQLtext =
                    $"({string.Join(", ", fieldsToInsertSQL.Select(x => x.Item1))}) VALUES ({string.Join(", ", fieldsToInsertSQL.Select(x => $"@{x.Item2.ParameterName}"))}) "+
                    $"ON CONFLICT  ({string.Join(" AND ", fieldsToWhereSQL.Select(x => $"{x.Item1}"))}) DO UPDATE " +
                    $"SET {string.Join(", ", fieldsToSetSQL.Select(x => $"{x.Item1} = @{x.Item2.ParameterName}"))} "
                    + $"WHERE {string.Join(" AND ", fieldsToWhereSQL.Select(x => $"{x.Item1} = @{x.Item2.ParameterName}"))}",


                Parameters = fieldsToSetSQL.Select(x => x.Item2).Concat(fieldsToWhereSQL.Select(x => x.Item2)).ToArray(),
            };


        }

        internal virtual SqlExpression GetSQLForInsert()
        {
            var props = GetType().GetProperties();

            List<Tuple<string, SQLiteParameter>> fieldsToInsertSQL = new List<Tuple<string, SQLiteParameter>>();
            foreach (var prop in props)
            {
                var attributes = prop.GetCustomAttributes(false);
                var attr = attributes.FirstOrDefault(a => a is SQLiteFieldAttribute) as SQLiteFieldAttribute;
                if (attr != null && !attr.IsAutoIncrement)
                {
                    fieldsToInsertSQL.Add(new Tuple<string, SQLiteParameter>(string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName, GetPropValueAsQueryParam(prop)));
                }
            }
            return new SqlExpression
            {
                SQLtext = $"({string.Join(", ", fieldsToInsertSQL.Select(x => x.Item1))}) VALUES ({string.Join(", ", fieldsToInsertSQL.Select(x => $"@{x.Item2.ParameterName}"))})",
                Parameters = fieldsToInsertSQL.Select(x => x.Item2).ToArray()
            };
        }

        internal virtual SqlExpression GetSQLForUpdate()
        {
            var props = GetType().GetProperties();
            List<Tuple<string, SQLiteParameter>> fieldsToSetSQL = new List<Tuple<string, SQLiteParameter>>();
            List<Tuple<string, SQLiteParameter>> fieldsToWhereSQL = new List<Tuple<string, SQLiteParameter>>();
            foreach (var prop in props)
            {
                var attributes = prop.GetCustomAttributes(false);
                var attr = attributes.FirstOrDefault(a => a is SQLiteFieldAttribute) as SQLiteFieldAttribute;
                if (attr != null)
                {
                    if (attr.IsPrimaryKey)
                    {
                        fieldsToWhereSQL.Add(new Tuple<string, SQLiteParameter>(string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName, GetPropValueAsQueryParam(prop)));
                    }
                    else
                    {
                        fieldsToSetSQL.Add(new Tuple<string, SQLiteParameter>(string.IsNullOrEmpty(attr.FieldName) ? prop.Name : attr.FieldName, GetPropValueAsQueryParam(prop)));
                    }
                }
            }
            return new SqlExpression
            {
                SQLtext = $"SET {string.Join(", ", fieldsToSetSQL.Select(x => $"{x.Item1} = @{x.Item2.ParameterName}" ))} "
                    + $"WHERE {string.Join(" AND ", fieldsToWhereSQL.Select(x => $"{x.Item1} = @{x.Item2.ParameterName}"))}",
                Parameters = fieldsToSetSQL.Select(x => x.Item2).Concat(fieldsToWhereSQL.Select(x => x.Item2)).ToArray()
            };
        }

        internal SQLiteParameter GetPropValueAsQueryParam(PropertyInfo prop)
        {
            var t = prop.PropertyType;

            if (t == typeof(DateTime))
            {
                return new SQLiteParameter(prop.Name, ((DateTime)prop.GetValue(this)).ToUniversalTime().ToString(DATETIME_FORMAT));
            }
            if (t == typeof(DateTime?))
            {
                DateTime? val = (DateTime?)prop.GetValue(this);
                return new SQLiteParameter(prop.Name, val?.ToUniversalTime().ToString(DATETIME_FORMAT) ?? null);
            }

            if (t == typeof(DateTimeOffset))
            {
                return new SQLiteParameter(prop.Name, ((DateTimeOffset)prop.GetValue(this)).ToLocalTime().ToString(DATETIMEOFFSET_FORMAT));
            }

            if (t == typeof(DateTimeOffset?))
            {
                DateTimeOffset? val = (DateTimeOffset?)prop.GetValue(this);
                return new SQLiteParameter(prop.Name, val?.ToLocalTime().ToString(DATETIMEOFFSET_FORMAT) ?? null);
            }



            return new SQLiteParameter(prop.Name, prop.GetValue(this));
        }

        internal virtual void LoadFromTableRow(SQLiteDataReader r)
        {
            var fields = GetDbFields(GetType());
            for (int i = 0; i< r.FieldCount; i++)
            {
                string fieldName = r.GetName(i);
                if (fields.ContainsKey(fieldName))
                {
                    var prop = fields[fieldName];
                    var t = prop.PropertyType;
                    if (r.IsDBNull(i)) prop.SetValue(this, null);
                    else if (t == typeof(int) || t == typeof(int?)) prop.SetValue(this, r.GetInt32(i));
                    else if (t == typeof(bool) || t == typeof(bool?)) prop.SetValue(this, r.GetBoolean(i));
                    else if (t == typeof(string)) prop.SetValue(this, r.GetString(i));
                    else if (t == typeof(double) || t == typeof(double?)) prop.SetValue(this, r.GetDouble(i));
                    else if (t == typeof(float) || t == typeof(float?)) prop.SetValue(this, r.GetFloat(i));
                    else if (t == typeof(DateTime) || t == typeof(DateTime?)) prop.SetValue(this, DateTime.ParseExact(r.GetString(i), DATETIME_FORMAT, CultureInfo.InvariantCulture).ToUniversalTime());
                    else if (t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?)) prop.SetValue(this, DateTimeOffset.ParseExact(r.GetString(i), DATETIMEOFFSET_FORMAT, CultureInfo.InvariantCulture).ToUniversalTime());
                    else if (t == typeof(decimal) || t == typeof(decimal?)) prop.SetValue(this, r.GetDecimal(i));
                    else if (t == typeof(long) || t == typeof(long?)) prop.SetValue(this, r.GetInt64(i));
                }
            }
        }


    }
}
