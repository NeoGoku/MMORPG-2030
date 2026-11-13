# M8 — Speicherung, gemeinsame Geschosse und Produktionsanlagen

Dieser Stand setzt PR #11 fort und benötigt passende **v10-Clients und Server**.
Vor dem Update die gesamte PostgreSQL-Datenbank sichern. Migration 003 ergänzt
`commit_receipts`; Charaktere und gemeinsame Welt bleiben in den vorhandenen
JSONB-Aggregaten. Keine v9- und v10-Server gleichzeitig auf dieselbe Datenbank
schreiben lassen. Für einen Rückstieg den gesicherten alten Stand samt Datenbank
wiederherstellen, statt neue Geschossdaten mit einer alten Binärdatei zu schreiben.

## Commit-Bestätigungen und Recovery

Jede Charakter-/Welttransaktion erhält eine kanonische SHA-256-ID aus Payload,
Besitzern und erwarteten Revisionen. Datenänderungen und Receipt werden in
derselben SQL-Transaktion committed. Exakt dieselbe Schreibanforderung lässt
sich erneut bestätigen, ohne Währung, Items oder Revisionen doppelt zu ändern.
Ein älterer bestätigter Payload darf keinen inzwischen neueren Stand ersetzen.

Nach einem verlorenen Commit-Ergebnis versucht der Store, genau diese Receipt
erneut zu lesen. PostgreSQL-Transaktionslocks verhindern, dass die Statusabfrage
eine noch laufende identische Transaktion als sicher abgebrochen behandelt.
Receipts sind kontogebunden über `GET /api/commits/{id}` mit Bearer-Authentifizierung
abfragbar. `GET /api/characters` enthält die vollständige letzte Commit-ID;
das Charaktermenü zeigt eine Kurzform. RAM-Betrieb bleibt flüchtig.

Gezielte Prüfungen mit echtem PostgreSQL bestätigen Wiederholung, Besitzbindung,
Store-Neueröffnung und Schutz neuerer Revisionen. Der Prozessabbruchtest prüft
jetzt zusätzlich, dass die Receipt nach SIGKILL und erneuter Anmeldung erhalten
bleibt. Backup/Restore vergleicht auch die Receipt-Zeilen.

**Grenzen:** Die Simulation speichert weiterhin synchron unter dem Weltlock.
Eine dauerhafte asynchrone Save-Queue fehlt. Unbestätigte Ticks sind nicht
verlustfrei. Bei längerem Datenbankausfall können unklare Ergebnisse weiterhin
eine erneute Anmeldung bzw. betriebliche Recovery verlangen; Fehlertexte
behaupten dann keinen sicher ausgebliebenen Transfer. Dies ist Idempotenz exakter
Store-Schreibvorgänge, kein allgemeines Replay beliebiger Gameplay-Befehle.
Receipts haben noch keine Archivierungsstrategie; Wachstum vor Lastfreigabe messen.

## Physische Housing-Geschosse

Eigentümer bauen im Erdgeschoss über **Wohnen → Geschoss** zusätzliche Etagen.
Vorläufig maximal drei; Kosten je Ausbau: 20 Holz und 60 Münzen, multipliziert
mit der vorhandenen Geschosszahl. Werte und Geschosshöhe stehen in `game.json`.

Der gemeinsame Treppenaufgang liegt pro Haus bei **Haus-X−7, Haus-Z+3**.
In 1,8 m Reichweite funktionieren **Treppe hinauf/hinunter** im Wohnmenü.
**Interagieren** führt vom Erdgeschoss hinauf und sonst eine Etage hinunter.
Nur benachbarte Geschosse sind erreichbar. Der Wechsel ist interaktionsgesteuert;
eine kontinuierliche Rampen-/Treppenphysik ist nicht implementiert.

Position, Gebäude und Geschoss des Charakters bleiben gespeichert. Spieler und
Pets erhalten sichtbare Höhe; die Kamera folgt. Alle Besucher auf derselben
Etage sehen dieselben Spieler und Möbel. Die Ansicht blendet andere Etagen als
Gebäudeschnitt aus; es entstehen keine privaten Instanzen. Entfernungsstreaming
lädt auch obere Gebäudeteile und Einrichtung aus.

Möbel besitzen ein gespeichertes Geschoss. Dieselben X/Z-Koordinaten auf zwei
Etagen sind getrennte Stellflächen. Server prüft Platz, Tür, Treppenaufgang,
Belegung, Besitz und Geschoss. Fremde Etagenmöbel lassen sich nicht durch
überlappende X/Z-Koordinaten zurücknehmen. Besuchsrechte gelten auch oben;
Widerruf entfernt Besucher sofort in die Außenwelt. Wiederanmeldung validiert
Gebäude und Rechte erneut. Obere Etagen können nicht ins Leere verlassen werden.
Außenkampf/-Loot und Handel zwischen überlappenden Etagen sind gesperrt.

## Produktionsanlagen

Neue herstellbare Möbel: **Arkanschmiede**, **Industriewebstuhl** und
**Destillationsanlage**. Die Anlagen haben eigene ursprüngliche 3D-Modelle und
benutzen dieselbe persistente Möbelplatzierung. Rezepte verlangen Berufsstufe 2.
Eine Berufsquest belohnt den Bau der Arkanschmiede.

Im Wohnmenü die passende Anlage und ein Rezept auswählen, innerhalb von 3 m
auf derselben Etage stehen und den Auftrag starten. Schmiede: Schmieden und
Ingenieurskunst; Webstuhl: Schneiderei; Destillation: Alchemie und Kochen.
Arbeitstische unterstützen die bestehenden Berufsrezepte. Bis zu sechs Aufträge
je Wohnung können parallel arbeiten, mit einer Belegung pro Anlage.

Materialien werden beim Start gebunden; die UTC-Fertigstellung läuft offline.
Abholung gewährt Item, Berufs-EXP, Farm-EXP und passenden Questfortschritt genau
einmal. Stornierung erstattet die ursprünglichen Materialien auch nach einer
Rezeptänderung. Volles Inventar erhält den Auftrag unverändert. Belegte Anlagen
können nicht verschoben oder zurückgenommen werden. Alte Einzelproduktionen
bleiben bedienbar; alte Aufträge ohne Input-Metadaten bleiben abholbar.

## Nachweise und Fortsetzung

Gezielte PostgreSQL-Fälle: zwei Spieler auf gemeinsamer oberer Etage, Kosten,
Treppen-/Raumgrenzen, Besuchswiderruf, Neustart, Möbelrücknahme, parallele Anlagen,
falsche Rezepte, Duplikatbefehle, einmaliger Fortschritt und Inventar-Recovery.
`Server.Tests --m8` wählt diese Fälle ohne Wiederholung aller historischen Tests.
Die finale CI prüft außerdem die bestehenden Netzwerkpfade, gerenderte
Geschoss-/Anlagenansicht sowie Android-, Windows-, Web- und Serverartefakte.
Der Rendernachweis ist eine Szeneprüfung, keine physische Smartphone-Abnahme.

Weitere Städte, vollständige GDD-Systeme, finale semi-realistische Rigs/Texturen,
territoriale Gildenkriege, breite Produktionsketten und gemessene Geräte-/Last-
optimierung bleiben offen. Nächste Aufgabe: bestätigte Save-Queue und vollständige
Recovery bei längerem Ausfall, anschließend Stadt-/Housing-Erweiterung auf den
neuen Geschoss- und Anlagensystemen. Keine automatische Hintergrundfortsetzung.
