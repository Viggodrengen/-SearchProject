# Modul 2 - Introduktion til casen og testdata

## Tid
- 03/02 kl. 12.30-16.00

## Tema
- Soegemaskine-casen
- Testdata (Enron/seData)

## Forberedelse
- Tjek IDE og .NET SDK 10
- Hent kildekode til casen
- Laes siden om testdata

## Agenda
1. Hvad er en soegemaskine? (indeksering, soegning, datamodel)
2. Demo
3. Opgavearbejde
4. Code walk-through (arkitektur, indexer/crawler, console search)

## Opgaver (kort)
1. Faa soegemaskinen til at koere lokalt
- Build projekter, opdater pakker, koer indexer og search
- Konfigurer SQLite/Postgres
- Verificer data i DB

2. Forbedr indexer-output
- Vis total ordforekomster
- Sporg hvor mange ord der skal vises
- Vis topord sorteret efter hyppighed

3. Case sensitivity on/off
- Fx `/casesensitive=on|off`

4. Visning af timestamps on/off
- Fx `/timestamp=on|off`

5. Antal resultater
- Fx `/results=15` eller `/results=all`

6. (Svaer) Moenstersoegning
- Wildcards `?` og `*`
- Returner dokumenter + matchede ord
