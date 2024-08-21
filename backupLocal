using Microsoft.Data.Sqlite;
using Middleware.Controle_Acesso.List_Access_Control_Memory.Update;
using Middleware.Equipamentos.Equipamento.Classe;
using Middleware.Equipamentos.Monitor;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Middleware.Banco_Dados.Bkp
{
    public partial class C_BackupLocal
    {
        public event EventHandler<C_Monitor_Handler> monitor;
        public C_Monitor_Queue monitorQueue { get; set; }

        private readonly string diretorio = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                        @"\seurepo";

        private SqliteConnection sourceConnection;
        private SqliteConnection backupConnection;
        // private static Semaphore _databaseSemaphore = new Semaphore(1, 1); // Permite apenas um acesso por vez

        int total;
        int atual;

        #region backupLocal
        public async Task BackupLocal(CancellationToken cancellationToken)
        {
            try
            {
                var dir = new C_BackupLocal().diretorio;

                if (Directory.Exists(dir))
                {
                    // This path is a directory
                    await ProcessDirectory(dir, cancellationToken); // Passa o token para ProcessDirectory
                }
                else
                {
                    Send_Monitor_Limit("Backup", $@"{dir} não é válido.");
                    //return false;
                }
            }
            catch (Exception e)
            {
                Send_Monitor_Limit("Backup", $@"BackupLocal -  {e}."); ;
            }
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public async Task ProcessDirectory(string clientDirectory, CancellationToken cancellationToken)
        {
            try
            {
                // Verifique se o token foi cancelado antes de prosseguir
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Backup cancelado.");
                    return; // Retorna sem fazer mais nada
                }

                var backupDirectory = await Task.Run(() => VerifyBackupDirectory(clientDirectory, cancellationToken));

                // Verifique novamente se o token foi cancelado após verificar o diretório de backup
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Backup cancelado.");
                    return; // Retorna sem fazer mais nada
                }

                // Process the list of files found in the directory.
                string[] fileEntries = Directory.GetFiles(clientDirectory);

                // Processa cada arquivo
                foreach (string fileName in fileEntries)
                {
                    // Verifique o token antes de processar cada arquivo
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Backup cancelado.");
                        return; // Retorna sem fazer mais nada
                    }

                    await ProcessFile(fileName, backupDirectory, cancellationToken); // Passa o token para ProcessFile
                }

            }
            catch (Exception e)
            {
                Send_Monitor_Limit("Backup", $" 1 - {e.Message}");
            }
        }

        // Insert logic for processing found files here.
        private async Task ProcessFile(string path, string backupDirectory, CancellationToken cancellationToken)
        {
            //try
            //{

            // Verifique se o token foi cancelado antes de prosseguir
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Backup cancelado.");
                return; // Retorna sem fazer mais nada
            }

            try
            {

                // Aguarda a liberação do semáforo
                //_databaseSemaphore.WaitOne();

                Console.WriteLine("Processed file '{0}'.", path);

                var filename = Path.GetFileName(path);

                var sourceConnectionString = "Data Source=" + path;
                var backupConnectionString = "Data Source=" + backupDirectory + "\\" + filename;

                string password = C_BD_Globais.DB_PASSOWRD;

                var sourceConn = new SqliteConnectionStringBuilder(sourceConnectionString)
                {
                    Mode = SqliteOpenMode.ReadWriteCreate,

                    Password = password

                }.ToString();// Use this method to set the password , avoid sql Inject 

                using (backupConnection = new SqliteConnection(backupConnectionString))
                using (sourceConnection = new SqliteConnection(sourceConn))
                {
                    // Verifique o token antes de executar o comando
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Backup cancelado.");
                        return; // Retorna sem fazer mais nada
                    }

                    await sourceConnection.OpenAsync();

                    var query = $"PRAGMA key = '';" +
                                $"ATTACH DATABASE 'file:{filename}' AS encrypted KEY ''; " +
                                $"SELECT sqlcipher_export('encrypted');" +
                                $"DETACH DATABASE encrypted;";

                    using (var cmd = new SqliteCommand(query, sourceConnection))
                    {
                        // Verifique o token antes de executar o comando
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Backup cancelado.");
                            return; // Retorna sem fazer mais nada
                        }

                        var res = await cmd.ExecuteNonQueryAsync();
                    }


                    var inMemConnectionString = new SqliteConnectionStringBuilder($"Data Source={filename}")
                    {
                        Mode = SqliteOpenMode.ReadWriteCreate

                    }.ToString();

                    var inMemConnection = new SqliteConnection(inMemConnectionString);

                    await inMemConnection.OpenAsync();

                    await backupConnection.OpenAsync();


                    inMemConnection.BackupDatabase(backupConnection);


                    bool result = false;

                    try

                    {

                        if (inMemConnection != null && inMemConnection.State == ConnectionState.Open)
                        {
                            inMemConnection.Close();

                        }

                        CloseConnections();

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        result = true;

                    }

                    catch (Exception e)

                    {
                        Send_Monitor_Limit("Backup", $"CloseConnections - 1.2 - {e.Message}");

                        result = false;

                    }



                }



                using (var backupConnection2 = new SqliteConnection(backupConnectionString))
                {

                    // Verifique o token antes de executar o comando
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Backup cancelado.");
                        return; // Retorna sem fazer mais nada
                    }

                    await backupConnection2.OpenAsync();


                    await EncryptToMicrosoft(backupConnection2, password);

                    // Verifique o token antes de executar o IntegrityCheck
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Backup cancelado.");
                        return; // Retorna sem fazer mais nada
                    }
                    await Task.Run(() => IntegrityCheck(filename));


                }

                //   Send_Monitor_Limit("Backup", $"Ok - 2 - {filename}");

            }
            catch (Exception e)
            {
                var filename = Path.GetFileName(path);
                Send_Monitor_Limit("Backup", $"Erro - ProcessFile - 2 - {filename} - {e.Message}");
            }
            //}
            //finally
            //{
            //    _databaseSemaphore.Release(); // Liberar acesso
            //}
        }

        private void DeleteFiles(string folderPath)
        {
            try
            {
                // Obtém uma lista de todos os arquivos dentro da pasta
                string[] files = Directory.GetFiles(folderPath);


                // Verifica se existem arquivos na pasta
                if (files.Length > 0)
                {
                    // Exclui cada arquivo dentro da pasta
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }


                    Send_Monitor_Limit("Backup", $"Arquivos '{folderPath}' excluídos com sucesso!");
                }
                else
                {
                    Send_Monitor_Limit("Backup", $"A pasta '{folderPath}' está vazia.");
                }
            }
            catch (Exception e)
            {

                Send_Monitor_Limit("Backup", $" Delete - {e.Message}");

            }
        }

        private async Task<bool> EncryptToMicrosoft(SqliteConnection backupConnection2, string password)
        {

            try
            {


                var databasePath = backupConnection2.DataSource;

                var databaseName = Path.GetFileNameWithoutExtension(databasePath);

                var databaseTemp = $"{databaseName}_Temp.db";

                File.Delete(databaseTemp);

                var query2 = $@"ATTACH DATABASE '{databaseTemp}' AS encrypted KEY '{password}'; SELECT sqlcipher_export('encrypted'); DETACH DATABASE encrypted;";


                using (var cmd2 = new SqliteCommand(query2, backupConnection2))
                {
                    await cmd2.ExecuteNonQueryAsync();

                };

                bool result = false;

                try

                {

                    if (backupConnection2 != null && backupConnection2.State == ConnectionState.Open)
                    {
                        backupConnection2.Close();

                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    result = true;

                }

                catch

                {

                    result = false;

                }

                Console.WriteLine("2", result);

                SqliteConnection.ClearAllPools();


                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }

                File.Move(databaseTemp, databasePath);

                return result;
            }
            catch (Exception e)
            {
                Send_Monitor_Limit("Backup", $" EncryptToMicrosoft - {e.Message}");
                return false;

            }
        }

        private async Task<string> VerifyBackupDirectory(string clientDirectory, CancellationToken cancellationToken)
        {
            try
            {
                // Recurse into subdirectories of this directory.
                string[] subdirectoryClientEntries = Directory.GetDirectories(clientDirectory, "bkp*", SearchOption.TopDirectoryOnly);

                foreach (string subdirectoryYear in subdirectoryClientEntries)
                {
                    string year = DateTime.Parse(DateTime.Now.ToString()).Year.ToString();

                    string[] subdirectoryYearEntries = Directory.GetDirectories(subdirectoryYear, year, SearchOption.TopDirectoryOnly);

                    // Verifique se o token foi cancelado
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Backup cancelado.");
                        throw new OperationCanceledException(); // Lance uma exceção para interromper a operação
                    }

                    if (Directory.Exists(string.Concat(subdirectoryYearEntries)))
                    {

                        //CreateMonthFolder(subdirectoryYearEntries);

                        string day = DateTime.Parse(DateTime.Now.ToString()).Day.ToString("00");
                        string month = DateTime.Parse(DateTime.Now.ToString()).Month.ToString("00");

                        foreach (var subdirectoryDayMonth in subdirectoryYearEntries)
                        {
                            string[] subdirectoryDayMonthEntries = Directory.GetDirectories(subdirectoryDayMonth, day + month, SearchOption.TopDirectoryOnly);

                            try
                            {
                                if (Directory.Exists(string.Concat(subdirectoryDayMonthEntries)))
                                {
                                    Console.WriteLine("Directory to backup");
                                    string dayMonthDirectory = string.Concat(subdirectoryDayMonthEntries);

                                    await Task.Run(() => DeleteFiles(dayMonthDirectory));

                                    return dayMonthDirectory;
                                }
                                else
                                {
                                    String path = string.Concat(subdirectoryYearEntries) + "\\" + day + month;

                                    Console.WriteLine("Create a directory day month");

                                    string dayMonthDirectory = Directory.CreateDirectory(string.Concat(path)).ToString();

                                    return path;
                                }
                            }
                            catch (Exception)
                            {

                                Send_Monitor_Limit("Backup", $@"Criar diretório dia/mês - Error");

                            }
                        }
                    }
                    else
                    {
                        String path = string.Concat(subdirectoryClientEntries) + "\\" + year;

                        Console.WriteLine("Create a directory year");

                        string yearDirectory = Directory.CreateDirectory(string.Concat(path)).ToString();

                        string[] subdirectoryYearEntriesCreated = Directory.GetDirectories(subdirectoryYear, year, SearchOption.TopDirectoryOnly);

                        if (Directory.Exists(string.Concat(subdirectoryYearEntriesCreated)))
                        {

                            //CreateMonthFolder(subdirectoryYearEntries);

                            string day = DateTime.Parse(DateTime.Now.ToString()).Day.ToString("00");
                            string month = DateTime.Parse(DateTime.Now.ToString()).Month.ToString("00");

                            foreach (var subdirectoryDayMonth2 in subdirectoryYearEntriesCreated)
                            {
                                string[] subdirectoryDayMonthEntries2 = Directory.GetDirectories(subdirectoryDayMonth2, day + month, SearchOption.TopDirectoryOnly);

                                try
                                {
                                    if (Directory.Exists(string.Concat(subdirectoryDayMonthEntries2)))
                                    {
                                        Console.WriteLine("Directory to backup");
                                        string dayMonthDirectory = string.Concat(subdirectoryDayMonthEntries2);

                                        return dayMonthDirectory;
                                    }
                                    else
                                    {
                                        String path2 = string.Concat(subdirectoryYearEntriesCreated) + "\\" + day + month;

                                        Console.WriteLine("Create a directory day month");

                                        string dayMonthDirectory = Directory.CreateDirectory(string.Concat(path2)).ToString();

                                        return path2;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Send_Monitor_Limit("Backup", $@"Criar diretório dia/mês - Error");
                                }
                            }
                        }

                    }


                }
            }
            catch (Exception e)
            {

                Send_Monitor_Limit("Backup", $@"Criar diretório ano - Error");

            }
            Console.WriteLine("Verify folder");
            return "Empty";
        }

        private void IntegrityCheck(string filename)
        {

            try
            {
                string connectionString = $@"Data Source={filename}";

                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    using (SqliteCommand command = new SqliteCommand("PRAGMA integrity_check;", connection))
                    {
                        string result = command.ExecuteScalar() as string;

                        if (result == "ok")
                        {

                            Send_Monitor_Limit("Backup", $@"Backup - {filename} - IntegrityCheck Ok");
                            atual++;

                            if (total == atual)
                            {
                                Send_Monitor_Limit("Backup", "Backup - IntegrityCheck - Fim");
                            }
                        }
                        else
                        {
                            Send_Monitor_Limit("Backup", $@"{filename} - Error - IntegrityCheck - {result}");
                        }
                    }
                }


            }
            catch (Exception e)
            {

                Send_Monitor_Limit("Backup", $@"IntegrityCheck - Error - {e}");

            }

        }

        public void CloseConnections()
        {
            try
            {


                if (sourceConnection != null && sourceConnection.State == ConnectionState.Open)
                {
                    sourceConnection.Close();
                    sourceConnection = null;
                }
                if (backupConnection != null && backupConnection.State == ConnectionState.Open)
                {
                    backupConnection.Close();
                    backupConnection = null;
                }

            }
            catch (Exception e)
            {

                Send_Monitor_Limit("Backup", $@"CloseConnections - Error - {e}");

            }
        }

        #endregion
    }
}
