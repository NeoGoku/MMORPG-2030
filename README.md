# MMORPG-2030 — Nordtor

Spielbarer 3D-Stadtbezirk für Android, Windows und Browser, entwickelt mit Godot 4.3 und einem autoritativen .NET-8-Server. GDD v1.1 bleibt verbindlich; das Projekt ist ein wachsender MMORPG-Ausschnitt, kein fertiges MMO.

## Sofort spielen

Die APK startet im ausdrücklich **temporären lokalen Training**, ohne Hosting. Mit linkem Joystick bewegen, rechts im freien Bildbereich Kamera drehen. **Sprechen** bei Mira nimmt den Drohnenauftrag an. **Ziel** wechselt Gegner, **Angriff** schaltet stationären Auto-Angriff um; Bewegung unterbricht normale Schläge. Persönliche Beute aufsammeln und bei Mira abgeben. HP, Schaden, Tod und sichere Wiederbelebung sind spielbar.

PC: WASD/Pfeile, Tab Zielwechsel, Leertaste Auto-Angriff, E sprechen, F Beute, 1–6 Skills, rechte Maustaste ziehen für Kamera. Joystick und Aktionen unterstützen mehrere Finger. Unter **Charakter / Inventar → Layout** Touchpositionen anpassen, lokal speichern oder zurücksetzen.

## Online und M4-Systeme

**Online** öffnet Registrierung/Anmeldung. Erreichbaren HTTPS-Server-Root eingeben, eigenen Charakterplatz (drei pro Account), Namen und eine der acht Klassen wählen, erstellen und spielen. Passender v4-Server erforderlich. PostgreSQL speichert Fortschritt/Identität; ohne DB steht ausdrücklich **flüchtiger Testbetrieb**. Lokales Training wird nicht hochgeladen. Kein öffentlicher Server wurde gebucht oder bereitgestellt. [Betrieb und Datenbank](docs/M3_OPERATIONS.md).

- **Gruppe / Chat / Handel**: Spieler auswählen, einladen/Einladung annehmen, Leitung übertragen oder Gruppe verlassen. Bis zehn Mitglieder; nahe Gruppenmitglieder teilen Kill-EXP und erhalten eigene Beute. Lokal-, Privat- und Gruppenchat. Direkter Handel innerhalb 5 m über Item/Menge/Münzen und zwei Bestätigungen; geänderte Angebote müssen neu bestätigt werden. Diese Funktionen benötigen echte Online-Mitspieler.
- **Skills**: acht funktionierende aktive Skills je Klasse, sechs gleichzeitig sichtbare, frei belegbare Plätze. Zwei Starter-Skills ab Stufe 1; weitere ab 2/3/5/8/12/16. Mana, Cooldown, Reichweite und Statuswirkungen berechnet der Server. Heiler heilen Gruppen, Tanks spotten, Beschwörer rufen separate zeitweilige Kreaturen.
- **Inventar**: Attribute/Ausrüstung, Händler rechts und Werkbank links der Wache, drei Sammelstellen und Rezepte für alle zehn Berufe. Berufs-EXP/Level, Questlog und Zusatzaufträge. Neue Produkte umfassen Mantel, Linse, Essen, Stuhl, Pet-Ei/Futter und Evolutionssplitter. Der Stuhl ist noch kein platzierbares Housing-Möbel.
- **Pet**: ein aktiver Begleiter mit Kampf/Heil/Tank/Buff/Beute/Sammelmodus, Tasche und bis zehn eigenen Pets. Ei ausbrüten, zwei erwachsene Pets ab Stufe 2 mit Futter kreuzen, ab Stufe 5 per Splitter entwickeln. Inaktives Pet mit Futter an der Werkbank fünf Minuten trainieren, auch offline; Ertrag einmalig. Details und Zutaten stehen im Menü.
- **Automatik**: bewusst am sicheren Stadtrand starten (z. B. nahe 0,3). Begrenzter Radius und maximal 60 Sekunden Kampf, Zielsuche/Skills/Tränke/Beute und Rückkehr bei Grenzen. Manuelle Bewegung stoppt sofort; blockierter Rückweg stoppt ohne Teleport. Kein Offline-Farmen.
- Klassen haben unterschiedliche ursprüngliche geometrische Silhouetten. Tag/Nacht, Regen/Nebel und entfernungsabhängige Sichtbarkeit ergänzen die bestehende Stadt/Südpark. Wetter wirkt derzeit optisch.

