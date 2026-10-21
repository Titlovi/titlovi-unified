# Instalacija dodatka Titlovi.com

[English](../en/INSTALL.md) · **Hrvatski** · [Српски](../sr-Cyrl/INSTALL.md)

> **Službena izdanja:** [Emby 2.0.0.0](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0) za **Emby Server 4.10.x** i [Jellyfin 2.0.0.0](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0) za **Jellyfin 12** su objavljeni.

Za preuzimanje titlova putem dodatka potreban je **korisnički račun na Titlovi.com**.

## Jellyfin 12 (.NET 10)

Službena distribucija: [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin). Zadržana je postojeća adresa kataloga dodataka:

```text
https://titlovi.github.io/titlovi-jellyfin/repository.json
```

**Instalacija objavljenog izdanja Jellyfin 2.0.0.0:**

1. Otvorite administratorsku upravljačku ploču Jellyfina i odjeljak **Plugins → Repositories**.
2. Provjerite je li navedena adresa kataloga Titlovi dodana; ako nije, dodajte je.
3. U katalogu pronađite **Titlovi.com** i instalirajte izdanje koje je izričito kompatibilno s **Jellyfinom 12**.
4. Ponovno pokrenite Jellyfin i otvorite postavke dodatka.
5. Unesite korisničko ime i lozinku za Titlovi.com, spremite postavke i provjerite pretraživanje i preuzimanje titla.

U postojećem katalogu nalaze se i **starija izdanja za starije inačice Jellyfina**. Instalirajte samo izdanje kompatibilno s vašim poslužiteljem. Unified 2.0 **nije** namijenjen Jellyfinu 10.10 ili 10.11.

### Nadogradnja izvornog Jellyfin dodatka

Nova implementacija zadržava **izvorni identifikator Jellyfin dodatka (GUID)** i postojeću adresu kataloga. Predviđena je kao nadogradnja, a ne kao zaseban dodatak.

Prije nadogradnje **sigurnosno kopirajte podatke i konfiguraciju poslužitelja**, uključujući `plugins/configurations/Titlovi.Plugin.xml` ako postoji. Na Jellyfinu 12 instalirajte ili nadogradite dodatak na verziju 2.0.0.0 iz postojećeg kataloga i ponovno pokrenite poslužitelj. Provjerite korisničko ime u postavkama i preuzimanje titla.

Pri prvom pokretanju novi dodatak predviđen je za **jednokratni uvoz** korisničkog imena i lozinke izvornog dodatka, ali samo ako novija konfiguracija nema pristupne podatke. Izvorna konfiguracijska datoteka ostaje sačuvana, a stari API token **ne prenosi se**. Nakon nadogradnje provjerite pristupne podatke u postavkama i po potrebi ih ponovno unesite.

**Nadogradnja samog Jellyfin poslužitelja s 10.11 na 12 zaseban je postupak.** Prethodno izradite sigurnosnu kopiju cijelog poslužitelja i slijedite službene Jellyfin upute. Uspješna provjera dodatka ne jamči uspješnu migraciju poslužiteljske baze.

### Ručna instalacija u Jellyfin

Ako instalacija iz kataloga nije dostupna, preuzmite [`titlovi-com_2.0.0.0.zip`](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0) iz službenog Jellyfin izdanja. Zaustavite poslužitelj, spremite sigurnosnu kopiju postojećeg dodatka i konfiguracije, **premjestite staru mapu dodatka izvan aktivnog direktorija dodataka** pa raspakirajte **cijeli** ZIP u novu zasebnu mapu:

```text
Jellyfin.Titlovi.dll
meta.json
thumb.png
```

Ponovno pokrenite poslužitelj. Nemojte instalirati samo DLL ni ostavljati sukobljene kopije dodatka. Za uobičajenu instalaciju i nadogradnju preporučuje se katalog.

## Emby Server 4.10.x

Distribucija: [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby). Instalacija u Emby **ručna je**; ne postoji katalog dodataka Titlovi.com koji se dodaje Embyju.

**Za objavljeni Emby dodatak:**

1. Preuzmite `Emby.Titlovi.dll` sa stranice [službenih Emby izdanja](https://github.com/Titlovi/titlovi-emby/releases).
2. Zaustavite Emby Server i, ako nadograđujete, sigurnosno kopirajte prethodni dodatak i konfiguraciju.
3. Kopirajte DLL u **stvarni direktorij dodataka** poslužitelja; zamijenite samo stariji DLL istog dodatka ako postoji.
4. Pokrenite Emby i otvorite postavke dodatka. Unesite svoje Titlovi.com korisničko ime i lozinku.
5. Provjerite pretraživanje i preuzimanje titla.

Uobičajena mjesta instalacije:

```text
Windows: C:\Users\<user>\AppData\Roaming\Emby-Server\programdata\plugins
Linux:   /var/lib/emby/plugins
```

To su **primjeri, a ne univerzalne putanje**. Stvarnu mapu podataka poslužitelja pronađite putem Emby upravljačke ploče (primjerice **Get Server Info**). Prilagođene instalacije i spremnici imaju drukčije putanje; u Dockeru koristite trajni volumen dodataka.

**Poznato ograničenje Embyja 4.10:** Pri spremanju titla na alternativnom jeziku Emby može upotrijebiti sufiks jezika prvotno odabranog u pretraživanju, iako je stvarni jezik u rezultatima ispravno prikazan.

## Postavke, poteškoće i povratak na prethodnu verziju

Nakon instalacije u postavkama možete odabrati formate titlova, alternativne hrvatske/srpske/bosanske rezultate, pretvorbu u UTF-8 i dijagnostičke zapise. Pogledajte [Korisnički vodič](USER-GUIDE.md).

Ako se dodatak ne učitava, provjerite inačicu poslužitelja, odgovarajući paket, direktorij instalacije i ponovno pokretanje. Ako pretraživanje ne radi, provjerite korisničko ime i lozinku i odabrani jezik.

Za povratak na starije izdanje vratite **kompatibilan** prethodni paket i sigurnosnu kopiju konfiguracije. Ne pokušavajte vraćati Jellyfinovu bazu na stariju inačicu samo radi zamjene dodatka.

**Sigurnost:** Ne objavljujte lozinke, API tokene ni konfiguracijske XML datoteke u javnim prijavama problema.
