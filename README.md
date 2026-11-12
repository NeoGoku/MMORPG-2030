# MMORPG-2030 · Nordtor

Ein spielbarer kleiner 3D-Stadtbezirk auf Basis des [GDD v1.1](docs/GDD.md). [Tatsächlicher Funktionsumfang und Grenzen](docs/IMPLEMENTATION_STATUS.md).

## APK spielen — ohne PC oder Server

Unter **Actions → Client exports → erfolgreicher Lauf → Artifacts** `MMORPG-Android-debug` herunterladen, ZIP entpacken und APK installieren. Jeder Build verwendet einen neuen Debug-Schlüssel; bei Signaturkonflikt die alte APK deinstallieren. Die APK startet sofort im ausdrücklich markierten **lokalen Training**. Der Fortschritt ist temporär und wird nicht gespeichert.

1. Mit dem linken Joystick zur goldenen Stadtwache **Mira** gehen. In ihrer Nähe **SPRECHEN** tippen: Auftrag annehmen.
2. Durch das Tor in den Nordhof gehen. **ZIEL** wählt/wechselt eine lebende Drohne; der goldene Ring und die Ziel-HP zeigen die Auswahl.
3. In Schlagweite gehen, stehen bleiben und **ANGRIFF AUTO** einschalten. Bewegung unterbricht normale Schläge. Die Drohnen greifen zurück; HP und Schadenszahlen sind sichtbar. Erneutes Tippen stoppt den Auto-Angriff.
4. Zur goldenen Beutekiste gehen und **BEUTE** tippen. Drei Drohnen besiegen, dann bei Mira den Auftrag abgeben: EXP und Münzen erhalten. Drohnen erscheinen erneut; besiegte Spieler werden bei der Wache wiederbelebt.

Rechts im freien Bildbereich wischen dreht/neigt die Third-Person-Kamera. Joystick und Aktionsknöpfe unterstützen verschiedene Finger. PC: WASD/Pfeile, Tab zum Zielwechsel, Leertaste für Auto-Angriff, E zum Sprechen, F für Beute, rechte Maustaste ziehen für Kamera.

## Echte Mitspieler

**Online** öffnen, erreichbare Serveradresse (`ws://…/ws` bzw. `wss://…/ws`) eingeben und verbinden. Zwei Clients am selben Server teilen Spieler, Drohnen, HP, Angriffe und Tode; andere Spieler sind blau markiert. Questfortschritt und Beute sind persönlich. Es gibt keinen vorinstallierten öffentlichen Server. Training und Online-Fortschritt sind getrennt und temporär; eine Verbindung erschafft einen neuen Charakter ohne Konto.

Server kostenlos in einer bestehenden Cloud-Entwicklungsumgebung mit .NET 8 starten:

```sh
dotnet run --project Server --urls http://0.0.0.0:5080
```

Die Telefon-App benötigt einen vom Telefon erreichbaren Host; `127.0.0.1` auf dem Telefon zeigt auf das Telefon selbst. HTTPS-Browserseiten benötigen einen genehmigten TLS-Endpunkt mit `wss://`. Es wird hier kein Hosting gebucht und nichts produktiv bereitgestellt. Der Entwicklungsserver hat noch keine Anmeldung oder dauerhafte Speicherung.

## Cloud-Builds und Nachweise

Ein PR oder **Actions → Client exports → Run workflow** erstellt `MMORPG-Android-debug`, `MMORPG-Windows` und `MMORPG-Web` (14 Tage). Windows benötigt EXE und PCK zusammen; den kompletten Web-Ordner per HTTP(S) bereitstellen. Alle Schritte sind in der Cloud möglich. Server- und Client-CI führen die passenden Szenario-, Netzwerk- und gerenderten Eingabeprüfungen aus.

Entwicklungsprüfungen mit Godot 4.3 / .NET 8:

```sh
dotnet run --project Server.Tests --configuration Release -- Client/data/district.json
godot --headless --path Client --script res://tests/gameplay_smoke.gd
xvfb-run -a godot --path Client --audio-driver Dummy --rendering-driver opengl3 --script res://tests/scene_smoke.gd
# Mit laufendem Server auf 127.0.0.1:5080:
python -m pip install -r tests/requirements.txt
python tests/multiplayer.py
godot --headless --path Client --script res://tests/network_smoke.gd
```

[Architektur/Protokoll](docs/ARCHITECTURE.md) · [Roadmap](docs/ROADMAP.md) · [Asset-Herkunft](docs/ASSETS.md). Noch fehlen unter anderem Konten, Charaktererstellung, PostgreSQL-Persistenz, vollständige Klassen/Fertigkeiten und MMO-Skalierung. Der aktuelle Bezirk ersetzt diese GDD-Ziele nicht.

## M3: Charaktere, Skills, Wirtschaft und Begleiter

**Online** öffnet jetzt Anmeldung und drei Charakterplätze. Server-Root per HTTPS eingeben, registrieren/anmelden, Name und Klasse wählen, erstellen und spielen. Eine PostgreSQL-Konfiguration speichert Online-Fortschritt; ohne Datenbank steht deutlich **flüchtiger Testbetrieb**. Kein öffentlicher Server wurde eingerichtet. [Server-/Datenbankanleitung](docs/M3_OPERATIONS.md).

**Charakter / Inventar** öffnet Attribute, Inventar/Ausrüstung, Questlog und Pet-Menü. Händler und Werkbank stehen rechts/links der Wache. In ihrer Nähe Interaktion wählen; Sammelstellen liegen bei den beschrifteten Markern. Erz zu einer Eisenwaffe schmieden, Kräuter zu Heiltränken verarbeiten, ausrüsten/handeln und Berufs-EXP für zusätzliche Statpunkte sammeln. Die Zusatzaufträge werden im Questlog geführt und beim Händler abgegeben. Das Portal führt zum Südpark und zurück.

Sechs Skillknöpfe sind sichtbar (PC: 1–6); pro Klasse sind derzeit zwei tatsächlich nutzbar, vier gesperrt. Mana und Cooldowns gelten serverseitig. Das Pet-Menü aktiviert genau einen Funken in Kampf-, Heil- oder Beutemodus. Fortschritt, vollständige Klassen/alle Berufe, Gruppen/Chat und Streaming sind nicht pauschal fertig — [genaue Grenzen](docs/IMPLEMENTATION_STATUS.md).

Im Fenster **Layout** lassen sich Joystick, Aktions- und Skillknöpfe einzeln verschieben und lokal speichern; **Standardpositionen** setzt sie zurück.
