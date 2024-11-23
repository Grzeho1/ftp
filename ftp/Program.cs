using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using FluentFTP;
using ftp;
using static System.Runtime.InteropServices.JavaScript.JSType;

    // FTP
    var config = ConfigLoader.LoadConfig("Config.ini");
    string ftpHost= config["ftpHost"];
    string ftpUser= config["ftpUser"];
    string ftpPassword= config["ftpPassword"];
    // SQL
    string sqlServerName= config["sqlServer"]; ;
    string sqlDatabaseName= config["sqlDatabase"]; 
    string? sqlUser= config["sqlUser"]; ;
    string? sqlPassword= config["sqlPassword"];
    string sqlPort= config["sqlPort"];

    // 0 = Ze souboru  1 = Z Databáze
    string source = config["source"]; 
    string ftpRemotePath = config["ftpRemotePath"];
    // 0 = NE 1 = ANO
    string importChybovych = config["importChybovych"];




   
    var dbConnector = new DatabaseConnector(sqlServerName, sqlPort, sqlDatabaseName, sqlUser, sqlPassword);
    //var dbConnector = new DatabaseConnector("data source=DESKTOP-FUQ15OI\\SQLEXPRESS;initial catalog=testovaci;user id=tomas;password=123456");

    dbConnector.OpenConnection();

    DataTable resultTable = await dbConnector.ExecProcedure("dbo.hpx_COAL_OdesliFaV", new SqlParameter("@ImportChybovych", importChybovych));
    var ftpUploader = new Uploader(dbConnector, ftpHost, ftpUser, ftpPassword);
    await ftpUploader.UploadToFTP(resultTable,source,ftpRemotePath);

    dbConnector.CloseConnection();

    await Logger.Instance.WriteLineAsync("---------- Konec ---------\n");



