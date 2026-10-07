# AppComunicazioni

Gestionale web per seguire l'invio e il rientro di lavorazioni affidate a fornitori. Registra comunicazioni, quantità di protocolli, date, servizio, tenant e stato di restituzione; consente di importare i dettagli da Excel e inviare notifiche email.

La soluzione comprende una **applicazione MVC** per gli operatori e una **API di importazione** per integrare altri strumenti.

## A cosa serve

- Mantenere un registro distinto per TENANT_A e TENANT_B.
- Registrare una lavorazione e i relativi dettagli.
- Monitorare protocolli richiesti e restituiti.
- Gestire destinatari, notifiche e report email.
- Consultare riepiloghi e dati di contabilità delle lavorazioni.
- Importare un file Excel tramite interfaccia o richiesta HTTP.

## Progetti e tecnologie

| Progetto | Funzione | Porta configurata |
| --- | --- | --- |
| [AppComunicazioni](AppComunicazioni) | Interfaccia MVC e servizi di monitoraggio | 5185 |
| [Api](Api) | Upload Excel e registrazione dei dati | 5280 |

La soluzione usa **.NET 8**, ASP.NET Core, Entity Framework Core 9 con SQL Server, AutoMapper, EPPlus, MailKit e Serilog.

Servono il .NET SDK compatibile con `net8.0`, SQL Server e SMTP per le notifiche.

## Configurazione

| Chiave | Scopo |
| --- | --- |
| `ConnectionStrings:ComDbContext` | Database comune ai due progetti |
| `EmailSettings:MailServer`, `MailPort` | Server e porta SMTP |
| `EmailSettings:SenderName`, `Sender`, `Password` | Mittente e autenticazione SMTP |
| `Encryption:Key` | Chiave di cifratura, esattamente 32 byte UTF-8 |
| `Notifications:Recipient` | Destinatario facoltativo della notifica di arresto |
| `SupplierRouting:SupplierAMarkers` | Elenco di marcatori per riconoscere il fornitore A |
| `SupplierRouting:SupplierBMarkers` | Elenco di marcatori per riconoscere il fornitore B |

La scelta del fornitore dipende dal servizio e, per alcune regole, dai marcatori nel nome file.

Usare User Secrets o variabili d'ambiente. Per condividere i valori tra i processi web e API, configurare l'ambiente di entrambi; nei nomi delle variabili sostituire `:` con `__`. Gli elementi di un array usano suffissi `__0`, `__1` e così via.

Esempio locale PowerShell, dalla radice:

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__ComDbContext = "Server=(localdb)\MSSQLLocalDB;Database=ComunicazioniDemo;Trusted_Connection=True;"
~~~

Impostare anche la chiave di cifratura e la configurazione SMTP con valori privati dell'ambiente. Nei due terminali di avvio devono essere disponibili le impostazioni necessarie.

### Database

Il repository non include un backup operativo né una sequenza di migrazioni per creare un ambiente completo. Predisporre lo schema SQL coerente con [ComDbContext](AppComunicazioni/Data/ComDbContext.cs) e i [modelli](AppComunicazioni/Models) prima dell'avvio. Non è previsto un popolamento automatico con dati dimostrativi.

## Avvio

Compilare dalla radice:

~~~powershell
dotnet restore AppComunicazioni.sln
dotnet build AppComunicazioni.sln
dotnet run --project AppComunicazioni/AppComunicazioni.csproj
~~~

Aprire `http://localhost:5185`.

Per l'API, usare un secondo terminale e avviare **dalla cartella Api**: il codice calcola il percorso della configurazione della web app a partire dalla directory corrente.

~~~powershell
cd Api
dotnet run --project Api.csproj
~~~

L'API ascolta sulla porta `5280`; in ambiente Development la documentazione è su `http://localhost:5280/swagger`.

## Utilizzo dall'interfaccia

1. Configurare i destinatari delle comunicazioni.
2. Aprire la sezione del tenant interessato.
3. Creare una comunicazione indicando file, servizio, date e quantità.
4. Allegare l'Excel, se si vogliono importare i dettagli dei protocolli.
5. Aggiornare la lavorazione al rientro e controllare quantità e stato.
6. Consultare report e riepiloghi contabili.

La web app include un servizio in background che esegue controlli nei giorni feriali alle 10 e invia il riepilogo alle 17, secondo l'orario del server. Anche alcune operazioni interattive possono attivare notifiche: durante le prove configurare destinatari e SMTP di test.

## Importazione Excel tramite API

Endpoint: `POST /api/Create/upload`, contenuto `multipart/form-data`, campo file chiamato `File`.

Un esempio di nome compatibile è:

~~~text
IT-1_TENANT_A_VSA_20260929-1200_3.xlsx
~~~

Il nome contiene prefisso/codice fornitore, tenant, codice servizio, data e quantità finale dei protocolli. L'underscore di `TENANT_A` o `TENANT_B` viene mantenuto come parte del tenant. Il servizio deve corrispondere a un valore di [ServizioType](AppComunicazioni/Models/ServizioType.cs), ad esempio `VSA`, `MIM` o `APP`. Il conteggio viene letto dall'ultimo segmento: non aggiungere ulteriori suffissi numerici.

Il primo foglio dell'Excel deve avere una riga di intestazione con le colonne **Protocollo** e **Codice Fiscale**. Senza queste intestazioni non vengono importati i dettagli.

Esempio PowerShell con `curl.exe`:

~~~powershell
curl.exe --fail-with-body -X POST "http://localhost:5280/api/Create/upload" -F "File=@C:\dati-demo\IT-1_TENANT_A_VSA_20260929-1200_3.xlsx"
~~~

L'operazione registra una comunicazione nel database. Usare file e database dimostrativi durante le prove.

## Verifiche

~~~powershell
dotnet run --project tests/FileNames/FileNames.csproj
~~~

Il controllo verifica il riconoscimento dei nomi con entrambi i tenant, anche in minuscolo, senza richiedere SQL Server. Il flusso completo di importazione e notifica richiede database e SMTP configurati.

Vedere [PUBLICATION.md](PUBLICATION.md) per la gestione delle impostazioni riservate.
