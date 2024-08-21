using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Middleware.Banco_Dados.Bkp;
using Middleware.Controle_Acesso.List_Access_Control_Memory.Update;
using Middleware.Equipamentos.Equipamento.Classe;
using Middleware.Equipamentos.Monitor;
using Middleware.Middleware;
using Middleware.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Middleware.Banco_Dados.Bkp
{
    public class C_BackupNuvem
    {

        public event EventHandler<C_Monitor_Handler> monitor;
        public C_Monitor_Queue monitorQueue { get; set; }

        private readonly string diretorio = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                        @"\seurepo";

        private static string parentDirectoryId = C_Middleware_Globais.Pasta_Google_Drive; 


        #region BackupNuvem

        public async Task BackupNuvem()
        {
            try
            {
                var service = GetDriveService();

                Console.WriteLine("Backup", "Backup - Nuvem - Início");
                string localBackupDir = new C_BackupNuvem().diretorio;

                // 1. Obter lista de pastas no Google Drive
                var driveFolders = await GetFolders(service, parentDirectoryId);

                // 2. Obter lista de pastas locais
                var localFolders = GetLocalFolders(localBackupDir);

                // 3. Sincronizar pastas
                await SyncFolders(service, driveFolders, localFolders);


            }
            catch (Exception)
            {

                Console.WriteLine("Backup", "Erro - Backup Google Drive.");
            }


        }

        private DriveService GetDriveService()
        {
            try
            {


                var projectPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
                string PathToServiceAccountKeyFile = Path.Combine(projectPath, "Resources\\credentialGoogleDrive.json");


                var credential = GoogleCredential.FromFile(PathToServiceAccountKeyFile)
                    .CreateScoped(DriveService.ScopeConstants.Drive);

                return new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                });

            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", "Erro - GetDriveService - {e}.");
                return null;
            }
        }

        // Cria uma nova pasta no Google Drive.
        private async Task<string> CreateFolder(DriveService service, string folderName, string parentId)
        {
            try
            {


                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string>() { parentId }
                };

                var request = service.Files.Create(fileMetadata);
                request.Fields = "*";

                var file = await request.ExecuteAsync();
                return file.Id;

            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", "Erro - CreateFolder - {e}.");
                return null;

            }
        }

        // Faz upload de um arquivo local para o Google Drive.
        private async Task<string> UploadFile(DriveService service, string filePath, string parentId)
        {
            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new List<string>() { parentId },
                    MimeType = "" // Para arquivos
                };

                using (FileStream fsSource = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {

                    var request = service.Files.Create(fileMetadata, fsSource, fileMetadata.MimeType);
                    request.Fields = "id";


                    var file = await request.UploadAsync(CancellationToken.None);

                    if (file.Status == Google.Apis.Upload.UploadStatus.Failed)
                    {
                        Console.WriteLine($"Falha ao enviar o arquivo: {file.Exception.Message}");
                        return null;
                    }

                    return request.ResponseBody?.Id;

                }
            }

            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro UploadFile:{e.Message} ");
                return null;
            }

        }

        // Obtém a lista de pastas em uma pasta do Google Drive
        private async Task<List<Google.Apis.Drive.v3.Data.File>> GetFolders(DriveService service, string folderId)
        {

            try
            {
                var request = service.Files.List();
                request.Q = $"parents = '{folderId}' and mimeType = 'application/vnd.google-apps.folder'";
                request.Fields = "files(id, name)";

                var response = await request.ExecuteAsync();
                return (List<Google.Apis.Drive.v3.Data.File>)response.Files;
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro GetFolders:{e.Message} ");

                return null;
            }

        }

        // Obtém a lista de pastas locais
        private List<string> GetLocalFolders(string rootPath)
        {
            try
            {
                return Directory.GetDirectories(rootPath, "bkp*", SearchOption.AllDirectories)
                                .Select(p => p.Replace(rootPath, "").Trim('\\')) // Remove o caminho base e a barra invertida
                                .ToList();
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro GetLocalFolders:{e.Message} ");

                return null;
            }


        }

        // Sincroniza as pastas do Google Drive com as pastas locais
        private async Task SyncFolders(DriveService service, List<Google.Apis.Drive.v3.Data.File> driveFolders, List<string> localFolders)
        {
            try
            {
                foreach (var localFolder in localFolders)
                {
                    // Encontra a pasta correspondente no Google Drive
                    var driveFolder = driveFolders.FirstOrDefault(f => f.Name == localFolder);
                    string localBackupDir = diretorio;
                    // Pasta local existe, mas não no Google Drive
                    if (driveFolder == null)
                    {
                        // Cria a pasta no Google Drive
                        Console.WriteLine($"Pasta '{localFolder}' não encontrada no Google Drive. Iniciando envio...");
                        var folderId = await CreateFolder(service, localFolder, parentDirectoryId);
                        Console.WriteLine($"Pasta '{localFolder}' criada no Google Drive com ID: {folderId}");
                        await UploadFolder(service, Path.Combine(localBackupDir, localFolder), folderId);
                        Console.WriteLine($"Pasta '{localFolder}' enviada para o Google Drive.");
                    }
                    else
                    {
                        // Pasta local existe no Google Drive
                        // Atualiza o conteúdo da pasta
                        Console.WriteLine($"Pasta '{localFolder}' encontrada no Google Drive. Iniciando atualização...");
                        await UpdateFolder(service, Path.Combine(localBackupDir, localFolder), driveFolder.Id);
                        Console.WriteLine($"Pasta '{localFolder}' atualizada.");

                        // Remove a pasta da lista de pastas do Google Drive
                        driveFolders.Remove(driveFolder);
                    }
                }

                // Pasta no Google Drive, mas não na pasta local
                foreach (var driveFolder in driveFolders)
                {
                    // Exclui a pasta do Google Drive (ou atualiza a lógica para manter a pasta, se necessário)
                    Console.WriteLine($"Pasta '{driveFolder.Name}' encontrada no Google Drive, mas não na pasta local. Excluindo do Google Drive...");
                    // await DeleteFolder(service, driveFolder.Id); // Implemente a função DeleteFolder
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro SyncFolders:{e.Message} ");


            }

        }

        // Faz upload de uma pasta local para o Google Drive
        private async Task UploadFolder(DriveService service, string localFolder, string parentId)
        {

            try
            {
                foreach (var item in Directory.EnumerateFileSystemEntries(localFolder))
                {
                    if (Directory.Exists(item))
                    {
                        var subFolderId = await CreateFolder(service, Path.GetFileName(item), parentId);
                        Console.WriteLine($"Subpasta '{Path.GetFileName(item)}' criada no Google Drive com ID: {subFolderId}");
                        await UploadFolder(service, item, subFolderId);
                    }
                    else
                    {
                        await UploadFile(service, item, parentId);
                        Console.WriteLine($"Arquivo '{Path.GetFileName(item)}' enviado para o Google Drive.");
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro UploadFolder:{e.Message} ");


            }


        }

        // Atualiza uma pasta existente no Google Drive
        private async Task UpdateFolder(DriveService service, string localFolder, string folderId)
        {
            try
            {
                // 1. Obter lista de arquivos e subpastas no Google Drive
                var driveFiles = await GetFilesAndFolders(service, folderId);

                // 2. Itera sobre os arquivos e subpastas locais
                foreach (var localItem in Directory.EnumerateFileSystemEntries(localFolder))
                {
                    // 3. Verifica se o item local existe no Google Drive
                    var driveItem = driveFiles.FirstOrDefault(f => f.Name == Path.GetFileName(localItem));

                    if (Directory.Exists(localItem))
                    {
                        // Subpasta:
                        if (driveItem != null)
                        {
                            // Subpasta existente, atualiza o conteúdo
                            Console.WriteLine($"Subpasta '{Path.GetFileName(localItem)}' encontrada no Google Drive. Iniciando atualização...");
                            await UpdateFolder(service, localItem, driveItem.Id); // Corrigido: chamada recursiva para UpdateFolder
                            Console.WriteLine($"Subpasta '{Path.GetFileName(localItem)}' atualizada.");
                        }
                        else
                        {
                            // Subpasta não existente, cria no Google Drive
                            Console.WriteLine($"Subpasta '{Path.GetFileName(localItem)}' não encontrada no Google Drive. Iniciando envio...");
                            var subFolderId = await CreateFolder(service, Path.GetFileName(localItem), folderId);
                            Console.WriteLine($"Subpasta '{Path.GetFileName(localItem)}' criada no Google Drive com ID: {subFolderId}");
                            await UploadFolder(service, localItem, subFolderId); // Corrigido: chamada para UploadFolder
                            Console.WriteLine($"Subpasta '{Path.GetFileName(localItem)}' enviada para o Google Drive.");
                        }
                    }
                    else
                    {
                        // Arquivo:
                        if (driveItem != null)
                        {
                            // Arquivo existente, atualiza
                            Console.WriteLine($"Arquivo '{Path.GetFileName(localItem)}' encontrado no Google Drive. Iniciando atualização...");
                            //await UpdateFile(service, driveItem.Id, localItem);
                            Console.WriteLine($"Arquivo '{Path.GetFileName(localItem)}' atualizado.");
                        }
                        else
                        {
                            // Arquivo não existente, envia para o Google Drive
                            Console.WriteLine($"Arquivo '{Path.GetFileName(localItem)}' não encontrado no Google Drive. Iniciando envio...");
                            await UploadFile(service, localItem, folderId);
                            Console.WriteLine($"Arquivo '{Path.GetFileName(localItem)}' enviado para o Google Drive.");
                        }
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro UpdateFolder:{e.Message} ");


            }




        }

        // Obtém a lista de arquivos e subpastas em uma pasta do Google Drive
        private async Task<List<Google.Apis.Drive.v3.Data.File>> GetFilesAndFolders(DriveService service, string folderId)
        {

            try
            {
                var request = service.Files.List();
                request.Q = $"parents = '{folderId}'";
                request.Fields = "files(id, name)";

                var response = await request.ExecuteAsync();
                return (List<Google.Apis.Drive.v3.Data.File>)response.Files;
            }
            catch (Exception e)
            {

                Console.WriteLine("Backup", $"Erro GetFilesAndFolders:{e.Message} ");

                return null;
            }

        }


        #endregion
    }
}
