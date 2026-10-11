CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `MarketIndices` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `Value` decimal(18,2) NOT NULL,
        `Change` decimal(18,2) NOT NULL,
        `ChangePct` decimal(9,4) NOT NULL,
        `SortOrder` int NOT NULL,
        CONSTRAINT `PK_MarketIndices` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Stocks` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Symbol` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `Price` decimal(18,2) NOT NULL,
        `ChangePct` decimal(9,4) NOT NULL,
        `Volume` bigint NOT NULL,
        `Low` decimal(18,2) NOT NULL,
        `High` decimal(18,2) NOT NULL,
        `SortOrder` int NOT NULL,
        CONSTRAINT `PK_Stocks` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Users` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
        `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
        `ClientCode` varchar(30) CHARACTER SET utf8mb4 NULL,
        `Role` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Verified` tinyint(1) NOT NULL,
        `PasswordHash` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `LedgerEntries` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Date` date NOT NULL,
        `Description` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
        `Type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Amount` decimal(18,2) NOT NULL,
        CONSTRAINT `PK_LedgerEntries` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_LedgerEntries_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `PaymentInfos` (
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `QrImagePath` varchar(300) CHARACTER SET utf8mb4 NULL,
        `UpiId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Note` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_PaymentInfos` PRIMARY KEY (`UserId`),
        CONSTRAINT `FK_PaymentInfos_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Portfolios` (
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `LedgerBalance` decimal(18,2) NOT NULL,
        `MarginAvailable` decimal(18,2) NOT NULL,
        `MarginUsed` decimal(18,2) NOT NULL,
        `M2M` decimal(18,2) NOT NULL,
        `TodayPnl` decimal(18,2) NOT NULL,
        `Message` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_Portfolios` PRIMARY KEY (`UserId`),
        CONSTRAINT `FK_Portfolios_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Positions` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Symbol` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `Side` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` decimal(18,4) NOT NULL,
        `AvgPrice` decimal(18,2) NOT NULL,
        `Ltp` decimal(18,2) NOT NULL,
        `Pnl` decimal(18,2) NOT NULL,
        `SortOrder` int NOT NULL,
        CONSTRAINT `PK_Positions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Positions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Trades` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Symbol` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
        `Side` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `OrderType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Quantity` decimal(18,4) NOT NULL,
        `EntryPrice` decimal(18,2) NOT NULL,
        `ExitPrice` decimal(18,2) NULL,
        `Pnl` decimal(18,2) NOT NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Date` datetime(6) NOT NULL,
        CONSTRAINT `PK_Trades` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Trades_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE TABLE `Withdrawals` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Amount` decimal(18,2) NOT NULL,
        `AccountDetails` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `AdminNote` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Withdrawals` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Withdrawals_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE INDEX `IX_LedgerEntries_UserId_Date` ON `LedgerEntries` (`UserId`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE INDEX `IX_Positions_UserId_SortOrder` ON `Positions` (`UserId`, `SortOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE INDEX `IX_Trades_UserId_Status_Date` ON `Trades` (`UserId`, `Status`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Users_ClientCode` ON `Users` (`ClientCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    CREATE INDEX `IX_Withdrawals_UserId_CreatedAt` ON `Withdrawals` (`UserId`, `CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261010134409_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261010134409_InitialCreate', '9.0.20');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261011093852_CommonPaymentSettings') THEN

    CREATE TABLE `PaymentSettings` (
        `Id` int NOT NULL,
        `QrImagePath` varchar(300) CHARACTER SET utf8mb4 NULL,
        `UpiId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `BankName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `AccountHolderName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `AccountNumber` varchar(34) CHARACTER SET utf8mb4 NOT NULL,
        `IfscCode` varchar(11) CHARACTER SET utf8mb4 NOT NULL,
        `Note` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_PaymentSettings` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261011093852_CommonPaymentSettings') THEN

    DROP TABLE `PaymentInfos`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261011093852_CommonPaymentSettings') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261011093852_CommonPaymentSettings', '9.0.20');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

