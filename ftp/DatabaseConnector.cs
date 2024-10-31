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

        public DatabaseConnector(string serverName, string port, string databaseName, string userId, string password)
       {
           _connectionString = _connectionString = $"Data Source={serverName},{port};Network Library=DBMSSOCN;Initial Catalog={databaseName};User ID={userId};Password={password};";
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
                _connection.Close();
               await Logger.Instance.WriteLineAsync("Odpojeno od SQL");
            }
        }


        public DataTable ExecProcedure(string procedureName, params SqlParameter[] parameters)
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
                        Task task = Logger.Instance.WriteLineAsync(ex.ToString());
                    }


                }

            }
            return dataTable;



        }

    }
}
