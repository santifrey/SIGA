-- =============================================================================
-- SIGA - Script de Reinicio Limpio de Base de Datos (MySQL)
-- =============================================================================
-- Opción más simple y limpia:
-- 1. Ejecutá este script en MySQL Workbench o consola MySQL con el backend detenido.
-- 2. Iniciá el backend (`dotnet run --project src/SIGA.API`).
--
-- Al iniciar con `siga_db` vacía:
--   • EF Core (`EnsureCreatedAsync`) crea automáticamente las 25 tablas actuales
--     del dominio SIN ninguna tabla ni columna obsoleta (sin `Ventas`,
--     `DetallesVentas`, `OperacionesComerciales`, `TipoOperacion`, `VentaId`,
--     `OperacionComercialId` ni `Discriminator`).
--   • `DatabaseSeeder` carga automáticamente:
--       - Los 5 roles del sistema y la matriz completa de 31 permisos.
--       - El usuario administrador (`admin` / `Admin123!`).
--       - La empresa titular (`Azafrán Mediterráneo S.A.`) y la contraparte
--         técnica (`Ajustes de Inventario`).
--       - Datos de ejemplo (Sucursal Villa General Belgrano, 3 Clientes con
--         nombre y apellido: Carlos López, Mariana Sosa y Gonzalo Pérez,
--         Temporada 2026 con Filas 1..3, Precios de Referencia por calibre
--         y Ajustes de Stock iniciales confirmados).
-- =============================================================================

DROP DATABASE IF EXISTS siga_db;

CREATE DATABASE siga_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE siga_db;