Lokales Training unterstützt Klassen-/Wirtschafts-/Pet-/Farmregeln temporär; es simuliert keine anderen Spieler. Online-Gruppenmitgliedschaft überlebt eine kurze Wiederverbindung im selben Serverprozess, nicht dessen Neustart. Charakterfortschritt benötigt die konfigurierte Datenbank.

## Cloud-Builds und Prüfung

PR-Checks oder **Actions → Client exports → Run workflow** erzeugen `MMORPG-Android-debug`, `MMORPG-Windows` und `MMORPG-Web` als ZIP-Artefakte für 14 Tage. Downloadlinks und tatsächliche CI-Ergebnisse stehen im PR. APK aus dem Android-ZIP installieren; wegen temporärer Debug-Signatur kann eine Neuinstallation nötig sein. Windows EXE/PCK zusammen entpacken, kompletten Web-Ordner über HTTP(S) ausliefern. Der Besitzer benötigt keinen lokalen Entwicklungs-PC.

```sh
dotnet run --project Server.Tests --configuration Release -- Client/data/district.json
godot --headless --path Client --script res://tests/m4_smoke.gd
# Gerenderte Stadt-/Touchprüfung unter Linux:
xvfb-run -a godot --path Client --audio-driver Dummy --rendering-driver opengl3 --script res://tests/scene_smoke.gd
```

CI ergänzt echte PostgreSQL-, HTTP/WebSocket- und gerenderte Account-/Skill-/Chat-/Pet-UI-Integration. Physische Android-Abnahme dieses M4-Pakets, Windows-Runtime, Geräte-Crossplay und Last-/Batterieprüfung bleiben offen.

[Genauer Funktionsstand und Grenzen](docs/IMPLEMENTATION_STATUS.md) · [GDD](docs/GDD.md) · [Architektur/Protokoll](docs/ARCHITECTURE.md) · [Roadmap](docs/ROADMAP.md) · [Asset-Herkunft](docs/ASSETS.md).

Historischer M4-Stand: Housing, Gilden/PvP und Auktionen waren noch offen. Der folgende M5-Abschnitt und der aktuelle Implementierungsstatus beschreiben die inzwischen ergänzten Funktionen. Reparaturen/Verkaufsstände, vollständiges Welt-Streaming und Produktionsskalierung bleiben offen. Eigene prozedurale Assets; keine kostenpflichtigen Dienste.

## M5: gemeinsame Wohnungen, Gilden, Börse und Arena

**Wohnen / Gilde / Börse** öffnet die neuen Touchmenüs. Elin steht bei **(30,24)**. Wohnquest annehmen, einen Eichenstuhl bauen und drei zusätzliche Holz sammeln, dann eine freie Lindenhof-Wohnung wählen. Reale Eingänge liegen links an der Südseite; keine privaten Instanzen. In der Wohnung Stuhl platzieren/drehen/entfernen, Lager/Werkstatt/Zucht/Garten nutzen und Besucher oder Gilde zulassen. Ein angrenzendes Zimmer lässt sich ausbauen. Miete manuell wöchentlich zahlen; ein Rückstand zerstört keine Items.

Beim selben Register Gilde gründen, einladen und Ränge verwalten. Materialaufträge bringen Gilden-EXP/Energie; Offiziere verwalten das Lager und aktivieren den PvE-Segen. Ein echtes Gildenhaus kann aus Lagermaterialien erworben werden. Die **Auktionsbörse (30,12)** erlaubt Items einstellen, suchen, kaufen, zurücknehmen und Erlöse abholen, auch wenn der Verkäufer offline ist. Preise sind Gesamtpreise; fünf Prozent/minimal eine Münze Gebühr.

