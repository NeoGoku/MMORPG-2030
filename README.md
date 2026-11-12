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

Housing/Innenräume/Möbelplatzierung, Gilden/PvP, Reparaturen/Verkaufsstände/Auktionshaus, neue Dungeon-/Bossregionen, vollständiges Welt-Streaming und Produktionsskalierung sind noch nicht implementiert. Eigene prozedurale Assets; keine kostenpflichtigen Dienste.
