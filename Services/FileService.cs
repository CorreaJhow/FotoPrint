using Microsoft.AspNetCore.Components.Forms;
using System.IO;
using System.Threading;

namespace FotoPrint.Services
{
    public class FileService
    {
        private readonly ConfigService _configService;
        private readonly PrintSchedulerService _printScheduler;
        private readonly string _wwwroot;
        private string _staging;
        private string _backup;
        private string _impressora;
        private static readonly string[] Allowed = new[] { "image/jpeg", "image/png" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        public FileService(IWebHostEnvironment env, ConfigService configService, PrintSchedulerService printScheduler)
        {
            _configService = configService;
            _printScheduler = printScheduler;
            _wwwroot = env.WebRootPath;

            _staging = Path.Combine(_wwwroot, "Staging");
            CriarPastaSeNaoExistir(_staging);

            AtualizarPastasConfiguradas();
        }

        /// <summary>
        /// Atualiza as variáveis internas das pastas de backup e impressora com valores da configuração.
        /// </summary>
        public void AtualizarPastasConfiguradas()
        {
            var cfg = _configService.Load();

            _backup = !string.IsNullOrWhiteSpace(cfg.caminhoPastaBackup)
                ? cfg.caminhoPastaBackup
                : Path.Combine(_wwwroot, "Backup");
            CriarPastaSeNaoExistir(_backup);

            _impressora = !string.IsNullOrWhiteSpace(cfg.caminhoPastaImpressora)
                ? cfg.caminhoPastaImpressora
                : Path.Combine(_wwwroot, "Impressao");
            CriarPastaSeNaoExistir(_impressora);

            // Logs para facilitar debug (remova em produção)
            Console.WriteLine($"Backup folder: {_backup}");
            Console.WriteLine($"Impressora folder: {_impressora}");
            Console.WriteLine($"Staging folder: {_staging}");
        }

        /// <summary>
        /// Cria o diretório informado se ele não existir.
        /// </summary>
        public void CriarPastaSeNaoExistir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public async Task<(bool ok, string message, List<string> savedRelative)> SaveUploadsAsync(IReadOnlyList<IBrowserFile> files)
        {
            AtualizarPastasConfiguradas();

            var saved = new List<string>();
            if (files == null || files.Count == 0)
                return (false, "Nenhum arquivo selecionado.", saved);

            foreach (var f in files)
            {
                try
                {
                    if (!Allowed.Contains(f.ContentType))
                        return (false, $"Tipo não permitido: {f.ContentType}", saved);
                    if (f.Size > MaxBytes)
                        return (false, $"Arquivo muito grande (>5MB): {f.Name}", saved);

                    var ext = Path.GetExtension(f.Name);
                    var safeName = string.Concat(Path.GetFileNameWithoutExtension(f.Name).Take(40));
                    var finalName = $"{Guid.NewGuid()}_{safeName}{ext}";

                    var stagingPath = Path.Combine(_staging, finalName);
                    var backupPath = Path.Combine(_backup, finalName);
                    var impressoraPath = Path.Combine(_impressora, finalName);

                    await using var stream = f.OpenReadStream(MaxBytes);
                    // Abre arquivo e escreve, garantindo a liberação antes das cópias
                    await using (var fs = File.Create(stagingPath))
                    {
                        await stream.CopyToAsync(fs);
                    }

                    // Copia após o arquivo ser fechado para evitar bloqueio
                    File.Copy(stagingPath, backupPath, overwrite: true);
                    File.Copy(stagingPath, impressoraPath, overwrite: true);

                    saved.Add(Path.Combine("Staging", finalName).Replace("\\", "/"));
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Erro ao salvar o arquivo {f.Name}: {ex.Message}");
                    return (false, $"Erro ao salvar o arquivo {f.Name}: {ex.Message}", saved);
                }
            }

            // Após salvar arquivos, inicia o processo de impressão com delay
            var cfg = _configService.Load();
            var cts = new CancellationTokenSource();

            await _printScheduler.IniciarProcessoImpressaoAsync(cfg.intervaloImpressaoSegundos, cfg.fotosPorLote, cts.Token);

            return (true, $"{saved.Count} Fotos enviadas com sucesso, na fila de impressão.", saved);
        }

        public string[] ListBackupImages()
        {
            AtualizarPastasConfiguradas();

            if (!Directory.Exists(_backup)) return Array.Empty<string>();

            return Directory.GetFiles(_backup)
                .Where(p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || p.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                         || p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetCreationTimeUtc)
                .Select(full => full.Substring(_wwwroot.Length).TrimStart(Path.DirectorySeparatorChar, '/').Replace("\\", "/"))
                .ToArray();
        }
    }
}
