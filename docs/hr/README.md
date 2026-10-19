# Titlovi.com za Jellyfin i Emby

**Službeni izvorni kod dodataka Titlovi.com za Jellyfin i Emby.** Dodaci omogućuju pretraživanje i preuzimanje titlova sa stranice [Titlovi.com](https://titlovi.com/) izravno iz medijskog poslužitelja. Oba se dodatka razvijaju iz zajedničkog izvornog koda.

**Dokumentacija:** [English](../../README.md) · **Hrvatski** · [Српски](../sr-Cyrl/README.md)

> **Status izdanja:** Prvo službeno izdanje objedinjene implementacije, **2.0.0.0**, još se priprema. Instalacijski paketi za Jellyfin 12 i Emby 4.10.x **nisu objavljeni**. Ovaj repozitorij sadrži izvorni kod, a ne datoteke za preuzimanje dodataka.

## Podržani poslužitelji

| Medijski poslužitelj | Podržana inačica | Distribucija i instalacija |
| --- | --- | --- |
| **Jellyfin** | **12.x** (.NET 10) | Službeni katalog dodataka i izdanja u repozitoriju [Titlovi/titlovi-jellyfin](https://github.com/Titlovi/titlovi-jellyfin). Postojeća adresa kataloga ostaje nepromijenjena. |
| **Emby Server** | **4.10.x** | Ručna instalacija datoteke `Emby.Titlovi.dll` iz budućih izdanja u repozitoriju [Titlovi/titlovi-emby](https://github.com/Titlovi/titlovi-emby). |

Navedene inačice odnose se na **nadolazeće izdanje Unified**. Ne pokušavajte instalirati 2.0 prije službene objave. U postojećem Jellyfin katalogu i dalje su dostupna ranija izdanja za odgovarajuće inačice Jellyfina.

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

[Upute za instalaciju](INSTALL.md) opisuju planirani postupak nakon objave, a [korisnički vodič](USER-GUIDE.md) postavke i pretraživanje. **Službeni paketi 2.0 još nisu dostupni.**

## Izvorni kod i razvoj

Ovo je **središnji repozitorij izvornog koda** za oba dodatka. Svaki dodatak gradi se zasebno sa zajedničkom logikom; nije potrebna dodatna Common/Core DLL biblioteka.

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
