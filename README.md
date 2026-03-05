# DepotService

Eine WPF-Desktop-Anwendung zur Verwaltung und Überwachung von Empirum-Depots mit intelligenter Filterung und Job-Steuerung.

## Überblick

DepotService ist eine moderne Windows-Anwendung zur Verwaltung von Empirum-Depots. Die Anwendung bietet eine übersichtliche Oberfläche zur Überwachung von Computer-Depots, deren Synchronisationsstatus und ermöglicht die gezielte Verteilung von Software-Paketen über Empirum-Jobs.

## Hauptfunktionen

- **Depot-Übersicht**: Übersichtliche Darstellung aller Empirum-Depots mit Computer-Namen, Domänen und Status
- **Intelligente Filterung**:
  - Filterung nach Standorten (Domänen)
  - Filterung nach Computer-Namen
  - Volltextsuche über Computer und Domäne
- **Status-Überwachung**: Echtzeit-Anzeige des Job-Status mit visuellen Indikatoren
  - ⏳ Pending (Wartend)
  - 🔄 Running (Läuft)
  - ✅ Success (Erfolgreich)
  - ❌ Error (Fehler)
- **Paket-Verteilung**: Gezielte Verteilung von Software-Paketen an ausgewählte Depots
- **Job-Verwaltung**: Auswahl und Steuerung von Empirum-Jobs
- **Batch-Operationen**: Mehrfachauswahl und gleichzeitige Verarbeitung mehrerer Depots
- **Datenbankanbindung**: Direkte Integration mit der Empirum-Datenbank

## Technologie-Stack

- **.NET 8.0** (WPF Framework)
- **C# 12**
- **SQL Server** (Empirum-Datenbank)
- **MVVM-Pattern** (Model-View-ViewModel)
- **System.Data.SqlClient** (Datenbankzugriff)
- **CollectionView** (Filtering und Sortierung)

## Projektstruktur

```
DepotService/
├── App.xaml                    # Anwendungs-Einstiegspunkt
├── App.xaml.cs                 # Anwendungslogik
├── MainWindow.xaml             # Haupt-UI-Layout (moderne Oberfläche)
├── MainWindow.xaml.cs          # Haupt-UI-Code-Behind
├── appsettings.json            # Konfigurationsdatei
├── Data/
│   └── EmpirumRepository.cs    # Empirum-Datenbankzugriff
├── Models/
│   ├── DepotDto.cs            # Depot-Datenmodell
│   ├── DepotItem.cs           # Legacy-Modell
│   └── SelectableItem.cs      # Filter-Item-Modell
└── ViewModels/
    ├── MainViewModel.cs        # Haupt-ViewModel (MVVM)
    ├── RelayCommand.cs         # Synchroner Command
    └── AsyncRelayCommand.cs    # Asynchroner Command
```

## Datenmodelle

### DepotDto
Repräsentiert ein Empirum-Depot mit allen relevanten Informationen:
- `Id`: Eindeutige Depot-ID
- `Computer`: Computer-Name des Depots
- `Domain`: Domäne/Standort des Depots
- `Status`: Numerischer Status-Code (0-3)
  - 0 = Waiting (Wartend)
  - 1 = Running (Läuft)
  - 2 = Success (Erfolgreich)
  - 3 = Error (Fehler)
- `StatusText`: Textueller Status (z.B. "Success", "Error")
- `StatusIcon`: Emoji-Symbol für den Status (✅, ❌, ⏳, 🔄)
- `StatusDisplay`: Kombinierte Anzeige aus Icon und Text
- `JobResult`: Status-String für UI-Binding
- `LastCheck`: Zeitpunkt der letzten Überprüfung
- `Information`: Zusätzliche Informationen oder Fehlermeldungen
- `LastJobName`: Name des zuletzt ausgeführten Jobs
- `IsSelected`: Auswahl-Status für Batch-Operationen

### SelectableItem
Hilfsobjekt für Filter-Checkboxen:
- `Name`: Anzeigename des Elements
- `IsSelected`: Auswahl-Status (für Filterung)

## Installation

### Voraussetzungen

- Windows 10/11
- .NET 8.0 Runtime oder höher
- Zugriff auf die Empirum-Datenbank (SQL Server)
- Visual Studio 2022 (für Entwicklung)

### Schritt 1: Projekt öffnen

