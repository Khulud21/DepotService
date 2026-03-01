# DepotService

Eine WPF-Desktop-Anwendung zur Verwaltung und Überwachung von Depot-Beständen mit SQL Server-Anbindung.

## Überblick

DepotService ist eine moderne Windows-Anwendung, die entwickelt wurde, um Depot-Bestände zu verwalten, Statistiken zu überwachen und Job-Verarbeitungen zu steuern. Die Anwendung bietet eine benutzerfreundliche Oberfläche zur Verwaltung von Depot-Artikeln und deren Standorten.

## Hauptfunktionen

- **Depot-Verwaltung**: Vollständige CRUD-Operationen für Depot-Artikel
- **Standort-Filterung**: Filtern von Depot-Artikeln nach verschiedenen Standorten
- **Statistik-Dashboard**: Echtzeit-Übersicht über Depot-Bestände und Gesundheitsstatus
- **Job-Monitoring**: Überwachung und Verwaltung von Verarbeitungsjobs
- **Datenbankverbindung**: Robuste SQL Server-Integration mit Connection-Testing
- **Fehlerbehandlung**: Umfassendes Error-Handling und Logging

## Technologie-Stack

- **.NET 8.0** (WPF Framework)
- **C# 12**
- **SQL Server** (Datenbank)
- **MVVM-Pattern** (Model-View-ViewModel)
- **Microsoft.Data.SqlClient** (Datenbankzugriff)
- **Microsoft.Extensions.Logging** (Logging-Framework)

## Projektstruktur

```
DepotService/
├── App.xaml                    # Anwendungs-Einstiegspunkt
├── App.xaml.cs                 # Anwendungslogik
├── MainWindow.xaml             # Haupt-UI-Layout
├── MainWindow.xaml.cs          # Haupt-UI-Logik
├── appsettings.json            # Konfigurationsdatei
├── Data/
│   └── SqlRepository.cs        # Datenbankzugriffs-Schicht
├── Models/
│   ├── DepotItem.cs           # Depot-Artikel-Modell
│   ├── DepotStatistics.cs     # Statistik-Modell
│   └── JobInfo.cs             # Job-Informations-Modell
└── ViewModels/
    ├── MainViewModel.cs        # Haupt-ViewModel
    └── RelayCommand.cs         # Command-Implementation
```

## Datenmodelle

### DepotItem
Repräsentiert einen Depot-Artikel mit folgenden Eigenschaften:
- `Id`: Eindeutige Kennung
- `Name`: Artikelbezeichnung
- `Quantity`: Bestandsmenge
- `Location`: Lagerort
- `LastUpdated`: Zeitpunkt der letzten Aktualisierung

### DepotStatistics
Bietet statistische Informationen über das Depot:
- `TotalItems`: Gesamtanzahl der Artikel
- `TotalQuantity`: Gesamtmenge aller Artikel
- `LocationCount`: Anzahl verschiedener Standorte
- `AverageQuantity`: Durchschnittliche Menge pro Artikel
- `HealthScore`: Berechneter Gesundheitsstatus (0-100)
- `LastCalculated`: Zeitpunkt der Berechnung

### JobInfo
Verwaltet Job-Informationen:
- `JobId`: Eindeutige Job-ID
- `JobName`: Bezeichnung des Jobs
- `Status`: Aktueller Status (z.B. "Pending", "Running", "Completed")
- `CreatedAt`: Erstellungszeitpunkt
- `UpdatedAt`: Aktualisierungszeitpunkt
- `ErrorMessage`: Fehlermeldung (falls vorhanden)

## Installation

### Voraussetzungen

- Windows 10/11
- .NET 8.0 SDK oder höher
- SQL Server (lokal oder remote)
- Visual Studio 2022 oder höher (empfohlen)

### Schritt 1: Repository klonen

```bash
git clone <repository-url>
cd DepotService
```

### Schritt 2: Datenbankverbindung konfigurieren

Bearbeiten Sie die `appsettings.json` und tragen Sie Ihre SQL Server-Verbindungszeichenfolge ein:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=IHR_SERVER;Database=IHR_DATABASE;User Id=IHR_USER;Password=IHR_PASSWORT;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Schritt 3: Datenbank einrichten

Führen Sie folgende SQL-Skripte in Ihrer Datenbank aus:

```sql
-- Depot-Tabelle erstellen
CREATE TABLE DepotItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(255) NOT NULL,
    Quantity INT NOT NULL,
    Location NVARCHAR(100) NOT NULL,
    LastUpdated DATETIME2 DEFAULT GETDATE()
);

-- Jobs-Tabelle erstellen
CREATE TABLE Jobs (
    JobId INT PRIMARY KEY IDENTITY(1,1),
    JobName NVARCHAR(255) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    ErrorMessage NVARCHAR(MAX) NULL
);

-- Indizes für Performance
CREATE INDEX IX_DepotItems_Location ON DepotItems(Location);
CREATE INDEX IX_Jobs_Status ON Jobs(Status);
```

### Schritt 4: Projekt bauen

```bash
dotnet restore
dotnet build
```

### Schritt 5: Anwendung starten

```bash
dotnet run
```

Oder öffnen Sie die Solution in Visual Studio und drücken Sie F5.

## Verwendung

### Depot-Artikel verwalten

