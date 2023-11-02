using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ADXdbApp.Models
{
    public class QueryResult
    {
        public string TableName { get; set; }
        public DataTable Data { get; set; }
    }

    public class TableViewModel
    {
        public List<string> TableNames { get; set; }
        public Dictionary<string, DataTable> TableData { get; set; }
      //  public IEnumerable<QueryResult> PagedData { get; set; }
        //public int CurrentPage { get; set; }
        //public int TotalPages { get; set; }
         

    }

    public class DataModel1
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // Add other properties as needed
    }
    public class DataModel
    {
        public string TableName { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
        public object Name { get; internal set; }
    }
    public class CsvHeader
    {
        public List<string> Columns { get; set; }
    }

}
