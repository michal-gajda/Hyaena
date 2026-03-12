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
