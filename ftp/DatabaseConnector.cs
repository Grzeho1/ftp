using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics.Metrics;
using System.Data.Common;


namespace ftp
{
    public class DatabaseConnector
    {

        private readonly string _connectionString;
        private SqlConnection _connection;

            public DatabaseConnector(string sqlServerName, string sqlPort, string sqlDatabaseName, string? sqlUser = null, string? sqlPassword = null)
    {
        if (string.IsNullOrEmpty(sqlUser) || string.IsNullOrEmpty(sqlPassword))
        {
            // použije se Windows Auth (není user a heslo)
            _connectionString = $"Data Source={sqlServerName},{sqlPort};Initial Catalog={sqlDatabaseName};Integrated Security=True;";
        }
        else
        {
            //  použije se SQL Auth
            _connectionString = $"Data Source={sqlServerName},{sqlPort};Initial Catalog={sqlDatabaseName};User ID={sqlUser};Password={sqlPassword};";
        }

        _connection = new SqlConnection(_connectionString);
    }
        public DatabaseConnector(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(_connectionString);
        }

        public async void OpenConnection()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
              await  Logger.Instance.WriteLineAsync("Připojeno k SQL");
            }
        }
        public async void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
            {
                try
                {
                    _connection.Close();
                    await Logger.Instance.WriteLineAsync("Odpojeno od SQL");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("Error connecting to the database: " + ex.Message);
                    Console.WriteLine("Detailed error info: " + ex.ToString());
                }
            }
        }


        public async Task<DataTable> ExecProcedure(string procedureName, params SqlParameter[] parameters)
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                try
                {
                   
                    using (SqlCommand command = new SqlCommand(procedureName, conn))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(parameters.Where(p => p != null).ToArray());

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {

                            adapter.Fill(dataTable);
                        }

                    }
                }
                catch (Exception ex)
                {
                    {
                        Console.WriteLine(ex.ToString());
                        await Logger.Instance.WriteLineAsync(ex.ToString());
                    }


                }
            }
            return dataTable;

        }

        //public async Task<byte[]> GetFileFromDtb(int id)
        //{
        //    byte[]? fileData = null;

        //    using (SqlConnection conn = new SqlConnection(_connectionString))
        //    {
        //        conn.Open();

        //        using (SqlCommand command = new SqlCommand("SELECT Dokument FROM dbo.Table_1 WHERE ID = @ID", conn))
        //        {
        //            command.Parameters.AddWithValue("@ID", id);
                   
        //            using (SqlDataReader reader = await command.ExecuteReaderAsync())
        //            {
        //                if (await reader.ReadAsync())
        //                {
        //                    fileData = reader["Dokument"] as byte[];
        //                }
        //            }
        //        }
        //    }

        //    return fileData;
        //}

    }
}
