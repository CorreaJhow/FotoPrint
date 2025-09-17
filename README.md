# FotoPrint - Impressão Instantânea de Fotos para Eventos

## Visão Geral / Overview

FotoPrint é um sistema desenvolvido com Blazor Server (.NET 6) para atender fotógrafos de eventos, especialmente casamentos, oferecendo a funcionalidade de impressão instantânea de fotos durante o evento. O sistema roda localmente no computador do cliente, permitindo que convidados enviem fotos via aplicação web, que são organizadas, movidas para a impressora e automaticamente feitas cópias de backup.

FotoPrint is a system built with Blazor Server (.NET 6) to support event photographers, particularly weddings, providing instant photo printing during events. The system runs locally on the client’s computer, allowing guests to upload photos via a web app, which are then organized, sent to the printer folder, and simultaneously backed up.

---

## Funcionalidades Principais / Key Features

- Interface web local para upload de fotos com preview  
- Configuração administrativa para intervalo de impressão, número de fotos por lote e caminhos das pastas  
- Monitoramento de pastas para mover fotos do upload para a fila de impressão e backup  
- Download dos backups via endpoint zip  
- Autenticação simples para área administrativa  
- Frontend estilizado com Tailwind CSS e Font Awesome via CDN  
- Totalmente offline e local, sem dependência de servidores externos  

- Local web interface for photo uploads with preview  
- Admin panel to configure printing interval, batch size, and folder paths  
- Folder monitoring to move photos from upload to print queue and backup  
- Backup download via zip endpoint  
- Simple authentication for admin area  
- Frontend styled with Tailwind CSS and Font Awesome via CDN  
- Fully offline and local, without external server dependencies  

---

## Configuração / Configuration

O arquivo `config.json` contém as principais configurações do sistema:

<pre>
{
"intervaloImpressaoSegundos": 10,
"fotosPorLote": 2,
"caminhoPastaImpressora": "C:\FotoPrint\Impressao",
"caminhoPastaBackup": "C:\FotoPrint\Backup",
"titulo": "Noivo & Noiva"
}
</pre>

text

- `intervaloImpressaoSegundos`: tempo (em segundos) entre cada operação de mover fotos para impressão  
- `fotosPorLote`: quantidade máxima de fotos movidas por lote para impressão  
- `caminhoPastaImpressora`: pasta que a impressora monitora para imprimir  
- `caminhoPastaBackup`: pasta para armazenar backups das fotos  
- `titulo`: texto que aparece na tela inicial (ex: nome dos noivos)  

---

## Execução Local / Running Locally

1. Clone o repositório:  
git clone <url>

text
2. Abra o projeto no Visual Studio 2022 ou superior com suporte .NET 6.  
3. Compile e publique para pasta local (Release).  
4. Configure o arquivo `config.json` conforme necessário.  
5. Execute a aplicação com:  
dotnet FotoPrint.dll

text
6. Acesse no navegador:  
- Localmente: `http://localhost:5000`  
- Na rede local: `http://<IP_LOCAL>:5000` (configurar firewall para liberar a porta 5000)  

---

## Uso da Aplicação / Using the Application

- Área pública: página `/upload` para convidados enviarem fotos com preview.  
- Painel administrativo: página `/admin` com login simples para alterar configurações, como intervalo, lote, pastas e título.  
- Impressão e backup automáticos baseados nos arquivos movidos em lote da pasta `Staging` para `Impressao` e `Backup`.  

---

## Tecnologias Utilizadas / Technologies Used

- Blazor Server (.NET 6)  
- Tailwind CSS (via CDN)  
- Font Awesome (via CDN)  
- BackgroundService para tarefas de impressão  
- Arquivos JSON para configuração dinâmica  
- Injeção de dependência padrão ASP.NET Core  

---

## Segurança / Security

- Login simples para área administrativa  
- Aplicação local e sem exposição externa por padrão  
- Firewall e portas configuráveis para uso em rede local  

---

## Licença / License

MIT License – uso livre e alteração permitida.
