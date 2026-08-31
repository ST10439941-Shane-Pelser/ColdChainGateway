# Cold-Chain Monitoring Gateway

FreshRoute Logistics telemetry gateway. ASP.NET Core Web API plus a WinForms operator console,
sharing one class library.

## Projects

| Project | Type | What it does |
| --- | --- | --- |
| `ColdChain.Shared` | Class library (net10.0) | Models used by both sides: `TelemetryPacket<T>`, `TemperatureReading`, `Device`, `AnomalyRecord`, `LocationNode` |
| `ColdChain.Api` | ASP.NET Core Web API (net10.0) | The gateway. Seeds and simulates devices, validates registrations, detects anomalies, stores evidence files |
| `ColdChain.Client` | WinForms (net10.0-windows) | The operator console. Talks to the API over HTTP only |

The frontend never touches the API's in-memory collections. Everything goes through `ApiClient.cs`.

## Running it

1. Open `ColdChainGateway.sln` in Visual Studio.
2. Right click the solution, **Configure Startup Projects**, choose **Multiple startup projects**,
   set `ColdChain.Api` to **Start** and `ColdChain.Client` to **Start**. Move the API above the client.
3. Press F5. The API listens on `http://localhost:5165` and the console auto-connects to it on load.

If your machine is on .NET 8 or 9 rather than 10, change `net10.0` and `net10.0-windows` in the three
`.csproj` files to match. Nothing else needs to change.

`ColdChain.Api.http` in the API project has ready-made requests for every endpoint if you want to
test the gateway on its own.

## Where each requirement lives

| Brief | Implementation |
| --- | --- |
| 3.1 Device registration | `Services/DeviceValidator.cs`, `Controllers/DevicesController.RegisterDevice`, `MainForm.BtnRegister_Click` |
| 3.2 Generic `TelemetryPacket<T>` | `Shared/Models/TelemetryPacket.cs`, used as `<double>`, `<int>` and `<bool>` in `Services/TelemetrySimulator.cs` |
| 3.3 Operator overloading | `Shared/Models/TemperatureReading.cs` (`operator +`), folded over a zone in `TelemetrySimulator.CombineZoneTemperatures` |
| 3.4 Jagged array and `List<T>` | `Services/MonitoringZones.cs` (jagged), `Services/GatewayStore.cs` (add, search, filter, display) |
| 3.5 Recursive validation | `Services/LocationTreeService.cs` (`FindByCode`, `BuildPath`, `Flatten`), called from `DeviceValidator.Validate` |
| 3.6 File evidence | `Services/AttachmentRules.cs`, `DevicesController.UploadAttachment`, `ApiClient.UploadEvidenceAsync` (multipart/form-data) |
| 3.7 Anomaly dashboard | `Services/AnomalyDetector.cs`, `Controllers/AnomaliesController.cs`, the Anomalies tab in `MainForm.cs` |
| 4 Endpoints | All six required routes plus `/api/locations` helpers. Full list on the API's landing page |
| 5 Frontend | Three tabs: Devices (register plus evidence), Telemetry (grid, filters, async refresh), Anomalies (acknowledge plus note) |
| 6 Validation | `DeviceValidator`, `AttachmentRules`, `AcknowledgeRequest` checks. Client catches every failure in `ShowError` |

### The three generic types

Every telemetry row carries a `ValueType` column so the marker can see the generic argument in the UI:

- `TelemetryPacket<double>` for temperature (C) and humidity (%)
- `TelemetryPacket<int>` for compressor current (A)
- `TelemetryPacket<bool>` for door open and cooling active

### The operator overload

`TemperatureReading.operator +` returns a new reading whose value is the average of the two,
weighted by how many raw samples each side represents. That weighting is what makes folding a whole
list correct:

```
(4.0 C from 3 samples) + (10.0 C from 1 sample) = 5.5 C from 4 samples
```

`CombineZoneTemperatures` picks the temperature devices out of a row of the jagged zone array, pulls
their latest readings, and folds them with `combined += readings[i]`. The Telemetry tab button
"Combine zone readings with the + operator" shows the result and the arithmetic behind it.

### The location hierarchy

```
FR-NET  FreshRoute Network
├── DEP-JHB  Johannesburg Depot
│   ├── CR-JHB-01  Cold Room 1
│   │   ├── SH-JHB-01A  Shelf A
│   │   └── SH-JHB-01B  Shelf B
│   ├── CR-JHB-02  Cold Room 2 (Frozen)
│   │   ├── SH-JHB-02A
│   │   └── SH-JHB-02B
│   └── VB-JHB-01  Vehicle Bay 1
│       ├── VEH-JHB-114
│       └── VEH-JHB-119
└── DEP-PTA  Pretoria Depot
    ├── CR-PTA-01  Cold Room 1
    │   ├── SH-PTA-01A
    │   └── SH-PTA-01B
    └── VB-PTA-01  Vehicle Bay 1
        └── VEH-PTA-207
```

A location code is only valid for registration if the recursive search finds it **and** the node is a
leaf. A device sits on a shelf or in a vehicle, not on a whole depot, so `DEP-JHB` is rejected with a
different message from a code that does not exist at all.

### Anomaly rules

| Metric | Acceptable | Flagged as |
| --- | --- | --- |
| Temperature, chilled | 2.0 to 8.0 C | Cold-chain breach or freeze risk |
| Temperature, frozen (`SH-JHB-02*`) | -22.0 to -16.0 C | Same, frozen band |
| Humidity | 45 to 85 % | Outside range |
| Compressor current | up to 15 A | Possible seizing compressor |
| Door open | false | Door reported open |
| Cooling active | true | Cooling reported stopped |

The simulator emits a fresh reading for every active device every 5 seconds, with roughly one reading
in twelve deliberately pushed out of range, so the dashboard always has something to acknowledge.

## Demo script

1. Start both projects. The console connects and the grids fill.
2. **Devices tab.** Register `TMP-006`, name "PTA Shelf B Probe", type Temperature, location
   `SH-PTA-01B`. It appears in the grid.
3. Try to break it: register `nope` as an ID, or pick a duplicate ID. The gateway returns every
   validation message at once.
4. Select a device, browse to a JPG or PDF, add a description, upload. The Files column increments.
   Try a `.txt` file to show the rejection.
5. **Telemetry tab.** Point out the ValueType column showing Double, Int32 and Boolean from the one
   generic class. Tick Anomalies only, then tick Auto refresh to show async polling.
6. Pick a zone and press Combine. The label shows the sum and the averaged result from the overloaded
   `+` operator.
7. **Anomalies tab.** Select an unacknowledged row, type your name and a note, acknowledge. Tick
   Include acknowledged to see it come back with your note attached.
8. Stop the API and press Refresh. The console shows a readable connection error instead of crashing.

## Notes

- State is in memory. Restarting the API resets the devices, telemetry and anomalies.
- Evidence files are written to `ColdChain.Api/Uploads/`, created on first upload.
- No NuGet packages are needed, so the solution restores offline. If you want Swagger, add
  `Swashbuckle.AspNetCore` and call `AddSwaggerGen` / `UseSwagger` in `Program.cs`.
- Methods carry a `// Co-authored by Claude` comment per your attribution rule. Review each one and
  keep it only where it is accurate, and swap it out on anything you rewrite yourself.
