# Veltrex API

The ASP.NET Core (.NET 10) Web API behind the Veltrex React app. It uses EF Core code-first on **MySQL** (Pomelo provider), JWT auth, and BCrypt password hashing.

## Run

```bash
dotnet run --launch-profile http
```

- API: http://localhost:5165/api
- Swagger: http://localhost:5165/swagger. Call `POST /api/auth/login`, click **Authorize**, then paste the token.

On startup the API creates the database if it doesn't exist and applies any pending migrations. It then seeds the first admin (`admin@veltrex.com` / `admin123`, set under `Seed` in `appsettings.json`) and sample market data. **Change the admin password before going live.**

The React app (`D:\Harendra\Veltrex`) reads the API address from `.env.development.local`:

```
VITE_API_BASE_URL=http://localhost:5165/api
VITE_USE_MOCK=false
```

## Configuration (`appsettings.json`)

| Key | Purpose |
| --- | ------- |
| `ConnectionStrings:Default` | MySQL connection string (see below). |
| `Database:MySqlVersion` | Your MySQL server version, e.g. `8.0.36-mysql` (or `10.11.0-mariadb`). Run `SELECT VERSION();` to check. |
| `Database:MigrateOnStartup` | `true` creates the database and applies migrations automatically. Set it to `false` if you run the SQL script yourself. |
| `Jwt:Key` | Token signing secret, at least 32 characters. The dev key is in `appsettings.Development.json`. In production, set the `Jwt__Key` environment variable. |

### Connection string (MySQL on AWS EC2)

Set it in `appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=65.0.87.56;Port=3306;Database=VeltrexDb;User=veltrex;Password=YOUR_PASSWORD;"
}
```

The file contains the database password, so don't commit it to a shared or public repository.

### One-time EC2 / MySQL setup

1. **Security group.** In EC2, go to your instance → Security → Security group → Inbound rules. Add MySQL/Aurora (TCP 3306), with your IP (`x.x.x.x/32`) and the API server's IP as sources. Avoid `0.0.0.0/0`.
2. **MySQL listens on the network.** In `/etc/mysql/mysql.conf.d/mysqld.cnf` (Ubuntu) or `/etc/my.cnf`, set `bind-address = 0.0.0.0`, then run `sudo systemctl restart mysql`.
3. **Remote user.** `root` is usually allowed from `localhost` only. Create an app user instead:
   ```sql
   CREATE USER 'veltrex'@'%' IDENTIFIED BY 'a-strong-password';
   GRANT ALL PRIVILEGES ON VeltrexDb.* TO 'veltrex'@'%';
   GRANT CREATE ON *.* TO 'veltrex'@'%';   -- lets the API create VeltrexDb the first time
   FLUSH PRIVILEGES;
   ```
4. **Test from your PC:** `Test-NetConnection 65.0.87.56 -Port 3306` should show `TcpTestSucceeded : True`.

## Database: code-first migrations

The entity classes in `Entities/` and the table setup in `Data/AppDbContext.cs` define the schema, and `Data/Migrations/` holds the generated migrations. The project pins `dotnet-ef` 9 as a local tool in `dotnet-tools.json`; run `dotnet tool restore` once.

```bash
# after changing an entity:
dotnet tool run dotnet-ef migrations add <Name> -o Data/Migrations

# apply to the database (or just start the API):
dotnet tool run dotnet-ef database update
```

### The SQL script

`Database/VeltrexDb.sql` is the whole schema in one plain MySQL file, for setting the database up by hand:

- It has no `DELIMITER` or stored procedures, so it runs in any client: MySQL Workbench, DBeaver, phpMyAdmin, HeidiSQL, or `mysql -h 65.0.87.56 -u veltrex -p VeltrexDb < Database/VeltrexDb.sql`.
- Every table uses `CREATE TABLE IF NOT EXISTS`, so it's safe to run more than once. Use the same file for a new database and for upgrading an existing one; only missing tables are created.
- The last statement records every EF migration in `__EFMigrationsHistory`, so the API won't try to create the tables again on startup.

For a new database, run `CREATE DATABASE VeltrexDb CHARACTER SET utf8mb4;` and `USE VeltrexDb;` first.

The file is maintained by hand. Don't regenerate it with `dotnet-ef migrations script`, which produces the `DELIMITER` format most clients reject. When you add a migration, add its `CREATE TABLE IF NOT EXISTS` (or other change) to the file, plus its migration ID in the final `INSERT IGNORE`.

The script creates every table:

