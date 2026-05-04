<div align="center">

# 🏃‍♂️ SCP Escape System
**A game-changing EXILED plugin that allows SCPs to escape the facility and survive.**

[![EXILED](https://img.shields.io/badge/EXILED-9.0%2B-blue.svg)](#)
[![SCP:SL](https://img.shields.io/badge/SCP:SL-latest-green.svg)](#)
[![Downloads](https://img.shields.io/github/downloads/TwojaNazwaUzytkownika/ScpEscapeSystem/total.svg)](https://github.com/TwojaNazwaUzytkownika/ScpEscapeSystem/releases)
[![License](https://img.shields.io/badge/License-MIT-red.svg)](#)

</div>

## 📖 About
Tired of SCPs camping on the surface waiting for the round to end? **SCP Escape System** totally overhauls the late-game dynamics. It allows SCPs (including SCP-049-2 and Custom Roles) to escape the facility through the standard Class-D/Scientist escape zone. 

To maintain balance, the exterior gates remain locked until the first MTF or Chaos Insurgency wave arrives. Once the gates are open, escaping SCPs transform into fully armed Chaos Insurgents, triggering global broadcasts and smart end-round calculations.

## ✨ Features

*   🚪 **Balanced Escape Mechanics:** SCPs cannot escape at the start of the round. The escape route unlocks dynamically upon the first MTF/Chaos respawn.
*   🎭 **100% Custom Role Support:** Fully compatible with `Exiled.CustomRoles`. Custom SCPs (like SCP-066, SCP-153, etc.) are recognized automatically and can escape!
*   🧟 **Zombie Integration:** SCP-049-2 instances have a reason to survive! They can escape and become Chaos Insurgents.
*   💻 **SCP-079 Network Transfer:** If all physical SCP teammates die or escape, SCP-079 successfully "transfers its database" outside the facility, surviving the round.
*   🏆 **Smart End-Round Logic:** The plugin intelligently overrides round summaries. If humans also escaped, it forces a **Draw**. If only SCPs escaped, Anomalies secure the **Victory**.
*   💬 **Rich Text Broadcasts:** Beautiful, customizable on-screen hints and global broadcasts.

## 🚀 Installation

1. Download the latest `ScpEscapeSystem.dll` from the [Releases](https://github.com/TwojaNazwaUzytkownika/ScpEscapeSystem/releases) page.
2. Place the `.dll` file into your `EXILED/Plugins` folder.
3. Restart your server.
4. Customize the plugin via the generated config file in `EXILED/Configs`.

## ⚙️ Configuration

The plugin is highly customizable. Below are the default settings you can find in your `config_gameplay.yml`:

| Config Option | Default Value | Description |
| :--- | :--- | :--- |
| `is_enabled` | `true` | Enables or disables the plugin. |
| `debug` | `false` | Enables debug messages in the server console. |
| `allowed_scps` | `[Scp049, Scp0492, ...]` | List of base-game SCPs allowed to escape. *(Custom Roles are allowed by default)*. |
| `escape_locked_hint` | `<b><color=orange>[SYSTEM]</color></b>...` | The hint displayed if an SCP touches the escape zone before MTF arrives. |
| `scp_escaped_broadcast` | `<b><size=40><color=red>...` | The global message sent when an SCP escapes. `{scp}` is replaced with the name. |
| `scp079_transferred_broadcast`| `<b><size=40><color=#00FFFF>...` | The global message sent when SCP-079 survives as the last SCP. |

## 🛠️ Requirements
* **EXILED** (Version 9.0.0 or higher)
* **SCP: Secret Laboratory** (Target Framework: .NET 4.8)

## 🤝 Contributing
Feel free to open an issue or submit a pull request if you want to add new features or report bugs!

---
<div align="center">
  <sub>Created with ❤️ by <b>Matysiak</b></sub>
</div>
