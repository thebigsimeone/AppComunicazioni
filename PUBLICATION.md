# Configurazione privata

Le credenziali e i dati operativi devono restare fuori dal repository. Le impostazioni ASP.NET Core si configurano tramite variabili di ambiente (separatore doppio underscore) oppure dotnet user-secrets in sviluppo, indicando il progetto con --project. I valori vuoti nei file di esempio devono essere configurati prima di utilizzare i servizi corrispondenti. I file .env non vengono caricati automaticamente.

Non includere backup, esportazioni, log, chiavi private o dati personali nei commit.

La bonifica dei file correnti non elimina i valori presenti nella cronologia Git: prima di rendere pubblico il repository, revocare o ruotare le credenziali già versionate e bonificare la cronologia, gli altri branch/tag e gli eventuali allegati.

Configurare ConnectionStrings__ComDbContext, EmailSettings__MailServer, EmailSettings__MailPort, EmailSettings__SenderName, EmailSettings__Sender, EmailSettings__Password, Encryption__Key (32 byte UTF-8) e, facoltativamente, Notifications__Recipient. La rotazione della chiave rende non più decifrabili gli URL cifrati con la chiave precedente. Il progetto Api richiede a sua volta ConnectionStrings__ComDbContext. I tenant dimostrativi sono TENANT_A e TENANT_B. Il backup del database è escluso dalla versione bonificata.

I fornitori dimostrativi sono FORNITORE_A/B/C. SupplierRouting__SupplierAMarkers__0 e SupplierRouting__SupplierBMarkers__0 (e indici successivi) configurano i codici usati per smistare i file del servizio BA2. Per dati preesistenti adattare privatamente i valori di tenant e fornitore.