Öffnen Sie die Solution in Visual Studio:
```bash
DepotService.sln
```

### Schritt 2: Datenbankverbindung konfigurieren

Erstellen oder bearbeiten Sie die `.env`-Datei im Projektverzeichnis:

```env
EMPIRUM_CONNECTION_STRING=Server=IHR_EMPIRUM_SERVER;Database=EMPIRUM_DB;User Id=BENUTZER;Password=PASSWORT;TrustServerCertificate=True;
```

Oder konfigurieren Sie die Verbindung in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "EmpirumConnection": "Server=IHR_EMPIRUM_SERVER;Database=EMPIRUM_DB;Integrated Security=true;"
  }
}
```

### Schritt 3: Projekt bauen und starten

**Via Visual Studio:**
- Öffnen Sie `DepotService.sln`
- Drücken Sie F5 zum Starten

**Via Kommandozeile:**
```bash
dotnet restore
dotnet build
dotnet run
```

## Verwendung

### Übersicht über die Benutzeroberfläche

Die Anwendung ist in folgende Bereiche unterteilt:

1. **Header-Bereich**
   - Depot-Zähler: Zeigt die Gesamtanzahl der Depots
   - Suchfeld: Volltextsuche über Computer und Domäne

2. **Filter- und Aktionsleiste**
   - Standort-Filter (Domänen)
   - Computer-Filter
   - Job-Auswahl (ComboBox)
   - Aktions-Buttons (Verteilen, Refresh)

3. **Haupttabelle**
   - Checkbox-Spalte für Mehrfachauswahl
   - Computer-Name
   - Domäne
   - Status mit farbigem Badge
   - Letzter Check
   - Informationen

4. **Status-Leiste**
   - Aktueller Status der Operation
   - Lade-Indikator
   - Empirum-DB-Verbindungsstatus

### Depots filtern

**Nach Standort (Domäne):**
1. Klicken Sie auf das Filter-Symbol neben "Depots"
2. Wählen Sie die gewünschten Domänen aus
3. Ausgewählte Filter werden als Chips angezeigt
4. Entfernen Sie Filter durch Klick auf das ✕

**Nach Computer:**
1. Klicken Sie auf das Filter-Symbol in der Computer-Spalte
2. Wählen oder deaktivieren Sie Computer
3. Filter sofort aktiv

**Suche:**
- Geben Sie Text in das Suchfeld ein
- Sucht in Computer-Namen und Domänen
- Kombinierbar mit anderen Filtern

### Pakete verteilen

1. **Depots auswählen:**
   - Aktivieren Sie Checkboxen bei den gewünschten Depots
   - Oder nutzen Sie "Alle auswählen" in der Header-Checkbox
   - Nutzen Sie Filter, um gezielt Depots zu finden

2. **Job auswählen:**
   - Wählen Sie einen Job aus der Dropdown-Liste

3. **Verteilen:**
   - Klicken Sie auf "📦 verteilen"
   - Die ausgewählten Depots erhalten den Job
   - Status wird in der Tabelle aktualisiert

### Status-Überwachung

Die Status-Spalte zeigt den aktuellen Job-Status:

- **⏳ Pending**: Job wartet auf Ausführung
- **🔄 Running**: Job wird gerade ausgeführt
- **✅ Success**: Job erfolgreich abgeschlossen
- **❌ Error**: Fehler bei der Job-Ausführung

Farb-Kodierung:
- Grün: Erfolg
- Rot: Fehler
- Orange: Wartend
- Blau: Läuft

### Daten aktualisieren

- Klicken Sie auf "🔄 Refresh" um die Depot-Liste zu aktualisieren
- Die Anwendung lädt automatisch beim Start

## Datenbankzugriff

Die `EmpirumRepository`-Klasse stellt die Schnittstelle zur Empirum-Datenbank bereit:

### Verbindungsverwaltung
- `TestConnectionAsync()`: Testet die Verbindung zur Empirum-Datenbank
  - Rückgabe: `(bool success, string message)`

### Depot-Operationen
- `GetDepotsAsync()`: Lädt alle Depots aus der Empirum-Datenbank
  - Rückgabe: `List<DepotDto>`
  - Enthält: Computer, Domäne, Status, LastCheck, Info

### Job-Verwaltung
- `GetJobNamesAsync()`: Lädt verfügbare Job-Namen aus Empirum
  - Rückgabe: `List<string>`

- `EnqueueStartSyncForManyAsync(depots, jobName)`: Erstellt Sync-Jobs für mehrere Depots
  - Parameter: Liste von Depots, Job-Name
  - Erstellt einen Job pro Depot in der Empirum-Queue

### Interne Datenbankstruktur

Die Anwendung greift auf folgende Empirum-Tabellen zu:
- Depot-Informationen (Computer, Domäne, Status)
- Job-Definitionen
- Job-Queue für Sync-Aufträge

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

### Schichtenaufbau

1. **View (MainWindow.xaml)**
   - Moderne WPF-Oberfläche mit Material Design-Elementen
   - DataGrid mit Virtualisierung für Performance
   - Responsive Filter-Popups
   - Status-Badges mit Farbcodierung

2. **ViewModel (MainViewModel.cs)**
   - Datenbindung zwischen View und Model
   - Command-Implementierungen (SyncAllCommand, RefreshCommand)
   - Filter-Logik (CollectionView)
   - Auswahl-Management (SelectAll, Multi-Select)
   - Asynchrone Datenbankoperationen

3. **Model (DepotDto, SelectableItem)**
   - Datenstrukturen mit INotifyPropertyChanged
   - Berechnete Properties (StatusDisplay, JobResult)
   - Business-Logik für Status-Mapping

4. **Data Access Layer (EmpirumRepository)**
   - Repository-Pattern für Datenbankzugriff
   - SQL-Queries für Empirum-Datenbank
   - Verbindungsmanagement
   - Fehlerbehandlung

### Datenfluss

```
UI-Interaktion
    ↓
