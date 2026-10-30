# Changelog

## 1.4.0

- Confirm successful Chaos conversion before recording an SCP escape, sending success messages or notifying integrations.
- Open the escape route only after a completed wave has actual living MTF/Chaos players.
- Count confirmed Class-D/Scientist escapes, with a check for failed role changes.
- Re-evaluate SCP-079 after completed deaths, spawns, team changes and disconnects. Cancel stale work on round reset or plugin disable.
- Use a separate SCP conversion and public `ScpEscaping`/`ScpEscaped` events; do not increment native Class-D counters.
- Preserve primitive round state across plugin reloads in the same scene; prevent it from crossing into a different scene/round.
- Remove registered custom roles after a successful class change, without their default removal death. Restore provider flags even if an operation fails.
- Normalize nullable configuration and invalid intervals; throttle hints and retries; isolate message and integration-listener exceptions.
- Add an explicit CustomRole opt-out, 38 regression scenarios, CI and build instructions.
- Standardize the assembly name, config prefix, release documentation and version to `ScpEscapeSystem` 1.4.0.

The default round-result rule is unchanged: after an SCP escape, a confirmed human escape means a draw; otherwise Anomalies win. Server/client and custom-SCP integration testing remains required before a public release.
