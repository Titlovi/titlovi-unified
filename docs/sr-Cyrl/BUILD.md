# Компајлирање додатака из изворног кода

[English](../en/BUILD.md) · [Hrvatski](../hr/BUILD.md) · **Српски**

Ово упутство описује компајлирање два самостална додатка за **Jellyfin 12 / .NET 10** и **Emby Server 4.10.x** из [централног репозиторијума](https://github.com/Titlovi/titlovi-unified).

> Званична [ознака изворног кода Unified `v2.0.0`](https://github.com/Titlovi/titlovi-unified/tree/v2.0.0) извор је [објављеног Emby 2.0.0.0 издања](https://github.com/Titlovi/titlovi-emby/releases/tag/v2.0.0). Jellyfin 2.0 још није објављен. Компајлирање из изворног кода намењено је развоју; за инсталацију користите одобрена издања за одговарајући сервер.

## Организација и предуслови

```text
src/Common/       Заједничка логика независна од медијског сервера
src/Jellyfin/     Интеграција за Jellyfin 12
src/Emby/         Интеграција за Emby 4.10.x
tests/            Регресиони и интеграциони тестови
scripts/windows/  Скрипте за Windows
scripts/linux/    Скрипте за Linux
```

Датотеке из `src/Common` уграђују се **директно у DLL сваког додатка**.

| Окружење | Захтеви |
| --- | --- |
| Windows | PowerShell 5.1+, Git, интернет веза за пакете и референце које нису у кешу. **Visual Studio није неопходан**. |
| Linux | Bash, Git, стабилан **.NET 10 SDK или новији**, приступ NuGet пакетима. |
| Jellyfin паковање на Linux-у | Python 3. |
| Emby компајлирање | Референтне библиотеке `MediaBrowser.Model.dll`, `MediaBrowser.Common.dll` и `MediaBrowser.Controller.dll` — само за време компајлирања. |

**Emby Server НЕ мора бити инсталиран на рачунару на којем се додатак компајлира.**
Потребне су само три `MediaBrowser.*` DLL датотеке као референце за
компајлирање; поступак не инсталира нити покреће Emby Server.
Званичне скрипте прво траже локалне референце, а ако их нема,
могу их аутоматски преузети из званичних Emby дистрибуција.
На рачунару без раније преузетих референци за то су неопходни
приступ интернету и одговарајући алат за распакивање, описани
у наставку. **Компајлирање мора остати могуће и без локалне
инсталације Emby Servera.**

Windows скрипте бирају инсталирани стабилни .NET 10 SDK, а по потреби преузимају предвиђени преносиви SDK. Ако локалне Emby референце нису доступне, преузимају их из званичног пакета Emby 4.10; за распакивање је потребна Windows команда `tar`. Пробне и RC верзије SDK-а се не бирају.

На Linux-у се Emby референце траже у `EMBY_REFERENCE_DIR`, у уобичајеним инсталационим директоријумима и најзад у Docker слици `emby/embyserver:4.10.0.40`. Docker је потребан само за последњу могућност и проширене тестове.

## Преузимање и компајлирање

```console
git clone https://github.com/Titlovi/titlovi-unified.git
cd titlovi-unified
```

**Све команде покрећите из коренског директоријума репозиторијума**, а не из `docs/sr-Cyrl`.

### Windows Command Prompt

```bat
scripts\windows\run-tests.cmd
scripts\windows\build-emby.cmd
scripts\windows\build-jellyfin.cmd
```

Компајлирање оба додатка:

```bat
scripts\windows\build-all.cmd
```

### Linux

```bash
bash scripts/linux/run-tests.sh
bash scripts/linux/build-emby.sh
bash scripts/linux/build-jellyfin.sh
```

Или компајлирање оба додатка:

```bash
bash scripts/linux/build-all.sh
```

Ако не желите Docker за прибављање Emby референци, поставите `EMBY_REFERENCE_DIR` на фасциклу са све три потребне DLL датотеке.

## Резултати компајлирања

Привремени подаци, референце, дневници рада и резултати тестова смештају се у **`.build/`**, а датотеке за инсталацију у **`dist/`**. Оба директоријума изузета су из Git-а.

| Сервер | Излазна датотека |
| --- | --- |
| Emby | `dist/Emby/Emby.Titlovi.dll` |
| Jellyfin | `dist/Jellyfin/titlovi-com_<version>.zip` |
| Jellyfin распакована фасцикла | `dist/Jellyfin/Titlovi.com_<version>/` |

Jellyfin ZIP мора садржати **тачно** `Jellyfin.Titlovi.dll`, `meta.json` и `thumb.png` у корену архиве. Windows и Linux производе исту структуру пакета независну од оперативног система.

Не инсталирајте Emby DLL у Jellyfin нити обратно. Погледајте [Упутство за инсталацију](INSTALL.md).

## Тестови

Регресиони тестови **не шаљу захтеве сервису Titlovi.com**; за почетно преузимање NuGet пакета ипак може бити потребна интернет веза. Тестира се исти заједнички код који се уграђује у стварне додатке.

Проширени Linux тест учитавања захтева Docker, curl и Python 3:

```bash
bash tests/smoke/linux-runtime-smoke.sh
```

Он прави оба пакета и учитава их у контејнере за **Emby 4.10** и **Jellyfin 12**, ради провере компатибилности са сервером. Не контактира Titlovi.com.

**Аутентификовани end-to-end тестови покрећу се само наменски**, јер користе стварни сервис и скривене приступне податке. Детаљи су у [E2E документацији](../../tests/e2e/README.md). Приступне податке, токене и конфигурациони XML никада не додајте у Git.

Јавни репозиторијум редовно извршава регресионе тестове и компајлирање на Windows-у и Ubuntu-у. Стандардни GitHub runner-и за јавне репозиторијуме не троше стару приватну месечну квоту од 2.000 минута. Тестови са стварним API-јем остају контролисани.

## Дневници рада и проблеми

```text
.build/logs/Tests/test.log
.build/logs/Emby/build.log
.build/logs/Jellyfin/build.log
```

Након промене ревизије можете обрисати `.build/` и `dist/` како бисте очистили застареле резултате; скрипте ће их поново направити.

За техничке границе видети [Архитектуру (енглески)](../development/ARCHITECTURE.md), а за регресионе случајеве [Тестни корпус (енглески)](../../tests/TEST-CORPUS.md).
