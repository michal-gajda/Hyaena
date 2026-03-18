# Hyaena

```powershell
dotnet new gitignore
git init
```

```powershell
dotnet new sln --name Hyaena
dotnet new web --framework net10.0 --no-https --use-program-main --output src/Web --name Hyaena.Web
dotnet sln add src/Web
```

```powershell
dotnet add src/Web package Microsoft.AspNetCore.Components.WebAssembly.Server
dotnet add src/Web package Yarp.ReverseProxy
dotnet add src/Web package OpenTelemetry.Extensions.Hosting
dotnet add src/Web package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/Web package OpenTelemetry.Instrumentation.AspNetCore
dotnet add src/Web package OpenTelemetry.Instrumentation.Http
dotnet add src/Web package OpenTelemetry.Instrumentation.Runtime
dotnet add src/Web package OpenTelemetry.Instrumentation.Process --version 1.15.0-beta.1
```

```powershell
dotnet new blazorwasm --framework net10.0 --no-https --use-program-main --output src/WebUI --name Hyaena.WebUI
dotnet sln add src/WebUI
dotnet add src/Web reference src/WebUI
```

## Uruchomienie na IIS

### 1. Wymagania

1. Zainstaluj rolę IIS (co najmniej: `Web Server`, `Static Content`, `Default Document`).
2. Zainstaluj ASP.NET Core Hosting Bundle dla wersji runtime zgodnej z projektem (`net10.0`).
3. Po instalacji Hosting Bundle wykonaj restart IIS:

```powershell
iisreset
```

### 2. Publikacja aplikacji

Z poziomu katalogu repozytorium opublikuj projekt serwera do folderu deploymentu:

```powershell
dotnet publish .\src\Web\Hyaena.Web.csproj -c Release -o C:\inetpub\hyaena
```

Po publikacji upewnij się, że w folderze publikacji istnieje plik `web.config` (jest już dodany w projekcie jako `src/Web/web.config`).

### 3. Konfiguracja Application Pool

W IIS Manager:

1. Utwórz nowy Application Pool, np. `HyaenaPool`.
2. Ustaw `NET CLR Version` na `No Managed Code`.
3. `Managed pipeline mode`: `Integrated`.

### 4. Konfiguracja witryny

1. Utwórz nową witrynę (lub aplikację) wskazującą na katalog publikacji, np. `C:\inetpub\hyaena`.
2. Przypisz ją do `HyaenaPool`.
3. Ustaw binding (np. `http`, port `8080`, host opcjonalny).

### 5. Uprawnienia

Nadaj uprawnienia odczytu do folderu publikacji dla tożsamości puli aplikacji (np. `IIS AppPool\HyaenaPool`).

### 6. Start i weryfikacja

1. Uruchom witrynę w IIS.
2. Otwórz adres z bindingu, np. `http://localhost:8080`.
3. Health check: `http://localhost:8080/healthz`.

Jeżeli aplikacja nie startuje, sprawdź:

1. `Event Viewer` -> `Windows Logs` -> `Application`.
2. Czy zainstalowany Hosting Bundle odpowiada wersji runtime aplikacji.
3. Logi stdout po tymczasowym włączeniu `stdoutLogEnabled="true"` w `web.config`.
