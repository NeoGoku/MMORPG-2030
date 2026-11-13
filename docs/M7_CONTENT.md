# M7 — tatsächlicher spielbarer Umfang

Passende v9-Clients und Server gemeinsam verwenden. Bestehende Charaktere und
Weltaggregate bleiben erhalten; Migrationen 001/002 gelten weiter. Neue JSON-Felder
sind additiv. Keine offenen PRs wurden gemergt; PR #11 baut auf #10 und #9 auf.

## Neue zusammenhängende Inhalte

| Gebiet | Zugang / Transit | Inhalt |
|---|---|---|
| Helixhafen | Stufe 15; (48,-34) ↔ (78,-52) | Drohne, Arkanforscher, Elitewächter, offenes Sturmlabor mit Sturmkern-Boss, Erzader, Feldhandel und Werkbank |
| Aschenhain | Stufe 12; (-28,60) ↔ (-72,78) | Aschenkeiler, Glutgeist, Hainwächter, offener Feuerschrein mit Aschenmutter, Herzholz, Feldhandel und Werkbank |

Straßen/Wege verbinden die Gebiete auch ohne Transit. Server prüft Regionslevel
bei Bewegung, Transit und Wiederanmeldung. Teleporter sind per Interagieren oder
Weltmenü → Arena/Welt bedienbar. Figuren und Monster teilen dieselbe Welt und die
bestehenden räumlich gefilterten Snapshots. Bosswarnkreise, zweite Kampfphase und
Kooperationsrüstung verwenden das bestehende Gruppenkampfsystem.

Die sechs neuen Quests stehen im Questlog: je Gebiet normale Gegner besiegen,
dann den Boss, anschließend regionale Rüstung herstellen. Questmarker wechseln
bei erfülltem Ziel zum nächsten Feldhändler. Persönliche Bossbeute und
Questbelohnungen liefern Materialien; Helixpanzer und Aschenmantel benötigen
Berufsstufe 4 und seltene Materialien. Beide Rüstungen haben echte Kampfwerte
und Level-/Klassenanforderungen. Balancewerte bleiben vorläufige Katalogdaten.

Neue Gebietskulissen laden und entladen nach Entfernung mit Hysterese; primitive
Geometrie wird nach Material gebündelt. Türen und Wände der offenen Dungeonräume
besitzen dieselben Strukturdaten für Clientkollision und Serverbewegung.
Kamerafahrt und Figuren verwenden die vorhandene Third-Person-Steuerung.

## Speichern, Wiederverbinden und Produktion

Im Charakterfenster **Online-Zustand jetzt speichern / bestätigen** wählen.
Bestätigung folgt erst nach erfolgreichem Commit. Bestätigter und aktueller
Simulationstick sind sichtbar. RAM-Betrieb bestätigt ausdrücklich keine
dauerhafte Speicherung. Party-Checkpoints sind atomare Batches; Speicherfehler
werden mit begrenztem Abstand wiederholt. Bei Wiederverbindung wird ein noch
ausstehender Disconnect-Stand zuerst gespeichert, statt den älteren SQL-Stand
zu laden.

Wohnungsproduktion bindet eine Kopie der eingesetzten Materialien. **Produktion
abbrechen / Materialien zurück** gibt genau diese einmalig zurück. Bei vollem
Inventar bleibt der Auftrag erhalten. Nach Rezeptänderungen gelten die
ursprünglichen Mengen. Ältere Aufträge ohne Materialmetadaten bleiben abholbar,
können aber keine rückwirkend erfundene Erstattung erzeugen.

## Reproduktion und Nachweise

Der erfolgreiche Server-Workflow liefert `MMORPG-Server`: .NET8-Programm,
Katalogdaten, SQL-Migrationen und `SOURCE_COMMIT`. Entpacken, .NET8 installieren,
`MMORPG_DATABASE` über geschützte Umgebung setzen und
`dotnet MMORPG.Server.dll --urls http://127.0.0.1:5080` hinter dem dokumentierten
TLS-Proxy starten. Für reproduzierbaren PostgreSQL16-/Server-Betrieb weiterhin
Dockerfile/Compose und die vorhandene Betriebsdokumentation verwenden.

Lokaler gezielter Nachweis: atomare Checkpoints, fehlschlagender Disconnect mit
sicherer Wiederverbindung, Materialstornierung nach Welt-Neuerstellung und
Rezeptänderung, Gebietslevel/Transit, Quest-/Crafting-/Loot-Transaktionen und
Godot-Gebietsstreaming. Ein eigener realer Serverprozess mit PostgreSQL wurde
nach bestätigtem Speichern mit SIGKILL beendet: der gesamte Charakterdatensatz
blieb identisch, alte Tokens/Tickets wurden abgewiesen, frische Anmeldung und
Wiederverbindung funktionierten. CI führt diesen Test erneut sowie den
isolierten Backup-/Restore-Vergleich sämtlicher Account-, Charakter-, Welt- und
Migrationszeilen aus. Build-Ergebnisse und Artefakte stehen im PR.

## Weiterhin offen

Unbestätigte Ticks können bei hartem Abbruch verloren gehen. Datenbankzugriffe
bleiben synchron unter dem Weltlock; eine asynchrone dauerhafte Save-Queue und
Crash-Tests an allen Transaktionsgrenzen fehlen. Monster, Drops, Parties und
Matches sind Laufzeitzustand. Dies sind zwei kompakte Inhaltsgebiete mit
stilisierten Originalmeshes. Vollständige Großstädte, obere Housing-Etagen,
finale semi-realistische Grafik, gemessene Android-Optimierung und die gesamte
GDD-Alpha sind damit nicht abgeschlossen. Keine Kapazitätszusage für 50–100
Spieler und keine physische Geräteabnahme.
