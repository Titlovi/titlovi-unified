# Korisnički vodič za Titlovi.com

[English](../en/USER-GUIDE.md) · **Hrvatski** · [Српски](../sr-Cyrl/USER-GUIDE.md)

Dodatak omogućuje pretraživanje i preuzimanje titlova sa stranice Titlovi.com iz Jellyfina ili Embyja. **Unified 2.0.0.0 objavljen je za Jellyfin 12 i Emby 4.10.x**. Preuzimanje, instalacija i nadogradnja opisani su u [uputama za instalaciju](INSTALL.md).

## Preduvjeti i postavke

Potreban je kompatibilan medijski poslužitelj i **Titlovi.com korisnički račun**. Nakon instalacije odgovarajućeg dodatka i ponovnog pokretanja poslužitelja otvorite njegove postavke.

| Postavka | Namjena |
| --- | --- |
| Korisničko ime / lozinka | Prijava u servis Titlovi.com. |
| Prihvaćeni formati | Uključivanje ili isključivanje SRT, SUB, TXT, SSA i ASS titlova. |
| Alternativni hrvatski/srpski/bosanski | Prikaz rezultata na preostala dva jezika kad pretražujete jedan od njih. Zadano isključeno. |
| Pretvorba preuzetih titlova u UTF-8 | Pretvorba starijih kodiranja znakova kada je potrebna. Zadano isključeno. |
| Detaljna dijagnostika | Dodatni zapisi radi rješavanja problema. Zadano isključeno. |

Svi podržani formati titlova početno su prihvaćeni. Spremite promjene prije pretraživanja.

## Pretraživanje i preuzimanje

1. Otvorite film ili epizodu u medijskom poslužitelju.
2. U njegovu ugrađenom sučelju za **pretraživanje/preuzimanje titlova** odaberite jezik.
3. Pregledajte rezultate, posebno jezik titla i podatke o izdanju videosnimke.
4. Odaberite odgovarajući titl i preuzmite ga preko poslužitelja.

Pretraživanje može koristiti IMDb identifikator i, kada je potrebno, rezervno pretraživanje po naslovu. Kod serija koriste se dostupni podaci o sezoni i epizodi.

### Alternativni jezici

Ako uključite alternativno pretraživanje, rezultati za hrvatski mogu uključiti i srpski i bosanski, i obratno. Zadržava se **stvarni jezik koji navodi Titlovi.com**: hrvatski titl pronađen tijekom srpskog pretraživanja ostaje označen kao hrvatski.

Titlovi s **različitim Titlovi.com medijskim identifikatorima** ostaju zasebni rezultati. Ponavljanja istog identifikatora mogu se ukloniti.

### Rangiranje prema izdanju

Izvor videosnimke, izdavačka skupina, broj sličica u sekundi (FPS) i posebna verzija/montaža važni su pokazatelji podudaranja; rezolucija, kodek i drugi podaci imaju manju težinu. **Podudaranje služi samo rangiranju** — titl se ne uklanja zato što oznaka izdanja nije jednaka.

### Arhive i epizode

Podržani su pojedinačni titlovi, arhive cijelih sezona ili serija, ugniježđeni ZIP-ovi, stariji nazivi datoteka i određeni zapisi za više epizoda. Dodatak nastoji izabrati titl za traženu epizodu, umjesto da preuzima prvu datoteku iz arhive.

## Formati i kodiranje

Podržani su **SRT, SUB, TXT, SSA i ASS**. U postavkama možete isključiti pojedine formate. Ako su znakovi poput č, ć, đ, š ili ž pogrešno prikazani, razmotrite uključivanje neobavezne **pretvorbe u UTF-8**.

**Zašto se titl prikazuje u rezultatima, ali ga nije moguće preuzeti?** Rezultati pretraživanja na Titlovi.com ne otkrivaju stvarni format datoteke titla. Dodatak ga može provjeriti tek nakon preuzimanja i pregleda datoteke ili arhive. Zato se među rezultatima može pojaviti i titl čiji je jedini format isključen u postavkama (primjerice SUB). Ako preuzeti sadržaj ne sadrži nijedan titl u dopuštenom formatu, preuzimanje neće uspjeti; to ne mora značiti da dodatak ili Titlovi.com ne radi ispravno. Uključite taj format ako ga želite prihvatiti ili odaberite drugi rezultat.

## Razlike između Jellyfina i Embyja

Jellyfin 12 sprema titl prema jeziku koji mu dostavi dodatak.

U **Emby Serveru 4.10.x** titl na alternativnom jeziku može dobiti sufiks jezika *prvotno traženog* u pretraživanju. Primjer: hrvatski titl pronađen tijekom srpskog pretraživanja može biti spremljen s nastavkom `.sr.srt`, iako rezultat ispravno navodi hrvatski. To je prihvaćeno ograničenje poslužitelja.

## Rješavanje problema i privatnost

| Problem | Što provjeriti |
| --- | --- |
| Dodatak se ne učitava | Inačicu poslužitelja, odgovarajući paket, sadržaj Jellyfin ZIP-a i ponovno pokretanje. |
| Prijava ne uspijeva | Korisničko ime i lozinku za Titlovi.com. |
| Nema rezultata | Metapodatke videosnimke, jezik, dopuštene formate i račun. |
| Titl je ponuđen, ali preuzimanje ne uspijeva | Jedan od mogućih razloga jest to što preuzeti titl sadrži samo formate isključene u postavkama, kao što je objašnjeno iznad. Pokušajte uključiti format ili odabrati drugi rezultat. |
| Pogrešan titl za epizodu | Oznake sezone/epizode, nazive u arhivi i druge ponuđene rezultate. |
| Neispravni znakovi | Neobaveznu pretvorbu u UTF-8. |
| Neobičan jezični sufiks u Embyju | Poznato ograničenje Embyja 4.10 opisano iznad. |

Dijagnostički zapisi mogu sadržavati naslove i nazive datoteka. **Nikada ne objavljujte lozinke, API tokene ni konfiguracijski XML.**

Probleme koji se mogu ponoviti prijavite putem [GitHub Issues](https://github.com/Titlovi/titlovi-unified/issues), uz inačice poslužitelja i dodatka te opis koraka bez osjetljivih podataka.
