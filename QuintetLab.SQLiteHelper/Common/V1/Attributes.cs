using System;

namespace QuintetLab.SQLiteHelperCore.Common.V1
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SQLiteFieldAttribute : Attribute
    {
        public string FieldName;
        public string FieldType;
        public bool IsPrimaryKey;
        public bool IsNotNull;
        public bool IsAutoIncrement;
        public bool IsUnique;
        public string Default;
        public string Comment;
        public SQLiteFieldAttribute(
            string FieldName = null, 
            string FieldType = null,
            bool IsPrimaryKey = false, 
            bool IsNotNull = false, 
            bool IsAutoIncrement = false,
            bool IsUnique = false, 
            string Default = null,
            string Comment = null)
        {
            this.FieldName = FieldName;
            this.FieldType = FieldType;
            this.IsPrimaryKey = IsPrimaryKey;
            this.IsNotNull = IsNotNull;
            this.IsAutoIncrement = IsAutoIncrement;
            this.IsUnique = IsUnique;
            this.Default = Default;
            this.Comment = Comment;
        }
    }
}