| Table | Holds |
| ----- | ----- |
| `Users` | Admins and clients: email or User ID login, role, verification, BCrypt password hash |
| `Portfolios` | One row per client: ledger balance, margins, M2M, today's P&L, announcement |
| `Positions` | A client's open net positions |
| `Trades` | Pending, active and closed trades |
| `LedgerEntries` | Credits and debits |
| `PaymentSettings` | Single row shared by all clients: UPI QR image path, UPI ID, bank name, account holder, account number, IFSC, note |
| `Withdrawals` | Withdrawal requests and the admin's decision |
| `KycDetails` | One row per client once they submit KYC: personal details, address, bank account, Aadhaar and PAN, review status (`Submitted`, `Approved`, `Rejected`) and admin note |
| `PaymentProofs` | Deposit screenshots clients upload after paying. The image bytes are stored in the row (`MEDIUMBLOB`, max 5 MB), with the uploader, optional amount and UTR reference, review status (`Pending`, `Approved`, `Rejected`) and admin note |
| `SupportMessages` | Support chat: one conversation per client (`UserId`). `FromAdmin` marks replies from the support team, `SenderId` is who sent it, `ReadAt` is when the other side read it. Ids auto-increment so the apps can poll for new messages |
| `MarketIndices`, `Stocks` | Market watch data shared by all clients |

## Endpoints

All routes are under `/api`. Errors are always returned as `{ "message": "..." }`. Enum values are UPPER_CASE strings (`BUY`, `ACTIVE`, `CREDIT`, ...).

| Method | Path | Who |
| ------ | ---- | --- |
| POST | `/auth/login` (`{ username, password }`, where username is the email or User ID) | anyone |
| POST | `/auth/register` (`{ name, email, phone?, password }`; creates an unverified client and returns `{ token, user }`) | anyone |
| GET | `/auth/me` | logged in |
| GET | `/market` | logged in |
| PUT | `/admin/market` | admin |
| GET | `/payment` (deposit details shown to every client) | logged in |
| PUT | `/admin/payment` (multipart: `upiId`, `bankName`, `accountHolderName`, `accountNumber`, `ifscCode`, `note`, `qrImage`) | admin |
| GET | `/client/portfolio`, `/client/trades`, `/client/ledger`, `/client/withdrawals` | client |
| POST | `/client/withdrawals`, `/client/change-password` | client |
| GET, POST | `/admin/users` | admin |
| GET, PUT | `/admin/users/{id}` | admin |
| GET, PUT | `/admin/users/{id}/portfolio`, `/admin/users/{id}/ledger` | admin |
| GET, POST | `/admin/users/{id}/trades` | admin |
| PUT, DELETE | `/admin/users/{id}/trades/{tradeId}` | admin |
| GET | `/admin/users/{id}/withdrawals` | admin |
| PUT | `/admin/users/{id}/withdrawals/{withdrawalId}` (`{ status, adminNote }`) | admin |
| GET, PUT | `/client/kyc` (GET returns 204 until submitted; PUT submits or re-submits, refused once approved) | client |
| GET, PUT | `/admin/users/{id}/kyc` (admin can fill in or correct details; the review status is kept) | admin |
| PUT | `/admin/users/{id}/kyc/status` (`{ status, adminNote }`; `APPROVED` also marks the client verified, `REJECTED` un-verifies) | admin |

| GET, POST | `/client/payment-proofs` (POST is multipart: `image` (PNG/JPG/WEBP/GIF, max 5 MB), `amount?`, `reference?`) | client |
| GET | `/client/payment-proofs/{proofId}/image` | client |
| GET | `/admin/payment-proofs` (`?status=PENDING` for the review queue) | admin |
| GET | `/admin/users/{id}/payment-proofs`, `/admin/users/{id}/payment-proofs/{proofId}/image` | admin |
| PUT | `/admin/users/{id}/payment-proofs/{proofId}` (`{ status, adminNote }`; approving doesn't credit the ledger) | admin |

| GET, POST | `/client/support/messages` (GET `?afterId=` returns only newer messages; POST `{ body }`) | client |
| POST | `/client/support/read` (marks support replies read) | client |
| GET | `/client/support/unread` → `{ count }` | client |
| GET | `/admin/support/conversations` (inbox: last message + unread count per client) | admin |
| GET | `/admin/support/unread` → `{ count }` (all clients) | admin |
| GET, POST | `/admin/users/{id}/support/messages` (same as the client's) | admin |
| POST | `/admin/users/{id}/support/read` | admin |

Support thread responses are `{ messages, readUpTo }`, where `readUpTo` is the highest id of the caller's own messages the other side has read (for "Seen" markers). The apps poll every few seconds; there are no websockets.

Screenshot images are only served through those authenticated `/image` endpoints, so the frontend fetches them with the token rather than using a plain `<img src>`.

Every user object includes `kycStatus`: `null` until KYC is submitted, then `SUBMITTED`, `APPROVED` or `REJECTED`.

Uploaded QR images are stored in `wwwroot/uploads/qr/` and served at `/uploads/qr/...`.
