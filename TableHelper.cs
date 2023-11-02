using System;
using System.Collections.Generic;
using System.Data;

namespace ADXdbApp.Helpers
{
    public static class TableHelper
    {
        public static List<Dictionary<string, string>> ConvertDataTableToList(DataTable table)
        {
            var result = new List<Dictionary<string, string>>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, string>();
                foreach (DataColumn column in table.Columns)
                {
                    dict[column.ColumnName] = Convert.ToString(row[column]);
                }
                result.Add(dict);
            }
            return result;
        }
    }
}