Die markierte **Arena (-50,35)** nutzt angenommene Duelle oder bereite Gruppenteams mit normierten Matchwerten. Gilden-Gruppenteams kämpfen ebenfalls nur hier. Ein gemeinsamer Arena-Angriff ist implementiert; vollständige Klassen-PvP-Balance fehlt. Forfeit/Disconnect/Zonenaustritt gibt keine Belohnung.

Im Industriegebiet und den offenen **Tiefwerken** warten fünf neue Gegner/Questziele und AUR-IX. Warnkreis verlassen oder den Schaden gemeinsam tragen; mehrere Angreifer brechen seine Kooperationsrüstung. Der Kern wird zur Schmiedewaffe verarbeitet. Optionaler Transit bei (0,-40) setzt Stufe 5 und Miras abgeschlossenen Auftrag voraus. Die Welt bleibt zu Fuß zusammenhängend.

Passender **v5-Server** nötig. [M5-Betrieb, Grenzen und vorläufige Regeln](docs/M5_OPERATIONS.md). Neue Häuser streamen nach Entfernung; dies ist noch kein vollständiges Welt-Streaming. Housing hat derzeit vier Wohnungen, zwei Räume/eine Etage und Stühle. Erweiterte Stockwerke, territoriale Gildenkriege, umfassende PvP-Skills, Reparaturen/Stände und Produktionsgrafik bleiben offen. Die M5-Erweiterung ersetzt die oben als M4 beschriebenen Grenzen nur dort, wo der aktuelle [Implementierungsstatus](docs/IMPLEMENTATION_STATUS.md) echte Funktionen ausweist.

## M6 integration
Gildenchat im Chatfenster; Leitung kann das Gildenhaus freigeben, Lager bleibt erhalten. Auktionssuche besitzt Seiten; abgelaufene Angebote bleiben bis Rücknahme sicher im Escrow. Neue tägliche Versorgungsquest ab Stufe 2 verdient einen Statpunkt pro UTC-Tag. Seltenen Resetstein über die Boss-Questkette verdienen; im Charaktermenü ab Stufe 10 nutzen. Prestige ab Stufe 100 beim Händler gegen Bosskern (PvE-Bonus, kein Levelreset). Tod verliert vorläufig 5 % der aktuellen Level-EXP, ohne Levelverlust. Passender v6-Server erforderlich. [Exakter Stand/Fortsetzung](docs/DEVELOPMENT_HANDOFF.md). Die vollständige MMORPG-Alpha ist weiterhin in Entwicklung.

## Zusammenhängende Fortsetzung (v7)
Im Charakterfenster **Aussehen** anpassen. Im Inventar alte Ausrüstung ablegen und einzeln in eine eindeutige Instanz umwandeln; diese ausrüsten und beim Händler reparieren. Zustand/Identität bleiben bei Handel, Auktion und Wohnungslager erhalten. Nicht ausgewählte inaktive Pets ohne Training lassen sich über die Börse handeln. Klassen besitzen eigene Ressourcen und ab Stufe 5 wirksame Passive. In Arenamatches verwenden die sechs Skillplätze normierte Klasseneffekte mit separater Energie.

Der zusammenhängende Grünsaum liegt südlich bei z=100–138: Waldgegner, Rohstoffe, offene Ruinen und Wurzelherz. Fortgeschrittene Rezepte verlangen Berufslevel und seltene Bossmaterialien; neue Klassen-/Berufs-/Weltquestketten führen weiter. Minimap links zeigt reale Gegner/Mitspieler/Services. **Darstellung** speichert Sparsam/Ausgewogen/Weite Sicht. Diese Profile sind noch keine gemessene Gerätefreigabe. [Exakter Fortsetzungspunkt](docs/DEVELOPMENT_HANDOFF.md).
