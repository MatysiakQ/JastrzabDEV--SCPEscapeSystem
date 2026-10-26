<div align="center">

# 🏃‍♂️ SCP Escape System
**An EXILED plugin that allows SCPs to escape the facility and survive. / Plugin EXILED pozwalający SCP uciec z placówki.**

[![EXILED](https://img.shields.io/badge/EXILED-9.0%2B-blue.svg)](#)
[![SCP:SL](https://img.shields.io/badge/SCP:SL-latest-green.svg)](#)
[![Version](https://img.shields.io/badge/Version-1.3.0-orange.svg)](#)
[![Downloads](https://img.shields.io/github/downloads/TwojaNazwaUzytkownika/ScpEscapeSystem/total.svg)](https://github.com/TwojaNazwaUzytkownika/ScpEscapeSystem/releases)
[![License](https://img.shields.io/badge/License-MIT-red.svg)](#)

</div>

---

# 🇬🇧 English

## 📖 About
Tired of SCPs camping on the surface waiting for the round to end? **SCP Escape System** changes the late-game dynamics. It lets SCPs (including SCP-049-2 and Custom Roles) escape the facility through the standard Class-D/Scientist escape zone.

To maintain balance, the exterior gates stay locked until the first MTF or Chaos Insurgency wave arrives. Once the gates are open, an escaping SCP becomes a **Chaos Insurgency Conscript**, a global broadcast is shown, and the end-of-round result is adjusted.

## ✨ Features

* 🚪 **Balanced escape mechanics:** SCPs cannot escape from the start of the round. The route unlocks on the first MTF/Chaos respawn wave (a respawn cancelled by another plugin does not unlock it).
* 🎭 **Custom Role support:** works with `Exiled.CustomRoles`. Registered custom roles on the SCP team are recognized automatically and may escape.
* 🧟 **Zombie support:** SCP-049-2 is on the default allowed list and becomes a Chaos Conscript when it escapes.
* 💻 **SCP-079 network transfer:** SCP-079 cannot walk out, so it "transfers" under these conditions: **at least one SCP has already escaped** and SCP-079 is **the last living SCP** in the facility (it does not matter whether the last other SCP escaped or died). SCP-079 then also becomes a Chaos Conscript. If nobody escaped, there is no transfer and no broadcast, and the standard game behavior applies.
* 🏆 **End-round logic:** if at least one SCP escaped, the plugin overrides the result: **Draw** if humans also escaped, otherwise **Anomalies win**.
* 💬 **Customizable messages:** hints and global broadcasts are configurable (rich text supported).

## 📋 Requirements
* **SCP: Secret Laboratory** server with **EXILED 9.x** (the plugin was built against 9.12.6, target framework .NET Framework 4.8).
* **Exiled.CustomRoles** (`Exiled.CustomRoles.dll`) must be present in `EXILED/Plugins`. It is required by the plugin. It normally ships with the standard EXILED release, but if the plugin fails to load, check that this file is there.

## 🚀 Installation

1. Install EXILED (with `Exiled.CustomRoles` in `EXILED/Plugins`).
2. Download the latest `ScpEscapeSystem.dll` from the [Releases](https://github.com/TwojaNazwaUzytkownika/ScpEscapeSystem/releases) page.
3. Put the file into your plugins folder:
   * Windows: `%AppData%\EXILED\Plugins`
   * Linux: `~/.config/EXILED/Plugins`
4. Restart the server. The config is generated on first start.
5. Edit the `scp_escape_system` section (the plugin does not set a custom prefix, so the key is derived from its name) in `EXILED/Configs/<PORT>-config.yml`, or in `EXILED/Configs/Plugins/` if separate configs are enabled. Restart afterwards.

### 🔄 Updating from older versions
Replace the DLL. A new field `gates_opened_broadcast` was added in v1.1.0. If the new field doesn't appear in your config, delete the plugin's config section/file so it regenerates.

## ⚙️ Configuration

| Config option | Default value | Description |
| :--- | :--- | :--- |
| `is_enabled` | `true` | Enables or disables the plugin. |
| `debug` | `false` | Enables debug messages in the console. |
| `allowed_scps` | `Scp049, Scp0492, Scp096, Scp106, Scp173, Scp939, Scp3114` | Base-game SCPs allowed to escape. *(Custom Roles are allowed by default.)* SCP-079 is not on this list on purpose; it escapes only through the transfer rule above. |
| `escape_locked_hint` | `<b><color=orange>[SYSTEM]</color></b>...` | Hint shown if an SCP enters the escape zone before the gates open. |
| `gates_opened_broadcast` | `<b><size=40><color=red>[SYSTEM]</color></size></b>...` | Broadcast shown to living SCPs when the gates open (first MTF/Chaos wave). |
| `scp_escaped_broadcast` | `<b><size=40><color=red>[WARNING]</color></size></b>...` | Global message when an SCP escapes. `{scp}` is replaced with the role name. |
| `scp079_transferred_broadcast` | `<b><size=40><color=#00FFFF>[NETWORK TRANSFER]</color></size></b>...` | Global message when SCP-079 transfers outside the facility. |

> The role given after escaping (Chaos Conscript) is fixed in the code and is not configurable. Broadcast durations are also fixed (7 s for the gate and escape messages, 10 s for the SCP-079 message).

## ⚠️ Known limitations
* If an SCP **disconnects** from the server, SCP-079's transfer check is not triggered (only deaths and escapes are checked).
* The plugin relies on the game's standard escape trigger, so the escape zone works like for Class-D/Scientists.

## 🤝 Contributing
Feel free to open an issue or submit a pull request if you want to add features or report bugs!

---

# 🇵🇱 Polski

## 📖 O projekcie
Masz dość SCP, które kampią na powierzchni w oczekiwaniu na koniec rundy? **SCP Escape System** zmienia dynamikę końcówki gry. Pozwala SCP (w tym SCP-049-2 i Custom Roles) uciec z placówki przez standardową strefę ucieczki Class-D/Naukowców.

Dla balansu zewnętrzne bramy są zablokowane, dopóki nie pojawi się pierwsza fala MTF lub Chaos Insurgency. Gdy bramy się otworzą, uciekający SCP zmienia się w **Chaos Insurgency Conscript**, pojawia się globalny broadcast, a wynik rundy jest odpowiednio korygowany.

## ✨ Funkcje

* 🚪 **Zbalansowana ucieczka:** SCP nie mogą uciec od startu rundy. Droga odblokowuje się przy pierwszej fali respawnu MTF/Chaosu (respawn anulowany przez inny plugin jej nie odblokowuje).
* 🎭 **Obsługa Custom Roles:** działa z `Exiled.CustomRoles`. Zarejestrowane customowe role z drużyny SCP są rozpoznawane automatycznie i mogą uciec.
* 🧟 **Obsługa zombie:** SCP-049-2 jest na domyślnej liście dozwolonych i po ucieczce zostaje Chaos Conscriptem.
* 💻 **Transfer SCP-079:** SCP-079 nie może wyjść na własnych nogach, więc "przesyła się" gdy spełnione są oba warunki: **przynajmniej jeden SCP już uciekł** i SCP-079 jest **ostatnim żywym SCP** w placówce (bez znaczenia, czy ostatni inny SCP uciekł, czy zginął). SCP-079 również zostaje wtedy Chaos Conscriptem. Jeśli nikt nie uciekł, nie ma transferu ani broadcastu, a działa standardowa mechanika gry.
* 🏆 **Logika końca rundy:** jeśli przynajmniej jeden SCP uciekł, plugin zmienia wynik: **remis**, gdy uciekli też ludzie, w przeciwnym razie **wygrywają Anomalie**.
* 💬 **Konfigurowalne komunikaty:** hinty i globalne broadcasty możesz zmieniać (obsługa rich text).

## 📋 Wymagania
* Serwer **SCP: Secret Laboratory** z **EXILED 9.x** (plugin budowany pod 9.12.6, .NET Framework 4.8).
* **Exiled.CustomRoles** (`Exiled.CustomRoles.dll`) musi znajdować się w `EXILED/Plugins`. Plugin go wymaga. Zwykle jest w standardowym pakiecie EXILED, ale jeśli plugin się nie ładuje, sprawdź obecność tego pliku.

## 🚀 Instalacja

1. Zainstaluj EXILED (z `Exiled.CustomRoles` w `EXILED/Plugins`).
2. Pobierz najnowszy `ScpEscapeSystem.dll` z zakładki [Releases](https://github.com/TwojaNazwaUzytkownika/ScpEscapeSystem/releases).
3. Wrzuć plik do folderu pluginów:
   * Windows: `%AppData%\EXILED\Plugins`
   * Linux: `~/.config/EXILED/Plugins`
4. Zrestartuj serwer. Config wygeneruje się przy pierwszym starcie.
5. Edytuj sekcję `scp_escape_system` (plugin nie ustawia własnego prefixu, więc klucz powstaje z jego nazwy) w `EXILED/Configs/<PORT>-config.yml` albo w `EXILED/Configs/Plugins/`, jeśli masz włączone osobne configi. Po zmianach zrestartuj serwer.

### 🔄 Aktualizacja ze starszych wersji
Podmień DLL. W v1.1.0 doszło pole `gates_opened_broadcast`. Jeśli nowe pole nie pojawi się w configu, usuń sekcję/plik configu pluginu, żeby wygenerował się od nowa.

## ⚙️ Konfiguracja

| Opcja | Domyślnie | Opis |
| :--- | :--- | :--- |
| `is_enabled` | `true` | Włącza lub wyłącza plugin. |
| `debug` | `false` | Komunikaty debugowania w konsoli. |
| `allowed_scps` | `Scp049, Scp0492, Scp096, Scp106, Scp173, Scp939, Scp3114` | Bazowe SCP, które mogą uciec. *(Custom Roles są dozwolone domyślnie.)* SCP-079 celowo nie ma na liście, ucieka tylko przez transfer. |
| `escape_locked_hint` | `<b><color=orange>[SYSTEM]</color></b>...` | Hint, gdy SCP wejdzie w strefę ucieczki przed otwarciem bram. |
| `gates_opened_broadcast` | `<b><size=40><color=red>[SYSTEM]</color></size></b>...` | Broadcast dla żywych SCP po otwarciu bram (pierwsza fala MTF/Chaos). |
| `scp_escaped_broadcast` | `<b><size=40><color=red>[WARNING]</color></size></b>...` | Globalny komunikat o ucieczce SCP. `{scp}` zamieniane jest na nazwę roli. |
| `scp079_transferred_broadcast` | `<b><size=40><color=#00FFFF>[NETWORK TRANSFER]</color></size></b>...` | Globalny komunikat o transferze SCP-079 poza placówkę. |

> Rola nadawana po ucieczce (Chaos Conscript) jest wpisana na stałe w kodzie i nie da się jej zmienić w configu. Czasy broadcastów też są stałe (7 s dla bramy i ucieczki, 10 s dla SCP-079).

## ⚠️ Znane ograniczenia
* Jeśli SCP **rozłączy się** z serwera, sprawdzenie transferu SCP-079 się nie odpali (sprawdzane są tylko śmierci i ucieczki).
* Plugin korzysta ze standardowego triggera ucieczki z gry, więc strefa ucieczki działa tak jak dla Class-D/Naukowców.

## 🤝 Współpraca
Śmiało otwieraj Issues lub Pull Requesty z nowymi funkcjami albo zgłoszeniami błędów!

---
<div align="center">
  <sub>Created with ❤️ by <b>Matysiak</b></sub>
</div>
