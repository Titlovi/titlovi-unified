# Izgradnja dodataka iz izvornog koda

[English](../en/BUILD.md) · **Hrvatski** · [Српски](../sr-Cyrl/BUILD.md)

Ovaj vodič opisuje izgradnju dvaju samostalnih dodataka za **Jellyfin 12 / .NET 10** i **Emby Server 4.10.x** iz [središnjeg repozitorija](https://github.com/Titlovi/titlovi-unified).

> Službena [Unified izvorna oznaka `v2.0.0`](https://github.com/Titlovi/titlovi-unified/tree/v2.0.0) izvor je [objavljenog Emby 2.0.0.0 izdanja](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0). Objavljena su oba izdanja: [Emby 2.0](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0) i [Jellyfin 2.0](https://github.com/Titlovi/titlovi-jellyfin/releases/tag/v2.0.0). Izgradnja iz izvornog koda namijenjena je razvoju; za instalaciju koristite odobrena izdanja za odgovarajući poslužitelj.

## Organizacija i preduvjeti

```text
src/Common/       Zajednički mehanizam neovisan o poslužitelju
src/Jellyfin/     Integracija za Jellyfin 12
src/Emby/         Integracija za Emby 4.10.x
tests/            Regresijski i integracijski testovi
scripts/windows/  Skripte za Windows
scripts/linux/    Skripte za Linux
```

Datoteke iz `src/Common` ugrađuju se **izravno u svaki DLL dodatka**.

| Okruženje | Potrebno |
| --- | --- |
| Windows | PowerShell 5.1+, Git, internetska veza za pakete i reference koje nisu u predmemoriji. **Visual Studio nije potreban**. |
| Linux | Bash, Git, stabilni **.NET 10 SDK ili noviji**, mrežni pristup za NuGet pakete. |
| Jellyfin pakiranje na Linuxu | Python 3. |
| Emby kompilacija | Referentne biblioteke `MediaBrowser.Model.dll`, `MediaBrowser.Common.dll` i `MediaBrowser.Controller.dll` — samo tijekom kompilacije. |

**Emby Server NE mora biti instaliran na računalu na kojem se dodatak izgrađuje.**
Za kompilaciju su potrebne samo tri `MediaBrowser.*` DLL datoteke kao
referentne biblioteke; kompajliranje ne instalira niti pokreće Emby Server.
Službene skripte najprije traže lokalne reference, a ako ih nema, mogu ih
automatski pribaviti iz službenih Emby distribucija. Na računalu bez
predmemoriranih referenci za to su potrebni mrežni pristup i odgovarajući
alat za raspakiravanje, opisani u nastavku. **Izgradnja mora ostati moguća
i bez lokalne instalacije Emby Servera.**

Windows skripte odabiru stabilan instalirani .NET 10 SDK, a po potrebi preuzimaju konfigurirani prijenosni SDK. Kada lokalne Emby reference nisu dostupne, preuzimaju ih iz službenog paketa Emby 4.10; za raspakiravanje je potreban Windows `tar`. Pretpregledne i RC inačice SDK-a ne odabiru se.

Na Linuxu se Emby reference traže u `EMBY_REFERENCE_DIR`, u uobičajenim instalacijskim direktorijima i naposljetku u Docker slici `emby/embyserver:4.10.0.40`. Docker je nužan samo za zadnju mogućnost i proširene testove.

## Preuzimanje i izgradnja

```console
git clone https://github.com/Titlovi/titlovi-unified.git
cd titlovi-unified
```

**Sve naredbe pokrećite iz korijena repozitorija**, a ne iz `docs/hr`.

### Windows Command Prompt

```bat
scripts\windows\run-tests.cmd
scripts\windows\build-emby.cmd
scripts\windows\build-jellyfin.cmd
```

Izgradnja oba dodatka:

```bat
scripts\windows\build-all.cmd
```

### Linux

```bash
bash scripts/linux/run-tests.sh
bash scripts/linux/build-emby.sh
bash scripts/linux/build-jellyfin.sh
```

Ili izgradnja oba dodatka:

```bash
bash scripts/linux/build-all.sh
```

Ako ne želite Docker za pribavljanje Emby referenci, postavite `EMBY_REFERENCE_DIR` na mapu sa sve tri potrebne DLL datoteke.

## Rezultati izgradnje

Privremeni podaci, reference, zapisnici i testni rezultati spremaju se u **`.build/`**, a datoteke za instalaciju u **`dist/`**. Oba direktorija isključena su iz Gita.

| Poslužitelj | Izlazna datoteka |
| --- | --- |
| Emby | `dist/Emby/Emby.Titlovi.dll` |
| Jellyfin | `dist/Jellyfin/titlovi-com_<version>.zip` |
| Jellyfin raspakirana mapa | `dist/Jellyfin/Titlovi.com_<version>/` |

Jellyfin ZIP mora sadržavati **točno** `Jellyfin.Titlovi.dll`, `meta.json` i `thumb.png` u korijenu arhive. Windows i Linux daju istu strukturu paketa neovisnu o operacijskom sustavu.

Ne instalirajte Emby DLL u Jellyfin ni obratno. Vidite [Upute za instalaciju](INSTALL.md).

## Testovi

Regresijski testovi **ne šalju zahtjeve servisu Titlovi.com**; za početno preuzimanje NuGet paketa ipak može biti potrebna internetska veza. Testira se isti zajednički kod koji se ugrađuje u stvarne dodatke.

Prošireni Linux test učitavanja zahtijeva Docker, curl i Python 3:

```bash
bash tests/smoke/linux-runtime-smoke.sh
```

On izrađuje oba paketa i učitava ih u kontejnere za **Emby 4.10** i **Jellyfin 12**, radi provjere kompatibilnosti s poslužiteljem. Ne kontaktira Titlovi.com.

**Autentificirani end-to-end testovi pokreću se samo namjenski**, jer koriste stvarni servis i skrivene pristupne podatke. Pojedinosti su u [E2E dokumentaciji](../../tests/e2e/README.md). Pristupne podatke, tokene i konfiguracijski XML nikada ne dodajte u Git.

Javni repozitorij redovito izvršava regresijske testove i izgradnju na Windowsu i Ubuntuu. Standardni GitHub runneri za javne repozitorije ne troše staru privatnu mjesečnu kvotu od 2.000 minuta. Testovi sa stvarnim API-jem ostaju kontrolirani.

## Zapisnici i poteškoće

```text
.build/logs/Tests/test.log
.build/logs/Emby/build.log
.build/logs/Jellyfin/build.log
```

Nakon promjene revizije možete ukloniti `.build/` i `dist/` kako biste očistili zastarjele rezultate; skripte će ih ponovno stvoriti.

Za tehničke granice vidi [Arhitekturu (engleski)](../development/ARCHITECTURE.md), a za slučajeve regresije [Testni korpus (engleski)](../../tests/TEST-CORPUS.md).
