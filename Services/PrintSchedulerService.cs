namespace FotoPrint.Services
{
    public class PrintSchedulerService : BackgroundService
    {
        private readonly ILogger<PrintSchedulerService> _logger;
        private readonly ConfigService _config;
        private readonly string _staging;
        private readonly string _impressao;
        private readonly string _backup;
        private readonly string _transicao;
        private int _lastTransitionCount = 0; // Contador visível

        public PrintSchedulerService(ILogger<PrintSchedulerService> logger, IWebHostEnvironment env, ConfigService config)
        {
            _logger = logger;
            _config = config;
            var cfg = _config.Load();

            _staging = Path.Combine(env.WebRootPath, "Staging");
            if (!Directory.Exists(_staging)) Directory.CreateDirectory(_staging);

            if (!string.IsNullOrWhiteSpace(cfg.caminhoPastaImpressora) && Path.IsPathRooted(cfg.caminhoPastaImpressora))
                _impressao = cfg.caminhoPastaImpressora;
            else
                _impressao = Path.Combine(env.ContentRootPath, "Impressao");
            if (!Directory.Exists(_impressao)) Directory.CreateDirectory(_impressao);

            if (!string.IsNullOrWhiteSpace(cfg.caminhoPastaBackup) && Path.IsPathRooted(cfg.caminhoPastaBackup))
                _backup = cfg.caminhoPastaBackup;
            else
                _backup = Path.Combine(env.ContentRootPath, "Backup");
            if (!Directory.Exists(_backup)) Directory.CreateDirectory(_backup);

            if (!string.IsNullOrWhiteSpace(cfg.caminhoPastaTransicao) && Path.IsPathRooted(cfg.caminhoPastaTransicao))
                _transicao = cfg.caminhoPastaTransicao;
            else
                _transicao = Path.Combine(env.ContentRootPath, "Transicao");
            if (!Directory.Exists(_transicao)) Directory.CreateDirectory(_transicao);

            _logger.LogInformation($"Staging folder: {_staging}");
            _logger.LogInformation($"Impressao folder: {_impressao}");
            _logger.LogInformation($"Backup folder: {_backup}");
            _logger.LogInformation($"Transicao folder: {_transicao}");
        }


        /// <summary>
        /// Método para ser chamado após o envio, que aguarda o intervalo e move as fotos para impressão e backup conforme lote.
        /// </summary>
        /// <param name="intervaloEmSegundos">Segundos para esperar antes de mover os arquivos</param>
        /// <param name="lote">Quantidade de fotos para processar nesta vez</param>
        /// <param name="cancellationToken">Token para cancelar a operação</param>
        public async Task IniciarProcessoImpressaoAsync(int intervaloEmSegundos, int lote, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Aguardando {Intervalo} segundos antes de iniciar impressão", intervaloEmSegundos);

            await Task.Delay(TimeSpan.FromSeconds(intervaloEmSegundos), cancellationToken);

            _logger.LogInformation("Iniciando movimentação dos arquivos para pasta de impressão e backup");

            var arquivos = Directory.Exists(_staging)
                ? Directory.GetFiles(_staging).OrderBy(File.GetCreationTimeUtc).Take(lote).ToList()
                : new List<string>();

            foreach (var arquivo in arquivos)
            {
                try
                {
                    var destImpressora = Path.Combine(_impressao, Path.GetFileName(arquivo));
                    var destBackup = Path.Combine(_backup, Path.GetFileName(arquivo));

                    if (!File.Exists(destImpressora))
                    {
                        File.Move(arquivo, destImpressora);
                        _logger.LogInformation("Movido para impressão: {FileName}", Path.GetFileName(arquivo));
                    }

                    if (!File.Exists(destBackup))
                    {
                        File.Copy(destImpressora, destBackup);
                        _logger.LogInformation("Copiado para backup: {FileName}", Path.GetFileName(arquivo));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao mover arquivo {FileName}", Path.GetFileName(arquivo));
                }
            }

            _logger.LogInformation("Processo de impressão finalizado");
        }
        public void RegistrarUploadNaFila(string arquivo)
        {
            // Este método pode ser chamado após o upload para backup+movimentação para transition
            var destBackup = Path.Combine(_backup, Path.GetFileName(arquivo));
            if (!File.Exists(destBackup))
                File.Copy(arquivo, destBackup, true);

            var destTransicao = Path.Combine(_transicao, Path.GetFileName(arquivo));
            if (!File.Exists(destTransicao))
                File.Copy(arquivo, destTransicao, true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cfg = _config.Load();
                var arquivosFila = Directory.Exists(_transicao) ?
                    Directory.GetFiles(_transicao).OrderBy(File.GetCreationTime).ToList() :
                    new List<string>();

                _lastTransitionCount = arquivosFila.Count;

                // Só processa se atingir o lote configurado!
                if (arquivosFila.Count >= cfg.fotosPorLote)
                {
                    foreach (var file in arquivosFila)
                    {
                        var destImpressora = Path.Combine(_impressao, Path.GetFileName(file));
                        if (!File.Exists(destImpressora))
                            File.Move(file, destImpressora);
                    }
                    foreach (var file in Directory.GetFiles(_transicao))
                        File.Delete(file);
                }
                await Task.Delay(TimeSpan.FromSeconds(cfg.intervaloImpressaoSegundos), stoppingToken);
            }
        }

        public int GetTransitionCount()
        {
            return _lastTransitionCount;
        }
    }
}