1. **Artikel hinzufügen**: Verwenden Sie die UI, um neue Depot-Artikel mit Name, Menge und Standort hinzuzufügen
2. **Artikel bearbeiten**: Wählen Sie einen Artikel aus und aktualisieren Sie die Details
3. **Artikel löschen**: Entfernen Sie nicht benötigte Artikel aus dem System
4. **Nach Standort filtern**: Nutzen Sie die Standort-Filterung, um spezifische Lagerorte anzuzeigen

### Statistiken überwachen

Das Dashboard zeigt automatisch:
- Gesamtanzahl der Artikel
- Gesamtbestand
- Anzahl der Standorte
- Durchschnittliche Bestandsmenge
- Gesundheitsstatus des Depots

### Jobs verwalten

- **Jobs erstellen**: Neue Verarbeitungsjobs anlegen
- **Status überwachen**: Aktuellen Job-Status in Echtzeit verfolgen
- **Jobs aktualisieren**: Status und Informationen von Jobs ändern

## Datenbankzugriff

Die `SqlRepository`-Klasse bietet folgende Methoden:

### Verbindungsverwaltung
- `TestConnectionAsync()`: Testet die Datenbankverbindung
- `IsConnectionAliveAsync()`: Prüft, ob die Verbindung aktiv ist

### Depot-Operationen
- `GetAllDepotItemsAsync()`: Lädt alle Depot-Artikel
- `GetDepotItemsByLocationAsync(location)`: Lädt Artikel nach Standort
- `GetLocationsAsync()`: Lädt alle verfügbaren Standorte
- `AddDepotItemAsync(item)`: Fügt einen neuen Artikel hinzu
- `UpdateDepotItemAsync(item)`: Aktualisiert einen vorhandenen Artikel
- `DeleteDepotItemAsync(id)`: Löscht einen Artikel

### Statistiken
- `GetDepotStatisticsAsync()`: Berechnet aktuelle Depot-Statistiken

### Job-Verwaltung
- `GetJobByIdAsync(jobId)`: Lädt Job-Informationen
- `CreateJobAsync(jobName)`: Erstellt einen neuen Job
- `UpdateJobStatusAsync(jobId, status, errorMessage)`: Aktualisiert Job-Status

## Fehlerbehandlung

Die Anwendung implementiert umfassendes Error-Handling:

- **Datenbankfehler**: Werden geloggt und mit aussagekräftigen Fehlermeldungen versehen
- **Verbindungsprobleme**: Automatische Erkennung und Benachrichtigung
- **Validierung**: Eingaben werden vor der Verarbeitung validiert
- **Logging**: Alle Operationen werden mit Microsoft.Extensions.Logging protokolliert

## Logging

Die Anwendung verwendet das Microsoft.Extensions.Logging-Framework. Log-Level können in der `appsettings.json` konfiguriert werden:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "DepotService": "Debug"
    }
  }
}
```

## Architektur

Die Anwendung folgt dem **MVVM-Pattern** (Model-View-ViewModel):

- **Models**: Datenstrukturen (DepotItem, JobInfo, DepotStatistics)
- **Views**: XAML-Benutzeroberfläche (MainWindow.xaml)
- **ViewModels**: Präsentationslogik (MainViewModel)
- **Data Access Layer**: Repository-Pattern (SqlRepository)

## Sicherheit

- **Parameterisierte Queries**: Schutz gegen SQL-Injection
- **Verschlüsselte Verbindungen**: TrustServerCertificate-Option konfigurierbar
- **Error-Handling**: Keine sensitiven Daten in Fehlermeldungen
- **Logging**: Passwörter und sensitive Daten werden nicht geloggt

## Performance-Optimierungen

- **Asynchrone Operationen**: Alle Datenbankzugriffe sind async
- **Connection Pooling**: Automatisch durch SqlClient
- **Indizierte Spalten**: Für schnelle Abfragen
- **Lazy Loading**: Daten werden nur bei Bedarf geladen

## Erweiterungsmöglichkeiten

- **Export-Funktionen**: Excel/CSV-Export von Statistiken
- **Barcode-Scanner**: Integration für Lagerverwaltung
- **Benutzerrechte**: Rollen- und Rechtemanagement
- **Audit-Trail**: Nachverfolgung aller Änderungen
- **Reporting**: Erweiterte Berichte und Analysen
- **Multi-Language**: Mehrsprachige Unterstützung

## Fehlerbehebung

### Verbindungsfehler

```
Fehler: Cannot connect to SQL Server
Lösung: Überprüfen Sie die Connection String in appsettings.json
```

### Datenbankfehler

```
Fehler: Invalid object name 'DepotItems'
Lösung: Führen Sie die SQL-Skripte zur Tabellenerstellung aus
```

### Startprobleme

```
Fehler: Application fails to start
Lösung: Stellen Sie sicher, dass .NET 8.0 Runtime installiert ist
```

## Lizenz

Dieses Projekt ist proprietär und darf nicht ohne Genehmigung verwendet werden.

## Kontakt

Für Fragen oder Support kontaktieren Sie bitte das Entwicklungsteam.

## Changelog

### Version 1.0.0 (Aktuell)
- Initiale Version mit Depot-Verwaltung
- SQL Server-Integration
- Standort-Filterung
- Job-Monitoring
- Statistik-Dashboard
- Umfassendes Error-Handling
- Logging-Integration

---

**Hinweis**: Diese Dokumentation wird kontinuierlich aktualisiert. Letzte Aktualisierung: März 2026
