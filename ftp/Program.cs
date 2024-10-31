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


// 1) je potřeba přidat něco jako TargetFolder do configu, protože se cílová složka na FTP může lišit.
// 2) 


var config = ConfigLoader.LoadConfig("Config.txt");
    string ftpHost = config["ftpHost"];
    string ftpUser = config["ftpUser"];
    string ftpPassword = config["ftpPassword"];
    string folderPath = config["folderPath"];
    string sqlPassword = config["sqlPassword"];
    string logFolderPath = "Logs";


    string sqlServerName = config["sqlServer"]; ;
    string sqlDatabaseName = config["sqlDatabase"]; 
    string sqlUser = config["sqlUser"]; ;
    string sqlPort = config["sqlPort"]; 



    // Tady jde buď předat plný connection string a nebo to parametrizovat v txt.
    
    //var dbConnector = new DatabaseConnector(sqlServerName, sqlPort, sqlDatabaseName, sqlUser, sqlPassword);
    var dbConnector = new DatabaseConnector("data source=DESKTOP-FUQ15OI\\SQLEXPRESS;initial catalog=testovaci;user id=tomas;password=123456");
    // Otevření spojení s DB
    dbConnector.OpenConnection();
    // Spuštění naší procedury
    DataTable resultTable = dbConnector.ExecProcedure("dbo.hpx_COAL_OdesliFaV", new SqlParameter("@ImportChybovych", 1));

    var ftpUploader = new Uploader(dbConnector, ftpHost, ftpUser, ftpPassword);

    await ftpUploader.UploadToFTP(resultTable);
    
    dbConnector.CloseConnection();

    await Logger.Instance.WriteLineAsync("-------- Konec ---------\n");


//Environment.Exit(success ? 0 : 1);

