-- Crea DB si no existe
IF DB_ID('$(APP_DB)') IS NULL
BEGIN
  PRINT 'Creating database $(APP_DB)...';
  EXEC('CREATE DATABASE [' + '$(APP_DB)' + ']');
END
GO

-- Crea login si no existe
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = '$(APP_LOGIN)')
BEGIN
  PRINT 'Creating login $(APP_LOGIN)...';
  DECLARE @sql NVARCHAR(MAX) = N'CREATE LOGIN [' + '$(APP_LOGIN)' + N'] WITH PASSWORD = ''' + '$(APP_PASSWORD)' + N''', CHECK_POLICY = OFF;'
  EXEC(@sql);
END
GO

-- Crea usuario en la DB y asigna roles
USE [$(APP_DB)];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$(APP_LOGIN)')
BEGIN
  PRINT 'Creating user in DB $(APP_DB)...';
  EXEC('CREATE USER [' + '$(APP_LOGIN)' + '] FOR LOGIN [' + '$(APP_LOGIN)' + '];');
END
GO

-- Permisos: db_datareader + db_datawriter
EXEC sp_addrolemember 'db_datareader', '$(APP_LOGIN)';
EXEC sp_addrolemember 'db_datawriter', '$(APP_LOGIN)';
GO
