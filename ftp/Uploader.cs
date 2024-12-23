﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using FluentFTP; 
using System.Threading.Tasks;
using FluentFTP.Exceptions;
using ftp;
using System.ComponentModel.Design;
using FluentFTP.Helpers;

public class Uploader
{
    private readonly DatabaseConnector _dbConnector;
    private readonly FtpClient _ftpClient;

    public Uploader(DatabaseConnector dbConnector, string ftpServer, string ftpUsername, string ftpPassword)
    {
        _dbConnector = dbConnector;
        _ftpClient = new FtpClient(ftpServer, ftpUsername, ftpPassword);
    }

    public async Task UploadToFTP(DataTable dataTable, string source,string remotePath) // 0 = Složka na disku, 1 = Databáze 
    {
        try
        {
         
            // Připojení k FTP
            _ftpClient.Connect();
            if (!_ftpClient.IsConnected)
            {
               
              await  Logger.Instance.WriteLineAsync("Nepodařilo se připojit k FTP serveru.");
                return;
            }

            await Logger.Instance.WriteLineAsync("Připojeno k FTP serveru.");


            foreach (DataRow row in dataTable.Rows)
            {
                int id = (int)row["ID"];
                string? cesta = (string)row["JmenoACesta"];
                string fileName;
                string ftpRemotePath;
                
                if (source == "0" )

                {
                    string localPath = cesta ?? string.Empty;

                    if (!string.IsNullOrEmpty(localPath))
                    {


                        fileName = Path.GetFileName(localPath);
                        ftpRemotePath = $"{remotePath}/{fileName}";
                        string directoryFolder = Path.GetDirectoryName(localPath) ?? string.Empty;
                      
                        
                        try
                        {
                           
                            // Upload souboru na FTP
                            FtpStatus status = _ftpClient.UploadFile(localPath, ftpRemotePath, FtpRemoteExists.Overwrite, false, FtpVerify.Retry | FtpVerify.Throw);
                            
                            if (status == FtpStatus.Success)
                            {

                                    //Přesunutí souboru do složky "Odeslané"
                                    string parentDir = Directory.GetParent(directoryFolder).FullName;
                                    string targetDir = Path.Combine(parentDir, "Odeslané");
                                    string targetPathFile = Path.Combine(targetDir, fileName);
                                

                                
                                if (!Directory.Exists(targetDir))
                                {
                                    Directory.CreateDirectory(targetDir);

                                }

                                File.Move(localPath, targetPathFile);
                                await Logger.Instance.WriteLineAsync($"Soubor {fileName} byl nahrán a přesunut do {targetPathFile}.");
                                await UploadStatusHelios(id, 1, null, targetPathFile); // Stav OK

                            }
                            else
                            {
                               
                                await Logger.Instance.WriteLineAsync($"Soubor {fileName} se nepodařilo nahrát.");
                                await UploadStatusHelios(id, 0, "Nepodařilo se nahrát soubor", null); // Stav NOK
                            }
                        }
                        catch (FtpException ftpEx)
                        {
                           
                            await Logger.Instance.WriteLineAsync($"Chyba FTP při nahrávání {fileName}: {ftpEx}");
                            await UploadStatusHelios(id, 0, ftpEx.Message, null);
                        }
                        catch (IOException ioEx)
                        {
                            
                            await Logger.Instance.WriteLineAsync($"Chyba při nahrávání {fileName}: {ioEx}");
                            await UploadStatusHelios(id, 0, ioEx.Message, null);
                        }
                        catch (Exception ex)
                        {
                            
                            await Logger.Instance.WriteLineAsync($"chyba při nahrávání {fileName}: {ex}");
                            await UploadStatusHelios(id, 0, ex.Message, null);
                        }
                    }
                    else
                    {
                        
                        await Logger.Instance.WriteLineAsync($"Soubor {id} nebyl nalezen nebo je neplatná cesta.");
                        await UploadStatusHelios(id, 0, "Soubor nebyl nalezen", null);
                    }
                }

                else if (source == "1")
                {
                    try
                    {
                        // fileData = await _dbConnector.GetFileFromDatabase(id); // Načte data z databáze
                        byte[]? documentFile = row["Dokument"] as byte[];
                        fileName = $"FaVyd_{id}.pdf"; // Pojmenuje soubor
                        ftpRemotePath = $"{remotePath}/{fileName}";

                        if(documentFile == null || documentFile.Length == 0)
                        {
                            await Logger.Instance.WriteLineAsync($"{id} Pole Dokument je prázdné.");
                            continue;
                        }

                        // using (var memoryStream = new MemoryStream(fileData))  --- Jen malé soubory ..Není potřeba posílat jako stream

                        // Nahrání souboru na FTP z paměti
                        FtpStatus status = _ftpClient.UploadBytes(documentFile, ftpRemotePath, FtpRemoteExists.Overwrite);

                            if (status == FtpStatus.Success)
                            {
                               
                                await Logger.Instance.WriteLineAsync($"Soubor {fileName} byl nahrán z databáze.");
                                await UploadStatusHelios(id, 1, null, null); // Stav OK
                            }
                            else
                            {
                               
                                await Logger.Instance.WriteLineAsync($"Soubor {fileName} se nepodařilo nahrát.");
                                await UploadStatusHelios(id, 0, "Nepodařilo se nahrát soubor", null); // Stav NOK
                            }
                       
                    }
                    catch (Exception ex)
                    {
                        
                        await Logger.Instance.WriteLineAsync($"Chyba při nahrávání souboru s ID {id} : {ex}");
                        await UploadStatusHelios(id, 0, ex.Message, null);
                    }
                }
                else
                {
                   
                    await Logger.Instance.WriteLineAsync("Neplatný zdroj souboru (lze zadat pouze 0 - soubor || 1 - databáze)");
                }
            }
        }
           

        finally
        {
            // Odpojení od FTP serveru
            if (_ftpClient.IsConnected)
            {
                _ftpClient.Disconnect();
               
                await Logger.Instance.WriteLineAsync("Odpojeno od FTP.");
            }
        }
    }

    private async Task UploadStatusHelios(int id, int status, string? errorMessage, string? newPath)
    {
        // Parametry
        SqlParameter idParam = new SqlParameter("@ID", SqlDbType.Int) { Value = id };
        SqlParameter statusParam = new SqlParameter("@Status", SqlDbType.Bit) { Value = status };
        SqlParameter errorMsg = new SqlParameter("@ErrMsg", SqlDbType.NVarChar, 255) { Value = errorMessage ?? null };
        SqlParameter NewPath = new SqlParameter("@NewPath", SqlDbType.NVarChar, 255) { Value = newPath ?? null };
        // zápis stavu jak dopadl upload

            await _dbConnector.ExecProcedure("dbo.hpx_COAL_ZapisStavOdeslaniFaV", idParam, statusParam, errorMsg, NewPath);
       

        await Logger.Instance.WriteLineAsync($"Stav pro ID: {id} nastaven na {(status == 1 ? "OK" : "NOK")}");
    }
}
