-- Script para crear la tabla de registros de backups
-- Ejecutar en phpMyAdmin o MySQL directamente

USE biblioteca_virtual;

CREATE TABLE IF NOT EXISTS backup_registros (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NombreArchivo VARCHAR(255) NOT NULL,
    RutaCompleta VARCHAR(500) NOT NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TamanoBytes BIGINT NOT NULL DEFAULT 0,
    Descripcion VARCHAR(500) NULL,
    Exitoso TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Verificar que se creó correctamente
DESCRIBE backup_registros;

