using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using FluentFTP; // Knihovna FluentFTP pro práci s FTP
using System.Threading.Tasks;
using FluentFTP.Exceptions;
using ftp;

public class Uploader
{
    private readonly DatabaseConnector _dbConnector;
    private readonly FtpClient _ftpClient;

    public Uploader(DatabaseConnector dbConnector, string ftpServer, string ftpUsername, string ftpPassword)
    {
        _dbConnector = dbConnector;
        _ftpClient = new FtpClient(ftpServer, ftpUsername, ftpPassword);
    }

    public async Task UploadToFTP(DataTable dataTable)
    {
        try
        {
            // Připojení k FTP
            _ftpClient.Connect();
            if (!_ftpClient.IsConnected)
            {
                Console.WriteLine("Nepodařilo se připojit k FTP serveru.");
              await  Logger.Instance.WriteLineAsync("Nepodařilo se připojit k FTP serveru.");
                return;
            }

            Console.WriteLine("Připojeno k FTP serveru.");
            await Logger.Instance.WriteLineAsync("Připojeno k FTP serveru.");
            foreach (DataRow row in dataTable.Rows)
            {
                int id = (int)row["ID"];
                string localPath = row["JmenoACesta"] as string;

                if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
                {
                    string fileName = Path.GetFileName(localPath);
                    string remotePath = $"/{fileName}";

                    try
                    {
                        // Upload souboru na FTP
                        FtpStatus status = _ftpClient.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, false, FtpVerify.Retry | FtpVerify.Throw);

                        if (status == FtpStatus.Success)
                        {
                            Console.WriteLine($"Soubor {fileName} byl úspěšně nahrán.");
                            await Logger.Instance.WriteLineAsync($"Soubor {fileName} byl úspěšně nahrán.");
                            await UploadStatusHelios(id, 1, null); // Stav OK
                        }
                        else
                        {
                            Console.WriteLine($"Soubor {fileName} se nepodařilo nahrát.");
                            await Logger.Instance.WriteLineAsync($"Soubor {fileName} se nepodařilo nahrát.");
                            await UploadStatusHelios(id, 0, "Nepodařilo se nahrát soubor"); // Stav NOK
                        }
                    }
                    catch (FtpException ftpEx)
                    {
                        Console.WriteLine($"Chyba FTP při nahrávání {fileName}: {ftpEx}");
                        await Logger.Instance.WriteLineAsync($"Chyba FTP při nahrávání {fileName}: {ftpEx}");
                        await UploadStatusHelios(id, 0, ftpEx.Message);
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"Chyba IO při nahrávání {fileName}: {ioEx}");
                        await Logger.Instance.WriteLineAsync($"Chyba IO při nahrávání {fileName}: {ioEx}");
                        await UploadStatusHelios(id, 0, ioEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Obecná chyba při nahrávání {fileName}: {ex}");
                        await Logger.Instance.WriteLineAsync($"Obecná chyba při nahrávání {fileName}: {ex}");
                        await UploadStatusHelios(id, 0, ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"Soubor s ID {id} nebyl nalezen na disku nebo je cesta neplatná.");
                    await Logger.Instance.WriteLineAsync($"Soubor s ID {id} nebyl nalezen na disku nebo je cesta neplatná.");
                    await UploadStatusHelios(id, 0, "Soubor nebyl nalezen na disku");
                }
            }
        }
        finally
        {
            // Odpojení od FTP serveru
            if (_ftpClient.IsConnected)
            {
                _ftpClient.Disconnect();
                Console.WriteLine("Odpojeno od FTP serveru.");
            }
        }
    }

    private async Task UploadStatusHelios(int id, int status, string errorMessage)
    {
        // Parametry pro zápis stavu
        SqlParameter idParam = new SqlParameter("@ID", SqlDbType.Int) { Value = id };
        SqlParameter statusParam = new SqlParameter("@Status", SqlDbType.Bit) { Value = status };
        SqlParameter errorMsgParam = new SqlParameter("@ErrMsg", SqlDbType.NVarChar, 255) { Value = (object)errorMessage ?? DBNull.Value };

        // zápis stavu
        await Task.Run(()=>
        {
            _dbConnector.ExecProcedure("dbo.hpx_COAL_ZapisStavOdeslaniFaV", idParam,statusParam,errorMsgParam);
        });

        await Logger.Instance.WriteLineAsync($"Stav pro ID: {id} nastaven na {(status == 1 ? "OK" : "NOK")}");
    }
}
