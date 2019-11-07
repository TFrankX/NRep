using System.Data.SQLite;

namespace QuintetLab.SQLiteHelperCore.Common.V1
{
    public class SqlExpression
    {
        public string SQLtext { get; set; }
        public SQLiteParameter[] Parameters { get; set; }
    }
}
