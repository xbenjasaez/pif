param(
    [string]$XamppPath = "C:\xampp",
    [string]$Database = "biblioteca_virtual",
    [string]$User = "root",
    [string]$Password = "",
    [string]$OutputDir = "$(Join-Path $PSScriptRoot '..\backups')",
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'

function Write-Step($message) {
    $timestamp = (Get-Date).ToString("HH:mm:ss")
    Write-Host "[$timestamp] $message" -ForegroundColor Cyan
}

function Throw-IfMissing($path, $label) {
    if (-not (Test-Path $path)) {
        throw "No se encontró $label en '$path'"
    }
}

try {
    $mysqldump = Join-Path $XamppPath "mysql\bin\mysqldump.exe"
    Throw-IfMissing $mysqldump "mysqldump.exe"

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $dumpFile = Join-Path $OutputDir "$Database-$timestamp.sql"

    Write-Step "Generando respaldo en $dumpFile"

    $args = @(
        "--single-transaction"
        "--routines"
        "--events"
        "--add-drop-database"
        "--databases", $Database
        "--default-character-set=utf8mb4"
        "--result-file=$dumpFile"
        "-u", $User
    )

    if ($Password -ne "") {
        $args += "-p$Password"
    }

    $process = Start-Process -FilePath $mysqldump -ArgumentList $args -NoNewWindow -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "mysqldump salió con código $($process.ExitCode)"
    }

    if ($Zip) {
        $zipPath = Join-Path $OutputDir "$Database-$timestamp.zip"
        Write-Step "Comprimiendo respaldo en $zipPath"
        Compress-Archive -Path $dumpFile -DestinationPath $zipPath -Force
        Remove-Item $dumpFile
        $dumpFile = $zipPath
    }

    Write-Step "Respaldo completado."
    Write-Host "Copia el archivo '$dumpFile' al otro equipo y restauralo con:"
    Write-Host "  mysql -u root -p < $([System.IO.Path]::GetFileName($dumpFile))" -ForegroundColor Yellow
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

