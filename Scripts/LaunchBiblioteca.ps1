param(
    # Ruta base de XAMPP. Por defecto se intenta usar C:\xampp,
    # pero más abajo se ajusta automáticamente si se encuentra una carpeta "xampp"
    # junto al sistema (pensado para entregas portables en pendrive).
    [string]$XamppPath = "C:\xampp",
    [int]$MysqlPort = 3306,
    [switch]$SkipApache,
    [switch]$StopServicesOnExit,
    [switch]$SkipPublish,
    [switch]$ForcePublish,
    [switch]$DevMode  # Usar dotnet run en lugar de publish (más rápido para desarrollo)
)

$ErrorActionPreference = 'Stop'

function Write-Step($message, [ConsoleColor]$color = [ConsoleColor]::Cyan) {
    $timestamp = (Get-Date).ToString("HH:mm:ss")
    $previousColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $color
    Write-Host "[$timestamp] $message"
    $Host.UI.RawUI.ForegroundColor = $previousColor
}

function Throw-IfMissing([string]$path, [string]$friendly = "") {
    if (-not (Test-Path $path)) {
        $label = if ($friendly) { $friendly } else { $path }
        throw "No se encontró: $label"
    }
}

function Start-XamppScript([string]$scriptName) {
    $fullPath = Join-Path $XamppPath $scriptName
    if (-not (Test-Path $fullPath)) {
        return $false
    }

    Write-Step "Ejecutando $scriptName..." ([ConsoleColor]::Yellow)
    $startInfo = @{
        FilePath        = $fullPath
        WorkingDirectory= $XamppPath
        WindowStyle     = 'Hidden'
        PassThru        = $false
    }
    Start-Process @startInfo
    return $true
}

function Wait-ForPort([int]$port, [int]$timeoutSeconds = 45) {
    Write-Step "Esperando a que el puerto $port quede disponible..." ([ConsoleColor]::DarkCyan)
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $async = $client.BeginConnect('127.0.0.1', $port, $null, $null)
            if ($async.AsyncWaitHandle.WaitOne(1000)) {
                $client.EndConnect($async)
                $client.Dispose()
                Write-Step "Puerto $port disponible." ([ConsoleColor]::Green)
                return $true
            }
            $client.Dispose()
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    Write-Step "El puerto $port no respondió antes del tiempo límite." ([ConsoleColor]::Red)
    return $false
}

function Stop-XamppScript([string]$scriptName) {
    $fullPath = Join-Path $XamppPath $scriptName
    if (-not (Test-Path $fullPath)) {
        return
    }
    Write-Step "Deteniendo $scriptName..." ([ConsoleColor]::DarkYellow)
    Start-Process -FilePath $fullPath -WorkingDirectory $XamppPath -WindowStyle Hidden | Out-Null
}

# --- Configuración de rutas ---
$repoRoot   = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $repoRoot "bin\Release\net6.0\win-x64\publish"
$appExe     = Join-Path $publishDir "BibliotecaVirtualWeb.exe"

# Intentar detectar XAMPP de forma portable:
# 1) Usar el valor pasado por parámetro (por defecto C:\xampp)
# 2) Si no existe, buscar una carpeta "xampp" al lado del sistema (por ejemplo en un pendrive)
$portableXampp = Join-Path (Resolve-Path (Join-Path $repoRoot "..")) "xampp"
if (-not (Test-Path $XamppPath) -and (Test-Path $portableXampp)) {
    $XamppPath = $portableXampp
}

