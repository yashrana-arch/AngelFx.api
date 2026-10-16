-- =====================================================================
-- Veltrex database: the complete schema in one script.
--
-- * Plain MySQL 8 with no DELIMITER or stored procedures, so it runs in any
--   client (MySQL Workbench, DBeaver, phpMyAdmin, HeidiSQL, the mysql CLI).
-- * Safe to run more than once. Every table uses CREATE TABLE IF NOT EXISTS,
--   so on an existing database only the missing tables are created.
-- * Use the same file for a brand-new database and for upgrading an old one.
--
-- How to run:
--   1. New database only:  CREATE DATABASE VeltrexDb CHARACTER SET utf8mb4;
--   2. Select it:          USE VeltrexDb;
--   3. Run this whole script.
--
-- The last section records the EF Core migrations as applied, so the API
-- doesn't try to create these tables again when it starts.
-- =====================================================================

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

-- ---------------------------------------------------------------------
-- Users: admins and clients
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `ClientCode` varchar(30) CHARACTER SET utf8mb4 NULL,
    `Role` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Verified` tinyint(1) NOT NULL,
    `PasswordHash` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Users_Email` (`Email`),
    UNIQUE INDEX `IX_Users_ClientCode` (`ClientCode`)
) CHARACTER SET=utf8mb4;

-- ---------------------------------------------------------------------
-- Per-client data
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Portfolios` (
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

CREATE TABLE IF NOT EXISTS `Positions` (
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
    INDEX `IX_Positions_UserId_SortOrder` (`UserId`, `SortOrder`),
    CONSTRAINT `FK_Positions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Trades` (
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
    INDEX `IX_Trades_UserId_Status_Date` (`UserId`, `Status`, `Date`),
    CONSTRAINT `FK_Trades_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `LedgerEntries` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Date` date NOT NULL,
    `Description` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Type` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Amount` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_LedgerEntries` PRIMARY KEY (`Id`),
    INDEX `IX_LedgerEntries_UserId_Date` (`UserId`, `Date`),
    CONSTRAINT `FK_LedgerEntries_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Withdrawals` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Amount` decimal(18,2) NOT NULL,
    `AccountDetails` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `AdminNote` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Withdrawals` PRIMARY KEY (`Id`),
    INDEX `IX_Withdrawals_UserId_CreatedAt` (`UserId`, `CreatedAt`),
    CONSTRAINT `FK_Withdrawals_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- KYC details: one row per client once they submit the KYC form.
CREATE TABLE IF NOT EXISTS `KycDetails` (
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `FirstName` varchar(75) CHARACTER SET utf8mb4 NOT NULL,
    `LastName` varchar(75) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `DateOfBirth` date NOT NULL,
    `AddressLine` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `City` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `State` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Nationality` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
    `BankName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `AccountHolderName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `AccountNumber` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IfscCode` varchar(11) CHARACTER SET utf8mb4 NOT NULL,
    `AadhaarNumber` varchar(12) CHARACTER SET utf8mb4 NOT NULL,
    `PanNumber` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `AdminNote` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `SubmittedAt` datetime(6) NOT NULL,
    `ReviewedAt` datetime(6) NULL,
    CONSTRAINT `PK_KycDetails` PRIMARY KEY (`UserId`),
    INDEX `IX_KycDetails_Status` (`Status`),
    CONSTRAINT `FK_KycDetails_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- Payment screenshots clients upload after depositing. The image is stored in the row (max 5 MB).
CREATE TABLE IF NOT EXISTS `PaymentProofs` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ImageData` mediumblob NOT NULL,
    `ContentType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `FileName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `FileSize` int NOT NULL,
    `Amount` decimal(18,2) NULL,
    `Reference` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `AdminNote` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ReviewedAt` datetime(6) NULL,
    CONSTRAINT `PK_PaymentProofs` PRIMARY KEY (`Id`),
    INDEX `IX_PaymentProofs_Status_CreatedAt` (`Status`, `CreatedAt`),
    INDEX `IX_PaymentProofs_UserId_CreatedAt` (`UserId`, `CreatedAt`),
    CONSTRAINT `FK_PaymentProofs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- ---------------------------------------------------------------------
-- Shared by all clients
-- ---------------------------------------------------------------------

-- Deposit details (single row, Id = 1): UPI QR + bank account.
CREATE TABLE IF NOT EXISTS `PaymentSettings` (
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

CREATE TABLE IF NOT EXISTS `MarketIndices` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Name` varchar(60) CHARACTER SET utf8mb4 NOT NULL,
    `Value` decimal(18,2) NOT NULL,
    `Change` decimal(18,2) NOT NULL,
    `ChangePct` decimal(9,4) NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_MarketIndices` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Stocks` (
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

-- Old per-client deposit table, replaced by PaymentSettings (migration CommonPaymentSettings).
DROP TABLE IF EXISTS `PaymentInfos`;

-- ---------------------------------------------------------------------
-- Mark every EF Core migration as applied (skips ones already recorded).
-- When a new migration is added, add its tables above and a row here.
-- ---------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
    ('20261010134409_InitialCreate', '9.0.20'),
    ('20261011093852_CommonPaymentSettings', '9.0.20'),
    ('20261015125527_AddKycDetails', '9.0.20'),
    ('20261015153639_AddPaymentProofs', '9.0.20');
