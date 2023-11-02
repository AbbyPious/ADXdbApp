using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Data;
using ADXdbApp.Models;
using Kusto.Data.Net.Client;
using Kusto.Data.Common;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using Kusto.Cloud.Platform.Utils;

namespace ADXdbApp.Services
{
    public class ADXService
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly string databaseName;
        private object IngestionMappingType;
        private Stream fileStream;

        public ADXService(IConfiguration configuration)
        {
            _configuration = configuration;
            this.connectionString = _configuration.GetConnectionString("ADXConnection");
             this.databaseName = "adx-database-sample";
           // this.databaseName = "SampleLogs";
        }
         

        public List<QueryResult> GetTableData()
        {
            string connectionString = _configuration.GetConnectionString("ADXConnection");
            
            var results = new List<QueryResult>();

            using (var client = KustoClientFactory.CreateCslQueryProvider(connectionString))
            {
                var adminClient = KustoClientFactory.CreateCslAdminProvider(connectionString);
               // var databaseName = "adx-database-sample";
                var query = $".show tables";
                //var tab= client.ExecuteQuery( databaseName, query, new ClientRequestProperties());
                var tables = client.ExecuteQuery(this.databaseName, query, new ClientRequestProperties()); //client.ExecuteQuery(query); //

                while (tables.Read())
                {
                    string tableName = tables.GetString(0);
                    query = $"{tableName}";
                    var tableData = client.ExecuteQuery(this.databaseName, query, new ClientRequestProperties());
                    results.Add(new QueryResult
                    {
                        TableName = tableName,
                        Data = ConvertToDataTable(tableData)
                    });
                }
            }

            
            return results;
        }

        public List<QueryResult> GetSortedAndFilteredData(string searchTerm, string table, string sortSettings )
        {
            string connectionString = _configuration.GetConnectionString("ADXConnection");

            var results = new List<QueryResult>();

            using (var client = KustoClientFactory.CreateCslQueryProvider(connectionString))
            {
                var adminClient = KustoClientFactory.CreateCslAdminProvider(connectionString);
                // var databaseName = "adx-database-sample";
                var databaseName = "Samples";
                 var query = "";
                if (sortSettings == null && searchTerm == null)
                {
                    query = $"{table}";
                }
                if (sortSettings== null && searchTerm != null)
                {
                    //query = $"{table} | where * has '{searchTerm}'";
                    query = $"{table} | where * matches regex '(?i){searchTerm}'";
                    //matches regex
                }
                if (searchTerm==null && sortSettings != null)
                {
                    query = $"{table} | sort by {sortSettings}";
                }
                if (searchTerm != null && sortSettings != null)
                {
                    query = $"{table} | where * matches regex '(?i){searchTerm}' | sort by {sortSettings}";
                }
                
                              


                //var tab= client.ExecuteQuery( databaseName, query, new ClientRequestProperties());
                var tables = client.ExecuteQuery(this.databaseName, query, new ClientRequestProperties()); //client.ExecuteQuery(query); //

                while (tables.Read())
                {
                    string tableName = table;//tables.GetString(0);
                    //query = $"{table} | where * has '{searchTerm}' | sort by {sortSettings}"; // $"{table} | where * has '{searchTerm}';";  //$"{tableName}";
                    var tableData = client.ExecuteQuery(this.databaseName, query, new ClientRequestProperties());
                    results.Add(new QueryResult
                    {
                        TableName = table,
                        Data = ConvertToDataTable(tableData)
                    });
                }
            }


            return results;
        }



        private DataTable ConvertToDataTable(IDataReader reader)
        {
            DataTable table = new DataTable();

            if (reader != null)
            {
                // Create columns in the DataTable based on the reader's schema
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                }

                // Read the data from the IDataReader and populate the DataTable
                while (reader.Read())
                {
                    DataRow row = table.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        // Read each column and populate the DataRow
                        row[i] = reader[i];
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        public bool InsertData(string tableName, IFormFile file)
        {

            try
            {

                
                using (var client = KustoClientFactory.CreateCslAdminProvider(connectionString))
                using (var stream = file.OpenReadStream())                  
                using (var reader = new StreamReader(stream))
                {
                    // Check if the table already exists. If not, create it.
                    if (!TableExists(client, tableName))
                    {
                        // var schema = BuildTableSchemaFromFormFile(file);
                        //CreateNewTable(client, tableName, schema);
                        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            Delimiter = "\t" // Set the delimiter to tab character
                        };

                        // Read the header row manually
                        string headerRow = reader.ReadLine();
                        string dataRow = reader.ReadToEnd();
                       
                        // Split the header row into columns
                        string[] header = headerRow.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] data = dataRow.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        string tabledata = $"{string.Join(", ", data.Select(column => $"{column.Trim()}"))}";
                        // Construct the table schema based on the header columns
                        string tableSchema = $"({string.Join(", ", header.Select(column => $"{column.Trim()}:string"))})";
                        var createTableCommand = $".create table {tableName} {tableSchema}";
                        client.ExecuteControlCommand(this.databaseName, createTableCommand);
                        string query1 = $".ingest inline into table {tableName} <| {tabledata}";
                        client.ExecuteControlCommand(this.databaseName, query1);
                    }
                    else
                    {
                        string fileContent = reader.ReadToEnd();
                        string[] data1 = fileContent.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        string tabledata1 = $"{string.Join(", ", data1.Select(column => $"{column.Trim()}"))}";
                        // Define the query to insert data into the specified table
                        string query = $".ingest inline into table {tableName} <| {tabledata1}";

                        // Execute the query
                        client.ExecuteControlCommand(this.databaseName, query);
                    }
                    

                    return true;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions or errors during data insertion
                // Log the error for debugging purposes
                return false;
            }
        }

        private bool TableExists(ICslAdminProvider client,  string tableName)
        {
            try
            {
                var result = client.ExecuteControlCommand(this.databaseName, $".show table {tableName} details");
                return result.Read();
            }
            catch
            {
                return false;
            }
        }

        private void CreateNewTable(ICslAdminProvider client,  string tableName , string schema)
        {
            var tableSchema = schema;
            var createTableCommand = $".create table {tableName} {tableSchema}";
            client.ExecuteControlCommand(this.databaseName, createTableCommand);

        }

        public string BuildTableSchemaFromFormFile(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = "\t" // Set the delimiter to tab character
                };

                // Read the header row manually
                string headerRow = reader.ReadLine();
                string dataRow = reader.ReadToEnd();
                if (string.IsNullOrEmpty(headerRow))
                {
                    // Handle the case where there's no header row
                    return "Table schema could not be determined.";
                }

                // Split the header row into columns
                string[] header = headerRow.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string[] data= dataRow.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string tabledata = $"({string.Join(", ", data.Select(column => $"{column.Trim()}"))})";
                // Construct the table schema based on the header columns
                string tableSchema = $"({string.Join(", ", header.Select(column => $"{column.Trim()}:string"))})";
                return tableSchema ;
            }
        }

        //public string BuildTableSchemaFromFormFile(IFormFile file)
        //{
        //    using (var reader = new StreamReader(file.OpenReadStream()))
        //    {
        //        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        //        {

        //            csv.Read();
        //            //string[] header = csv.HeaderRecord;                 
        //            string headerRow = reader.ReadLine();


        //            // Split the header row into columns
        //            string[] header = headerRow.Split('\t');

        //            // Construct the table schema based on the header columns
        //            string tableSchema = $"({string.Join(", ", header.Select(column => $"{column}:string"))})";
        //            return tableSchema;
        //        }
        //    }
        //}
        //--------
    }
  

}




