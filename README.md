# SCP Escape System

Plugin EXILED umożliwiający SCP ucieczkę przez standardową strefę Class-D/Naukowców i zmianę w Chaos Conscript.

**Source version: 1.4.0 · .NET Framework 4.8 · compiled against EXILED 9.12.6**

[Releases](https://github.com/MatysiakQ/JastrzabDEV--SCPEscapeSystem/releases) · [Build instructions](docs/BUILD.md) · [Changelog](CHANGELOG.md)

Build 1.4.0: [ScpEscapeSystem.dll](dist/ScpEscapeSystem.dll) · [SHA-256](dist/SHA256SUMS.txt) · [build information](dist/BUILDINFO.json). This build has not been smoke-tested on a running game server.

## Polski

### Działanie

- Droga ucieczki otwiera się po zakończonej fali, w której faktycznie odrodzili się żywi gracze MTF lub Chaos. Anulowana lub pusta fala jej nie odblokowuje.
- Dozwolony SCP w strefie ucieczki zmienia się w **Chaos Conscript**. Sukces i komunikat są zatwierdzane dopiero po skutecznej zmianie klasy oraz usunięciu zarejestrowanych ról customowych.
- SCP-079 transferuje się, gdy wcześniej uciekł przynajmniej jeden SCP, a komputer jest jedynym żywym SCP. Sprawdzane są zakończone śmierci, zmiany klas i rozłączenia. Anulowana śmierć nie jest odejściem SCP.
- Jeśli uciekł SCP, wynik to **Anomalie**, a jeżeli potwierdzono też ucieczkę Class-D lub Naukowca — **remis**. Ta reguła jest celowym ustawieniem balansu.
- Konwersje SCP nie są naliczane jako ucieczki Class-D. Dla integracji służą osobne zdarzenia opisane poniżej.
- Przeładowanie pluginu zachowuje stan bieżącej rundy w tej samej scenie. Nowa runda resetuje stan. Dane nie są zapisywane na dysku.
- Wskazówki i ponawianie anulowanych prób mają ograniczoną częstotliwość.

### Instalacja

1. Zainstaluj EXILED z modułem `Exiled.CustomRoles`.
2. Zbuduj aktualne źródła zgodnie z [instrukcją](docs/BUILD.md) albo użyj DLL odpowiadającej tej wersji. Starsze pliki w Releases mogą pochodzić z wcześniejszych wersji.
3. Usuń poprzednią DLL tego pluginu, także jeśli nazywa się `SCPRUN.dll`.
4. Umieść **`ScpEscapeSystem.dll`** w `EXILED/Plugins` i zrestartuj serwer.
5. Edytuj sekcję **`scp_escape_system`** w konfiguracji EXILED.

Przy aktualizacji zachowaj istniejące komunikaty. Puste komunikaty wyłączają ich wyświetlanie. Nie trzeba usuwać całej konfiguracji.

### Konfiguracja

| Klucz | Domyślnie | Znaczenie |
| --- | --- | --- |
| `is_enabled` | `true` | Włączenie pluginu. |
| `debug` | `false` | Ustawienie debugowania EXILED. |
| `allowed_scps` | 049, 049-2, 096, 106, 173, 939, 3114 | Dozwolone bazowe role SCP. |
| `allow_custom_roles` | `true` | Zarejestrowane CustomRoles z drużyny SCP mogą uciekać także poza listą bazową. |
| `escape_retry_seconds` | `2` | Odstęp między ponowieniami próby ucieczki. |
| `locked_hint_interval` | `3` | Odstęp między hintami zamkniętej drogi. |
| `escape_locked_hint` | komunikat angielski | Hint przed odblokowaniem. |
| `gates_opened_broadcast` | komunikat angielski | Broadcast o odblokowaniu. |
| `scp_escaped_broadcast` | komunikat angielski | Sukces fizycznej ucieczki; `{scp}` to nazwa roli. |
| `scp079_transferred_broadcast` | komunikat angielski | Sukces transferu SCP-079. |

Ustaw oba pola `allowed_scps: []` i `allow_custom_roles: false`, aby wyłączyć fizyczne ucieczki wszystkich SCP. SCP-079 ma osobną regułę transferu, wymagającą wcześniejszego sukcesu SCP w tej rundzie.

### Weryfikacja i ograniczenia

Testy regresji używają zastępczego API. Nie uruchamiają klientów, Unity, natywnej kolejki recontainmentu ani kodu umiejętności innych pluginów. Zweryfikuj na serwerze SCP-079 i każdy własny SCP, szczególnie sprzątanie modeli, audio i umiejętności. Nie deklarujemy automatycznie zgodności ze wszystkimi przyszłymi wersjami gry lub EXILED.

## English

### Behavior

The escape route unlocks only after a completed wave has actual living MTF/Chaos players. Allowed physical SCPs entering the normal escape zone become Chaos Conscripts. A cancelled or failed class change awards no success.

After at least one confirmed SCP escape, SCP-079 transfers when it is the only living SCP. Completed deaths, class changes and disconnects trigger re-evaluation. Cancelled deaths do not remove a teammate.

The round-result rule is unchanged: a confirmed SCP escape means Anomalies win unless a Class-D/Scientist also successfully escaped, in which case the result is a draw. SCP conversions never increment the native Class-D escape counter.

Reloading the plugin preserves primitive round state in EXILED session variables for the same scene. New rounds clear it. Hints and failed attempts are throttled; empty messages disable those messages. Registered custom roles are removed after a confirmed conversion with their removal-death flag temporarily suppressed and then restored.

### Installation and configuration

Install EXILED and its CustomRoles module. Build matching server references using [docs/BUILD.md](docs/BUILD.md). Remove any old `SCPRUN.dll` or duplicate plugin DLL, then install **`ScpEscapeSystem.dll`** into `EXILED/Plugins`. The configuration prefix is explicitly **`scp_escape_system`**.

The configuration table above lists exact keys. Existing allowed-role and message keys are preserved. New controls are `allow_custom_roles`, `escape_retry_seconds` and `locked_hint_interval`. Invalid intervals are replaced with defaults; a null list becomes empty. Source version, uploaded release labels and DLL names should be checked together before deployment.

### Integration events

SCP conversions intentionally use `SpawnReason.ForceClass` rather than impersonating human escapes. Vanilla/EXILED `Escaped` remains for real human escapes. Observe or cancel SCP attempts using the plugin instance:

```csharp
ScpEscapeSystem.Plugin.Instance.ScpEscaping += OnScpEscaping;
ScpEscapeSystem.Plugin.Instance.ScpEscaped += OnScpEscaped;

void OnScpEscaping(object sender, ScpEscapeSystem.ScpEscapingEventArgs ev)
{
    // ev.Player, ev.OldRole, ev.RoleName, ev.IsNetworkTransfer
    // ev.IsAllowed = false; // cancel without changing the player's class
}

void OnScpEscaped(object sender, ScpEscapeSystem.ScpEscapedEventArgs ev)
{
    // Confirmed Chaos class and registered CustomRole cleanup.
}
```

Unsubscribe during your plugin's disable lifecycle and rebind after an escape-plugin reload. Any denial or throwing attempt listener cancels the attempt. Exceptions in completion listeners are logged without undoing a committed escape. Native `Escaping.IsAllowed` defaults to false for SCPs, so it cannot reliably distinguish a third-party denial from vanilla's default: use `ScpEscaping` for SCP escape policies.

The built-in cleanup handles registered CustomRoles. Independently managed schematics, delayed callbacks and abilities must still be cleaned up by their owning plugin, for example through its role-removed hook. A provider that throws or changes the class during cleanup prevents a success award; provider exceptions are logged. Arbitrary third-party state cannot be rolled back generically.

### Validation

```sh
dotnet run --project tests/Regression/Regression.csproj --configuration Release
```

The tests compile the actual plugin sources against a small API simulation, without game DLLs or external test packages. They verify denial, event ordering, confirmed conversion, counters, CustomRole flags, reloads and stale callbacks. They do not establish Unity/Mono, native SCP-079 recontainment timing or client compatibility. Perform the server smoke tests in [docs/BUILD.md](docs/BUILD.md) before release.

## License

MIT — see [LICENSE.txt](LICENSE.txt).

