# Titlovi.com za Jellyfin i Emby

**Dokumentacija:** [English](../../README.md) · **Hrvatski** · [Српски](../sr-Cyrl/README.md)

**Službeni izvorni kod dodataka Titlovi.com za Jellyfin i Emby.** Dodaci omogućuju pretraživanje i preuzimanje titlova sa stranice [Titlovi.com](https://titlovi.com/) izravno iz medijskog poslužitelja. Oba se dodatka razvijaju iz zajedničkog izvornog koda.

> **Službena izdanja:** [Emby 2.0.0.0](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0) za Emby Server 4.10.x i [Jellyfin 2.0.0.0](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0) za Jellyfin 12 su objavljeni. Ovaj repozitorij sadrži zajednički izvorni kod, a instalacijski paketi dostupni su u zasebnim repozitorijima.

## Podržani poslužitelji

| Medijski poslužitelj | Podržana inačica | Distribucija i instalacija |
| --- | --- | --- |
| **Jellyfin** | **12.x** (.NET 10) | Službeni katalog dodataka i izdanja u repozitoriju [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin). Postojeća adresa kataloga ostaje nepromijenjena. |
| **Emby Server** | **4.10.x** | Ručna instalacija datoteke `Emby.Titlovi.dll` iz [službenog Emby izdanja](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0). |

**Unified 2.0.0.0 dostupan je za oba podržana poslužitelja.** Korisnici Jellyfina 12 mogu ga instalirati putem [postojećeg kataloga Titlovi.com](https://titlovi.github.io/titlovi-jellyfin/repository.json). Ranija izdanja 1.x ostaju dostupna za odgovarajuće starije verzije Jellyfina. **Unified 2.0 ne podržava Jellyfin 10.x.**

## Mogućnosti

- **Filmovi i serije:** pretraživanje i preuzimanje titlova kroz sučelje medijskog poslužitelja.
- **Prilagodljivo pretraživanje:** pretraživanje putem IMDb identifikatora, uz pretraživanje po naslovu kada je potrebno.
- **Hrvatski, srpski i bosanski titlovi:** neobavezno uključivanje rezultata na ostala dva jezika uz očuvanje stvarnog jezika koji navodi Titlovi.com.
- **Rangiranje prema izdanju videosnimke:** bolja podudaranja mogu dobiti prednost, ali se drugi valjani rezultati ne skrivaju.
- **Arhive titlova:** cijele sezone i serije, ugniježđene ZIP arhive, titlovi za više epizoda i stariji načini imenovanja datoteka.
- **Formati:** SRT, SUB, TXT, SSA i ASS, uz neobaveznu pretvorbu preuzetih titlova u UTF-8.
- **Dijagnostika:** dodatno zapisivanje informacija radi rješavanja problema.

Titlovi s različitim Titlovi.com medijskim identifikatorima ostaju **zasebni rezultati**, čak i kada imaju slične nazive ili oznake izdanja.

### Poznato ograničenje u Embyju

Emby Server 4.10.x može spremiti titl na drugom jeziku sa sufiksom jezika prvotno odabranog u pretraživanju. Primjerice, hrvatski titl pronađen pretraživanjem na srpskom može dobiti sufiks `.sr.srt`. U rezultatima se i dalje prikazuje **stvarni jezik titla**.

## Preduvjeti

- Kompatibilna inačica Jellyfina ili Emby Servera.
- **Korisnički račun na Titlovi.com s API pristupom.** Uvjete pristupa i pravo korištenja API-ja određuje Titlovi.com.

[Upute za instalaciju](INSTALL.md) opisuju instalaciju i nadogradnju oba objavljena dodatka, a [korisnički vodič](USER-GUIDE.md) postavke i pretraživanje.

## Izvorni kod i razvoj

Ovo je **središnji repozitorij izvornog koda** za oba dodatka. Svaki dodatak gradi se zasebno sa zajedničkom logikom.

```text
src/Common/       Zajednički mehanizam za pretraživanje i preuzimanje
src/Jellyfin/     Integracija za Jellyfin
src/Emby/         Integracija za Emby
tests/            Regresijski i integracijski testovi
scripts/          Skripte za izgradnju, pakiranje i testiranje
```

- [Upute za izgradnju i testiranje](BUILD.md)
- [Arhitektura i razvojni detalji (engleski)](../development/ARCHITECTURE.md)
- [Povijest razvojnih promjena (engleski)](../../CHANGELOG.md)
- [Prijava problema](https://github.com/Titlovi/titlovi-unified/issues)

Automatizirani testovi i izgradnja provode se na Windowsu i Linuxu. Testovi koji pristupaju stvarnom servisu Titlovi.com pokreću se samo namjenski i zahtijevaju zasebno konfigurirane pristupne podatke u GitHub Actionsu.

**Sigurnost:** Ne objavljujte lozinke, API tokene, konfiguracijske datoteke koje sadrže pristupne podatke ni osjetljive dijelove zapisnika.

## Licencija i zahvale

Projekt se distribuira pod licencijom **GNU General Public License v3.0**. Pogledajte [LICENSE](../../LICENSE) i [NOTICE.txt](../../NOTICE.txt).

Izvorni Jellyfin dodatak autora Matka i njegova ranija izdanja ostaju sačuvani u [Jellyfin repozitoriju](https://github.com/Titlovi/titlovi-jellyfin).

Emby i Jellyfin neovisni su projekti i nisu odgovorni za održavanje ovog dodatka.
