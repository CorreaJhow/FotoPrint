namespace FotoPrint.Models
{
    public class Settings
    {
        public int intervaloImpressaoSegundos { get; set; } = 5;
        public int fotosPorLote { get; set; } = 2;
        public string? caminhoPastaImpressora { get; set; } = "";  // Caminho que impressora monitora
        public string? caminhoPastaBackup { get; set; } = "";      // Caminho para backups do fotógrafo
    }
}
