using Microsoft.AspNetCore.Components.Forms;

namespace FotoPrint.Services
{
    public class FileService
    {
        private readonly string _wwwroot;
        private readonly string _staging;
        private readonly string _backup;
        private static readonly string[] Allowed = new[] { "image/jpeg", "image/png" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        public FileService(IWebHostEnvironment env)
        {
            _wwwroot = env.WebRootPath;
            _staging = Path.Combine(_wwwroot, "Staging");
            _backup = Path.Combine(_wwwroot, "Backup");
            Directory.CreateDirectory(_staging);
            Directory.CreateDirectory(_backup);
        }

        public async Task<(bool ok, string message, List<string> savedRelative)> SaveUploadsAsync(IReadOnlyList<IBrowserFile> files)
        {
            var saved = new List<string>();
            if (files == null || files.Count == 0)
                return (false, "Nenhum arquivo selecionado.", saved);

            foreach (var f in files)
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

                await using (var stream = f.OpenReadStream(MaxBytes))
                await using (var fs = File.Create(stagingPath))
                {
                    await stream.CopyToAsync(fs);
                }

                // Cópia para backup
                File.Copy(stagingPath, backupPath, overwrite: false);

                // Caminho relativo para exibir na UI (se precisar)
                saved.Add(Path.Combine("Staging", finalName).Replace("\\", "/"));
            }

            return (true, $"{saved.Count} arquivo(s) salvo(s).", saved);
        }

        public string[] ListBackupImages()
        {
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
