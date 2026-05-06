$ErrorActionPreference = "Stop"
clear

Write-Host "Building CLI..."
dotnet build .\src\IdleWizard.BuildTool.Cli\IdleWizard.BuildTool.Cli.csproj

Write-Host ""
Write-Host "Waiting 1 second..."
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "Building WPF..."
dotnet build .\src\IdleWizard.BuildTool.Wpf\IdleWizard.BuildTool.Wpf.csproj