# ITOTShop

*ITOTShop* är mitt projekt för kursmomentet **Automatisering och säkerhetsintegration av ett produktionssystem**.

Tanken med projektet är att simulera ett enklare produktionsflöde där en order skapas på IT-sidan, lagras i en SQL-databas och sedan hämtas av ett integrationsprogram. Integrationsprogrammet skickar ordern vidare till ett simulerat OT-system via Modbus.

Jag har även lagt till autentisering, felhantering, tester och övervakning med Prometheus och Grafana.

## Lösningens flöde

Det grundläggande flödet ser ut så här:

```text
ITOTShop.Console
       |
       v
   SQL Server
       |
       v
ITOTShop.Integration
       |
     Modbus
       |
       v
    OpenPLC
       |
       v
 ITOTShop.OT

ITOTShop.Integration
       |
 OpenTelemetry
       |
       v
   Prometheus
       |
       v
     Grafana
```

`ITOTShop.Console` används för orderhantering och inloggning.

Ordern sparas i SQL Server-databasen `ITOTShopDb` och hämtas sedan av `ITOTShop.Integration`. Integrationsprogrammet fungerar som bryggan mellan IT- och OT-sidan och är det enda programmet som kommunicerar direkt med PLC:n.

OpenPLC används för att simulera produktionslogiken.

Prometheus samlar in metrics från integrationsprogrammet och Grafana används för att visa produktionsstatus och statistik.

## Projekt i solutionen

### ITOTShop.Console

Console-applikationen representerar IT-systemet.

Här kan användaren bland annat:

- registrera användare
- logga in
- skapa ordrar
- lista ordrar

Användare lagras i databasen och lösenorden lagras hashade.

### ITOTShop.Integration

Integrationsprogrammet kopplar samman IT- och OT-sidan.

Programmet:

- läser ordrar från `ITOTShopDb`
- skickar orderdata till OpenPLC via Modbus
- läser tillbaka produktionsstatus
- uppdaterar orderstatus i databasen
- hanterar kommunikationsfel
- exponerar metrics med OpenTelemetry

En order kan bland annat ha status:

```text
Pending
Processing
Completed
Failed
```

Om en order redan står som `Processing` när integrationsprogrammet startas igen hämtas den före nya `Pending`-ordrar. Detta minskar risken att en order blir kvar i ett mellanläge efter exempelvis ett avbrott.

### ITOTShop.OT

OT-projektet används som ett enkelt övervakningsprogram för OT-sidan.

Själva produktionslogiken körs i OpenPLC och kommunikation mellan integrationsprogrammet och PLC:n sker med Modbus TCP.

### ITOTShop.Tests

Testprojektet innehåller tester för flera delar av lösningen.

#### Console / Authentication

Tester för `AuthService` kontrollerar bland annat:

- registrering av användare
- inloggning
- kontroll av användarnamn
- hashade lösenord
- felaktiga inloggningsuppgifter

Totalt finns **6 tester** för autentiseringsdelen.

#### Integration / OrderDataAccess

Tester för `OrderDataAccess` kontrollerar bland annat:

- att rätt order hämtas för behandling
- att orderstatus kan uppdateras
- att färdigbehandlade ordrar ignoreras
- att `null` returneras när ingen order kan behandlas
- statusändring från `Processing` till `Completed`
- statusändring från `Processing` till `Failed`

Totalt finns **6 tester** för integrationsdelen.

#### OT / PlcStatisticsService

Tester för OT-delen kontrollerar logiken i `PlcStatisticsService` och hanteringen av produktionsdata från PLC-systemet.

Totalt finns **6 tester** för OT-delen.

Samtliga **18 tester** går igenom vid senaste kontrollen.

### ITOTShop.Web

Ett webbprojekt finns också i solutionen men ingår inte i huvudflödet för den här inlämningen.

## Databas

Projektet använder SQL Server och Entity Framework Core.

Databasen heter:

```text
ITOTShopDb
```

## Tekniker

Projektet är byggt med:

- C#
- .NET 10
- Visual Studio
- Entity Framework Core
- SQL Server
- NModbus
- OpenPLC (Runtime & editor)
- OpenTelemetry
- Prometheus
- Grafana
- Docker / Docker Compose
- xUnit
- Git och GitHub

## IT/OT-separering

En viktig del i lösningen har varit att hålla IT- och OT-sidan separerade.

Orderhanteringen kommunicerar inte direkt med PLC:n.

I stället går kommunikationen genom `ITOTShop.Integration`:

```text
Order system -> ITOTShopDb -> Integration -> PLC
```

Det innebär att integrationsprogrammet fungerar som en kontrollerad gräns mellan systemen.

Orderdata valideras även i PLC-logiken innan produktionen startas.

## Monitoring

Prometheus och Grafana körs med Docker Compose.

Filerna finns under:

```text
Monitoring/
├── docker-compose.yml
├── prometheus-config.yml
└── Grafana/
    ├── DashboardFiles/
    └── Provisioning/
```

Integrationsprogrammet exponerar *metrics* på:

```text
http://localhost:9464/metrics
```

*Prometheus* finns på:

```text
http://localhost:9090
```

*Grafana* finns på:

```text
http://localhost:3000
```

Grafana-dashboarden visar bland annat:
*
- PLC Online / Offline
- Production Status
- Live Produced Quantity
- Produced Units
- Completed Orders
- Failed Orders
*

## Starta projektet

### 1. Monitoring

Öppna terminalen i:

```text
Monitoring
```

och kör:

```text
docker compose up -d
```

Det startar Prometheus och Grafana.

### 2. OpenPLC

Starta OpenPLC Runtime och starta PLC-programmet.

Modbus-kommunikationen använder:

```text
127.0.0.1:5020
```

### 3. Visual Studio

För ett komplett test kan följande projekt startas samtidigt med **Multiple startup projects**:

```text
ITOTShop.Console
ITOTShop.Integration
ITOTShop.OT
```

En order kan därefter skapas i Console-applikationen.

Integrationsprogrammet hämtar ordern automatiskt från `ITOTShopDb` och skickar den vidare till PLC:n.

## Projektstruktur

Jag har försökt hålla projekten uppdelade efter ansvar, exempelvis:

```text
Data/
DataAccess/
DTO/
Entities/
Enums/
Interfaces/
Metrics/
Services/
```

Syftet är att undvika att databasåtkomst, affärslogik och kommunikation blandas ihop i samma klasser.

## Versionshantering

Projektet versionshanteras med Git och GitHub.