Command (ViewModel)
    ↓
Repository-Methode
    ↓
SQL-Abfrage (Empirum-DB)
    ↓
DTO-Mapping
    ↓
ObservableCollection
    ↓
CollectionView (Filtering)
    ↓
DataGrid-Anzeige
```

## Sicherheit

- **Parameterisierte Queries**: Schutz gegen SQL-Injection
- **Verschlüsselte Verbindungen**: TrustServerCertificate-Option konfigurierbar
- **Error-Handling**: Keine sensitiven Daten in Fehlermeldungen
- **Logging**: Passwörter und sensitive Daten werden nicht geloggt

## Performance-Optimierungen

- **Asynchrone Operationen**: Alle Datenbankzugriffe mit async/await
- **Connection Pooling**: Automatisch durch SqlClient
- **DataGrid-Virtualisierung**: Effiziente Darstellung großer Datenmengen
  ```xml
  EnableRowVirtualization="True"
  VirtualizingPanel.IsVirtualizing="True"
  VirtualizingPanel.VirtualizationMode="Recycling"
  ```
- **CollectionView-Filtering**: Clientseitiges Filtern ohne DB-Queries
- **Batch-Operations**: Mehrere Depots gleichzeitig verarbeiten
- **Lazy Loading**: Job-Namen werden einmalig beim Start geladen

## Erweiterungsmöglichkeiten

- **Export-Funktionen**: Excel/CSV-Export der Depot-Liste
- **Automatische Aktualisierung**: Timer-basiertes Polling der Depot-Status
- **Job-Historie**: Anzeige vergangener Jobs pro Depot
- **Benachrichtigungen**: Desktop-Notifications bei Job-Abschluss
- **Erweiterte Filterung**: Speichern und Laden von Filter-Presets
- **Benutzerrechte**: Active Directory-Integration
- **Reporting**: Statistiken über erfolgreiche/fehlgeschlagene Jobs
- **Multi-Language**: Englische Übersetzung

## Fehlerbehebung

### Verbindungsfehler

**Problem:** "Empirum DB Getrennt" wird in der Status-Leiste angezeigt

**Lösung:**
1. Überprüfen Sie die Connection String in `.env` oder `appsettings.json`
2. Stellen Sie sicher, dass der SQL Server erreichbar ist
3. Prüfen Sie Firewall-Einstellungen
4. Testen Sie die Verbindung mit SQL Server Management Studio

### Keine Depots sichtbar

**Problem:** DataGrid bleibt leer nach dem Start

**Mögliche Ursachen:**
1. Alle Filter sind deaktiviert
   - Lösung: Klicken Sie auf "Löschen" im Filter-Popup
2. Suchtext filtert alle Ergebnisse heraus
   - Lösung: Leeren Sie das Suchfeld
3. Datenbankverbindung fehlgeschlagen
   - Lösung: Prüfen Sie den Connection-Status in der Status-Leiste

### Job-Verteilung schlägt fehl

**Problem:** Fehler beim Erstellen der Jobs

**Lösung:**
1. Stellen Sie sicher, dass Depots ausgewählt sind
2. Wählen Sie einen gültigen Job aus der Dropdown-Liste
3. Prüfen Sie die Berechtigungen auf der Empirum-Datenbank
4. Überprüfen Sie die Information-Spalte für Details

### Performance-Probleme

**Problem:** Anwendung reagiert langsam bei vielen Depots

**Lösung:**
1. Nutzen Sie Filter, um die Anzahl der angezeigten Depots zu reduzieren
2. Die Virtualisierung sollte automatisch greifen
3. Schließen Sie nicht benötigte Filter-Popups

### .NET Runtime Fehler

**Problem:** Anwendung startet nicht

**Lösung:**
1. Installieren Sie .NET 8.0 Runtime (Desktop)
2. Download: https://dotnet.microsoft.com/download/dotnet/8.0

## Lizenz

Dieses Projekt ist proprietär und darf nicht ohne Genehmigung verwendet werden.

## Kontakt

Für Fragen oder Support kontaktieren Sie bitte das Entwicklungsteam.

## Best Practices

### Effiziente Nutzung

1. **Filter verwenden**: Nutzen Sie die Filter-Funktionen, um gezielt Depots zu finden
   - Nach Standort für standortspezifische Operationen
   - Nach Computer für einzelne Rechner
   - Kombination beider Filter für präzise Auswahl

2. **Batch-Operationen**: Wählen Sie mehrere Depots gleichzeitig aus
   - Spart Zeit bei der Paket-Verteilung
   - Nutzen Sie "Alle auswählen" + Filter für gezielte Massenoperationen

3. **Status überwachen**: Aktualisieren Sie regelmäßig mit "Refresh"
   - Überprüfen Sie Job-Status nach der Verteilung
   - Achten Sie auf Error-Status in der Information-Spalte

4. **Suchfeld nutzen**: Schnelles Finden spezifischer Depots
   - Besonders nützlich bei vielen Depots
   - Kombinierbar mit Filtern

### Sicherheitshinweise

- **Datenbankzugriff**: Die Anwendung benötigt Lese- und Schreibzugriff auf die Empirum-DB
- **Berechtigungen**: Führen Sie die Anwendung mit entsprechenden Rechten aus
- **Verbindungsstring**: Speichern Sie Credentials sicher (nicht im Quellcode)
- **Produktivumgebung**: Testen Sie Jobs zuerst an wenigen Depots

## FAQ

**F: Wie viele Depots kann die Anwendung verwalten?**
A: Durch Virtualisierung können tausende Depots effizient dargestellt werden. Die Performance hängt hauptsächlich von der Datenbankverbindung ab.

**F: Werden Jobs sofort ausgeführt?**
A: Nein, Jobs werden in die Empirum-Queue eingereiht und von Empirum verarbeitet. Der Status wird in der Datenbank aktualisiert.

**F: Kann ich mehrere Jobs gleichzeitig verteilen?**
A: Nein, pro Verteil-Aktion wird ein Job ausgewählt und an die gewählten Depots verteilt.

**F: Was passiert bei Verbindungsabbruch?**
A: Die Anwendung zeigt "Empirum DB Getrennt" an. Bereits erstellte Jobs bleiben in der Empirum-Queue erhalten.

**F: Wie oft sollte ich "Refresh" klicken?**
A: Je nach Bedarf. Die Daten werden beim Start geladen. Aktualisieren Sie manuell, um aktuelle Job-Status zu sehen.

## Changelog

### Version 1.0.0 (März 2026)
- Initiale Version mit Empirum-Integration
- Moderne WPF-Oberfläche mit Material Design
- Standort- und Computer-Filterung
- Status-Überwachung mit visuellen Indikatoren
- Batch-Paket-Verteilung
- Job-Auswahl und -Verwaltung
- Empirum-Datenbankanbindung
- Volltextsuche
- Mehrfachauswahl mit SelectAll
- Connection-Status-Anzeige

---

**Entwickelt für:** Innomea Depot Management
**Letzte Aktualisierung:** März 2026
