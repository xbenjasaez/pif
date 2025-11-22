## Ejecutable autónomo y lanzador con XAMPP

### 1. Publicar la aplicación

Desde la raíz del proyecto (`C:\Users\benja\Documents\pif`):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true
```

Esto genera los binarios en `bin\Release\net6.0\win-x64\publish\BibliotecaVirtualWeb.exe`.

### 2. Lanzador `IniciarBiblioteca.bat` / `LaunchBiblioteca.ps1`

- `scripts\IniciarBiblioteca.bat` es un wrapper que ejecuta `scripts\LaunchBiblioteca.ps1` con los argumentos que quieras.
- El script en PowerShell realiza los siguientes pasos:
  1. Valida la existencia de XAMPP (`C:\xampp` por defecto, parametrizable) y del ejecutable publicado.
  2. Inicia MySQL (y Apache salvo que se use `-SkipApache`) usando los scripts nativos de XAMPP.
  3. Espera a que el puerto de MySQL (`3306` por defecto) responda antes de continuar.
  4. Lanza `BibliotecaVirtualWeb.exe`.
  5. Opcionalmente detiene los servicios cuando se cierra la app (`-StopServicesOnExit`).

Parámetros disponibles:

```powershell
.\LaunchBiblioteca.ps1 [-XamppPath "D:\xampp"] [-MysqlPort 3306] [-SkipApache] [-StopServicesOnExit]
```

### 3. Uso rápido

1. Publica la app cada vez que necesites una nueva versión.
2. Crea un acceso directo al archivo `scripts\IniciarBiblioteca.bat` y colócalo donde prefieras.
3. Doble clic y el lanzador publicará automáticamente la app (Release win-x64), levantará XAMPP, esperará los puertos y ejecutará la app.

> Si necesitas saltarte la publicación (por ejemplo para debug), ejecuta `LaunchBiblioteca.ps1` con `-SkipPublish` o añade el flag `-SkipPublish` al `IniciarBiblioteca.bat`.

> Consejo: ejecuta el batch con `-SkipApache` si solo necesitas MySQL. Si quieres que al cerrar la app también se apaguen los servicios, añade `-StopServicesOnExit`.

### 4. Respaldo de la base de datos

Usa `scripts\BackupBiblioteca.ps1` para exportar la base `biblioteca_virtual` con todos sus datos:

```powershell
cd C:\Users\benja\Documents\pif
pwsh .\scripts\BackupBiblioteca.ps1 -OutputDir "D:\Respaldos"
```

Parámetros opcionales:

- `-XamppPath "D:\xampp"`: si XAMPP está en otra ruta.
- `-Database "otro_nombre"`: para respaldar otra base.
- `-User` y `-Password`: credenciales MySQL (por defecto `root` sin contraseña).
- `-Zip`: genera un `.zip` (se elimina el `.sql` original para ahorrar espacio).

Para restaurar en otro equipo:

1. Copia el `.sql` generado.
2. En el equipo de destino, ejecuta:

   ```powershell
   mysql -u root -p < ruta\al\respaldo.sql

   Si usaste `-Zip`, descomprímelo primero.
   ```

   (o arrastra el archivo al `mysql.exe` en XAMPP). Después copia también la carpeta `publish` y lanza la app con el batch descrito antes.