try {
    Write-Step "=== Biblioteca Virtual - Lanzador ===" ([ConsoleColor]::White)
    Throw-IfMissing -path $XamppPath -friendly "XAMPP ($XamppPath)"
    
    # En modo desarrollo no necesitamos el exe publicado
    if (-not $DevMode) {
        if (-not $SkipPublish -and -not (Test-Path $appExe)) {
            Write-Step "Ejecutable no encontrado. Se compilará por primera vez..." ([ConsoleColor]::Yellow)
        } elseif ($SkipPublish) {
            Throw-IfMissing -path $appExe -friendly "Ejecutable publicado ($appExe)"
        }
    }

    # Modo desarrollo: usar dotnet run (más rápido, no requiere publish)
    if ($DevMode) {
        Write-Step "Modo desarrollo activado - usando dotnet run..." ([ConsoleColor]::Cyan)
    }
    elseif (-not $SkipPublish) {
        # Verificar si necesitamos recompilar
        $needsPublish = $ForcePublish
        
        if (-not $needsPublish -and (Test-Path $appExe)) {
            # Comparar fecha del exe con los archivos fuente
            $exeDate = (Get-Item $appExe).LastWriteTime
            $sourceFiles = Get-ChildItem -Path $repoRoot -Include "*.cs","*.cshtml","*.json" -Recurse -File | 
                           Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
            $newerFiles = $sourceFiles | Where-Object { $_.LastWriteTime -gt $exeDate }
            
            if ($newerFiles.Count -gt 0) {
                Write-Step "Detectados $($newerFiles.Count) archivo(s) modificados. Recompilando..." ([ConsoleColor]::Yellow)
                $needsPublish = $true
            } else {
                Write-Step "No hay cambios desde la última compilación. Saltando publish..." ([ConsoleColor]::Green)
            }
        } else {
            $needsPublish = $true
        }
        
        if ($needsPublish) {
            Write-Step "Ejecutando dotnet publish (Release / win-x64)..." ([ConsoleColor]::Yellow)
            $publishArgs = @(
                "publish",
                "`"$repoRoot`"",
                "-c", "Release",
                "-r", "win-x64",
                "--self-contained", "true",
                "/p:PublishSingleFile=true",
                "/p:IncludeNativeLibrariesForSelfExtract=true"
            )
            $publishProcess = Start-Process -FilePath "dotnet" -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru
            if ($publishProcess.ExitCode -ne 0) {
                throw "dotnet publish falló con código $($publishProcess.ExitCode). Revisa la salida."
            }
        }
    }

    # Iniciar MySQL (requerido)
    if (-not (Start-XamppScript -scriptName "mysql_start.bat")) {
        if (-not (Start-XamppScript -scriptName "xampp_start.exe")) {
            throw "No se pudo iniciar MySQL. Revisa la instalación de XAMPP."
        }
    }

    # Iniciar Apache si no se indicó lo contrario
    if (-not $SkipApache) {
        if (-not (Start-XamppScript -scriptName "apache_start.bat")) {
            Write-Step "No se encontró apache_start.bat. Continuando sin Apache..." ([ConsoleColor]::DarkYellow)
        }
    } else {
        Write-Step "SkipApache activo: no se intentará iniciar Apache." ([ConsoleColor]::DarkGray)
    }

    # Esperar puerto MySQL
    if (-not (Wait-ForPort -port $MysqlPort -timeoutSeconds 60)) {
        Write-Step "Advertencia: MySQL podría no estar disponible aún." ([ConsoleColor]::Red)
    }

    # Lanzar aplicación
    Write-Step "Iniciando BibliotecaVirtualWeb..." ([ConsoleColor]::White)
    
    if ($DevMode) {
        # En modo desarrollo, usar dotnet run
        Write-Step "Ejecutando en modo desarrollo (dotnet run)..." ([ConsoleColor]::Cyan)
        $appProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$repoRoot`"" -WorkingDirectory $repoRoot -PassThru
    } else {
        $appProcess = Start-Process -FilePath $appExe -WorkingDirectory $publishDir -PassThru
    }

    if ($StopServicesOnExit) {
        Write-Step "Esperando a que la aplicación cierre para detener XAMPP..." ([ConsoleColor]::DarkGray)
        Wait-Process -Id $appProcess.Id
        Stop-XamppScript -scriptName "mysql_stop.bat"
        if (-not $SkipApache) {
            Stop-XamppScript -scriptName "apache_stop.bat"
        }
        Write-Step "Servicios detenidos. Hasta luego!" ([ConsoleColor]::Green)
    } else {
        Write-Step "Aplicación iniciada correctamente. Puedes cerrar esta ventana." ([ConsoleColor]::Green)
    }
}
catch {
    Write-Step "ERROR: $($_.Exception.Message)" ([ConsoleColor]::Red)
    exit 1
}

