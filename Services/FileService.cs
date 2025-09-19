using Microsoft.AspNetCore.Components.Forms;
namespace FotoPrint.Services
{
    public class FileService
    {
        private readonly ConfigService _configService;
        private readonly PrintSchedulerService _printScheduler;
        private readonly string _wwwroot;
        private string _backup;
        private string _transicao;
        private string _impressora;
        private static readonly string[] Allowed = new[] { "image/jpeg", "image/png" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        public FileService(IWebHostEnvironment env, ConfigService configService, PrintSchedulerService printScheduler)
        {
            _configService = configService;
            _printScheduler = printScheduler;
            _wwwroot = env.WebRootPath;
            AtualizarPastasConfiguradas();
        }

        /// <summary>
        /// Atualiza as variáveis das pastas de backup e transição conforme configuração.
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

            _transicao = !string.IsNullOrWhiteSpace(cfg.caminhoPastaTransicao)
                ? cfg.caminhoPastaTransicao
                : Path.Combine(_wwwroot, "Transicao");
            CriarPastaSeNaoExistir(_transicao);
        }


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

                    var backupPath = Path.Combine(_backup, finalName);
                    var transicaoPath = Path.Combine(_transicao, finalName);

                    // Salvar arquivo em backup
                    await using (var stream = f.OpenReadStream(MaxBytes))
                    await using (var fsBackup = File.Create(backupPath))
                    {
                        await stream.CopyToAsync(fsBackup);
                    }

                    // Copiar o mesmo arquivo para pasta transicao
                    File.Copy(backupPath, transicaoPath, overwrite: true);

                    saved.Add(finalName);

                    // Verificar quantidade de fotos na pasta transição
                    var arquivosTransicao = Directory.GetFiles(_transicao);
                    var cfg = _configService.Load();

                    if (arquivosTransicao.Length == cfg.fotosPorLote)
                    {
                        // Mover todas as fotos da transição para a impressora
                        foreach (var arquivo in arquivosTransicao)
                        {
                            var destImpressora = Path.Combine(_impressora, Path.GetFileName(arquivo));
                            if (File.Exists(destImpressora))
                                File.Delete(destImpressora);
                            File.Move(arquivo, destImpressora);
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Erro ao salvar o arquivo {f.Name}: {ex.Message}");
                    return (false, $"Erro ao salvar o arquivo {f.Name}: {ex.Message}", saved);
                }
            }

            return (true, $"{saved.Count} foto(s) enviada(s) com sucesso para a fila de impressão.", saved);
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
