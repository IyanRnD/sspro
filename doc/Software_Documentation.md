# SSPRO TCP Server v2 — Software Documentation

| Item | Value |
|---|---|
| Project | `TCPServer v2/TCPServer.sln` |
| Type | Windows Forms tray application (C#) |
| Framework | .NET Framework 4.7.1 |
| Version | 2.1 (per change log in `Form1.cs`) |
| Databases | Microsoft SQL Server (central `FuelStation`), MySQL (`dbss` on each site) |
| Document date | 2026-10-26 |

---

## 1. Purpose

SSPRO TCP Server is the central integration service of a fuel-management system. It:

1. **Receives fuel transactions** from site terminals (SSPRO controllers) over TCP.
2. **Stores** each transaction in the central SQL Server table `CardTransaction`.
3. **Recalculates card consumption totals** (day / month / year volume and money, last odometer) for the driver tag and vehicle tag in `twaCard`.
4. **Synchronises updated cards back to every site**: a background worker per site pushes changed `twaCard` rows into the site's MySQL table `tbWaCard`, so limits are enforced at the pump.

---

## 2. Architecture

```
 ┌──────────────┐  TCP (port 9103)   ┌───────────────────────────────┐
 │ Site terminal├───────────────────►│  Server (CServer.cs)          │
 │  (SSPRO)     │◄─── [OK]/[NOT] ────┤  thread per client            │
 └──────────────┘                    └──────────────┬────────────────┘
                                                    │ OnStringReceived
                                     ┌──────────────▼────────────────┐
                                     │ Form1.OnReceive               │
                                     │  DataSetParser (Parser.cs)    │
                                     │  CardTransactionManager       │
                                     │   (clsDBTranscation.cs)       │
                                     └──────────────┬────────────────┘
                                                    │ ADO.NET
                                     ┌──────────────▼────────────────┐
                                     │ SQL Server  FuelStation       │
                                     │  CardTransaction, twaCard,    │
                                     │  TrxSyncCardData, Sites       │
                                     └──────────────┬────────────────┘
                                                    │ poll every 10 s
                                     ┌──────────────▼────────────────┐
 ┌──────────────┐   MySQL 3306       │ CardUpdater (one per site)    │
 │ Site MySQL   │◄───────────────────┤  (CardUpdater.cs)             │
 │ dbss.tbWaCard│                    └───────────────────────────────┘
 └──────────────┘
```

### 2.1 Source files

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point; starts `Form1`. |
| `Form1.cs` / `Form1.Designer.cs` | Startup (config, license, DB check), starts the TCP server and card updaters, handles received messages, UI log, tray icon. |
| `CServer.cs` | `Server` class: `TcpListener`, accepts clients, one thread per client, raises `OnStringReceived` / `OnLogAdd`; `Response`, `BroadCast`, `CloseClient`. |
| `Parser.cs` | `DataField` (45-field transaction model) and `DataSetParser` (validation + parsing of the `-:-` delimited message). |
| `clsDBTranscation.cs` | `SQLConnectionChecker`; `CardTransactionManager` – insert/merge transaction, recompute driver/vehicle totals, mark cards for sync. |
| `CardUpdater.cs` | `SSPROConfig` (loads `Sites`), `CardUpdater` (per-site state machine pushing pending cards to MySQL). |
| `Encryptor.cs` | `AESq` – encrypts/decrypts the connection string stored in `config.ini`. |
| `IniFile.cs` | Win32 INI file wrapper. |
| `MachineInfo.cs` | Hardware IDs (CPU, motherboard, volume serial) and product-key generation/validation. |
| `EventLogger.cs` | Appends log lines to `Logs/yyyy-MM-dd.txt`. |

---

## 3. Startup sequence (`Form1_Load`)

1. Read hardware IDs (CPU ID, baseboard serial, fixed-disk volume serial).
2. Open/create `config.ini` (working directory) and default missing keys.
3. If `[SQLServer] EncryptConn` is missing, write an encrypted default connection string.
4. Decrypt the connection string and test it (`SQLConnectionChecker`).
5. Validate `[License] ProductKey` against the hardware IDs.
6. If the license is valid:
   - start the TCP `Server` on `[Server] IPAddress:Port` in a new thread;
   - load sites from `Sites` and start one `CardUpdater` per site.
7. Minimise to the system tray.

If the license is invalid, only `[Invalid License Code]` is logged; nothing is started.

---

## 4. Configuration — `config.ini`

```ini
[APP]
type=1                     ; application type, part of the license hash

[Server]
Port=9103
IPAddress=127.0.0.1        ; listen address (use 0.0.0.0 for all interfaces)

[SQLServer]
EncryptConn=<base64>       ; AES-encrypted SQL Server connection string

[License]
MachineID=[cpu,board,disk] ; written automatically, send to vendor
ProductKey=<25 hex chars>
```

> **Note:** the site MySQL connection (`Server=<site IP>; Database=dbss; …`) is currently built in code from `Sites.IPAddress`; it is not configurable (see bug tracker).

### 4.1 Licensing
`ProductKey = first 25 hex chars of MD5(APPtype + CpuId + MotherboardId + DiskId)`.
`MachineInfo.GenerateProductKey` / `GetProductID(APP, "[a,b,c]")` produce it from the `MachineID` value in the INI.

---

## 5. TCP protocol

### 5.1 Request
One ASCII message, 45 fields separated by the literal delimiter **`-:-`**.

| # | Field | Type | | # | Field | Type |
|---|---|---|---|---|---|---|
| 0 | Header | string | | 23 | VehicleTag | string |
| 1 | Seq | int | | 24 | Odometer | int |
| 2 | CompanyID | string | | 25 | EngineHour | int |
| 3 | SiteID | string | | 26 | TransferTo | string |
| 4 | TerminalID | int | | 27 | TankID | int |
| 5 | TransNo | int | | 28 | TranType | string |
| 6 | TransCompleted | datetime | | 29 | TranMode | string |
| 7 | PumpID | int | | 30 | Loc_Longitude | string |
| 8 | NozzleID | int | | 31 | Loc_Latitude | string |
| 9 | ProductID | int | | 32 | OverridePurpose | string |
| 10 | ProductName | string | | 33 | TankReadingBefore_H | decimal |
| 11 | Quantity | decimal | | 34 | TankReadingBefore_V | decimal |
| 12 | UnitPrice | decimal | | 35 | TankReadingAfter_H | decimal |
| 13 | Amount | decimal | | 36 | TankReadingAfter_V | decimal |
| 14 | Tot_Amount | decimal | | 37 | TranSignature | string |
| 15 | Tot_Volume | decimal | | 38 | PostedToServer | string |
| 16 | Cal_Tot_Amount | decimal | | 39 | PostedToFuelokey | string |
| 17 | Cal_Tot_Volume | decimal | | 40 | ExportedToUSB | string |
| 18 | MOP | string | | 41 | PrintCount | int |
| 19 | MOPCardID | string | | 42 | JobOrderID | string |
| 20 | SupervisorTag | string | | 43 | CompartemenID | int |
| 21 | AttendantTag | string | | 44 | SynchToAX | string |
| 22 | CustomerTag (driver) | string | | | | |

Decimals are parsed with `InvariantCulture` (dot as decimal separator); invalid numbers become `0`.

### 5.2 Response
| Case | Reply |
|---|---|
| Valid & stored | `<Header>-:-<Seq>-:-<SiteID>-:-[OK] \r\n` |
| Field count ≠ 45 | `[NOT] \r\n` |
| DB / date error | *no reply* (logged only) |

---

## 6. Transaction processing (`Form1.OnReceive`)

1. Validate field count, parse into `DataField`.
2. `InsertOrUpdateCardTransaction` — `MERGE` into `dbo.CardTransaction` on key
   (CompanyId, SiteId, TerminalId, BusinessDate, TransNo, PumpId). Re-sent transactions update instead of duplicating.
3. `UpdateDriverTag(CustomerTag)` and `UpdateVehicleTag(VehicleTag)`:
   - sum `Quantity`/`Amount` for today, this month, this year;
   - latest odometer;
   - `UPDATE twacard` totals;
   - `ModifiedCardNumber` → `MERGE TrxSyncCardData` for **every** site with `SychStatus = 0` (pending).
4. Reply `[OK]`.

---

## 7. Card synchronisation (`CardUpdater`)

State machine per site (background thread):

| State | Action | Next |
|---|---|---|
| 0 Init | log | 3 |
| 3 Check MySQL | open/close connection | ok → 1, fail → 4 |
| 4 Back-off | sleep 10 × interval (100 s) | 3 |
| 1 Sync | select cards where `TrxSyncCardData.SiteID = site AND SychStatus IN (0,-1)`; for each: `INSERT … ON DUPLICATE KEY UPDATE tbWaCard` | 2 |
| 2 Wait | sleep interval (10 s) | 1 |

Result per card: success → `SychStatus = 1`; failure → `SychStatus = -1`, `SychRetryCount + 1`.

---

## 8. Database objects used

**SQL Server (FuelStation)**

| Table | Use |
|---|---|
| `Sites` | `SiteID, SiteName, IPAddress` – list of sites to sync. |
| `CardTransaction` | All fuel transactions. |
| `twaCard` | Card master (limits and running totals). |
| `TrxSyncCardData` | `SiteID, CardNumber, SychStatus (0 pending / 1 done / -1 failed), SychRetryCount, SyncDateTime`. |

**MySQL (site `dbss`)**

| Table | Use |
|---|---|
| `tbWaCard` | Site copy of `twaCard` used by the pump controller. |

---

## 9. Logging
- UI text box (cleared after 1000 lines; *Auto Scroll* checkbox).
- File: `Logs/yyyy-MM-dd.txt` relative to the working directory, one line per event with timestamp.

---

## 10. Build & deployment

1. Open `TCPServer v2/TCPServer.sln` in Visual Studio 2017+ (.NET Framework 4.7.1 targeting pack).
2. `MySql.Data.dll` (8.0.18) is in the project folder. Remove or provide the unused references `NetToolKit`, `SKCLNET`, `SymbolFactory*` if the build fails.
3. Build *Release*; copy `TCPServer.exe`, `MySql.Data.dll`, `TCPServer.exe.config` to the server.
4. Run once to create `config.ini`; send `MachineID` to obtain a `ProductKey`; set the connection string; restart.
5. Allow inbound TCP on the configured port; the server needs outbound MySQL (3306) to each site IP.

---

## 11. Known issues
See [`SSPRO_Bug_Tracker.xlsx`](SSPRO_Bug_Tracker.xlsx) — 47 findings (4 Critical, 16 High, 19 Medium, 8 Low). Most urgent:

1. Credentials hardcoded in source — rotate passwords.
2. No TCP message framing — split/merged packets cause rejected transactions.
3. `TotalMonthMoney` is written with the volume value (driver and vehicle).
4. Card totals use the server clock instead of the transaction's business date; culture-dependent date strings.
5. SQL built by string concatenation (SQL injection) in daily query and MySQL sync.
6. A single exception silently stops a site's card-sync thread.
