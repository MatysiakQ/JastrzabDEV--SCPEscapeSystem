using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace ScpEscapeSystem
{
    internal sealed class EventHandlers : IDisposable
    {
        private readonly Plugin plugin;
        private readonly RoundState state = new RoundState();
        private readonly HashSet<Player> pending = new HashSet<Player>();
        private readonly HashSet<Player> departed = new HashSet<Player>();
        private readonly HashSet<Role> countedHumanLives = new HashSet<Role>();
        private readonly Dictionary<Player, float> nextAttempt = new Dictionary<Player, float>();
        private readonly Dictionary<Player, float> nextHint = new Dictionary<Player, float>();
        private bool disposed;
        private bool transferCheckQueued;
        private int generation;

        public EventHandlers(Plugin plugin) => this.plugin = plugin;
        private static bool ActiveRound => Round.IsStarted && !Round.IsEnded;
        public void OnRoundStarted() => ResetState();
        public void OnRestartingRound() => ResetState();
        public void OnWaitingForPlayers() => ResetState();

        private void ResetState()
        {
            generation++;
            pending.Clear();
            departed.Clear();
            countedHumanLives.Clear();
            nextAttempt.Clear();
            nextHint.Clear();
            transferCheckQueued = false;
            state.Reset();
        }

        public void Dispose()
        {
            disposed = true;
            generation++;
            pending.Clear();
            departed.Clear();
            countedHumanLives.Clear();
            nextAttempt.Clear();
            nextHint.Clear();
            // Keep primitive round state for reloads in this scene.
        }

        public void Resume() => QueueTransferCheck();

        public void OnRespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (!ActiveRound || state.IsEscapeUnlocked) return;
            Player[] candidates = ev.Players.Where(p => p != null).ToArray();
            Defer(() =>
            {
                if (state.IsEscapeUnlocked || !candidates.Any(p => Present(p) && p.IsAlive &&
                    (p.Role.Team == Team.FoundationForces || p.Role.Team == Team.ChaosInsurgency))) return;

                state.IsEscapeUnlocked = true;
                foreach (Player player in Player.List.Where(p => Present(p) && p.Role.Team == Team.SCPs).ToArray())
                    PlayerBroadcast(player, plugin.Config.GatesOpenedBroadcast);
            });
        }

        public void OnEscaping(EscapingEventArgs ev)
        {
            Player player = ev.Player;
            if (!ActiveRound || !Present(player) || !player.IsAlive || player.Role.Team != Team.SCPs ||
                player.Role.Type == RoleTypeId.Scp079) return;

            // Vanilla SCP scenarios default to IsAllowed=false. Policies should use ScpEscaping.
            bool baseAllowed = plugin.Config.AllowedScps?.Contains(player.Role.Type) == true;
            if (!baseAllowed && (!plugin.Config.AllowCustomRoles || !MatchingRoles(player).Any())) return;
            ev.IsAllowed = false;

            if (!state.IsEscapeUnlocked)
            {
                float now = Time.realtimeSinceStartup;
                if (!nextHint.TryGetValue(player, out float next) || now >= next)
                {
                    nextHint[player] = now + Config.ValidInterval(plugin.Config.LockedHintInterval, 3f);
                    string hint = plugin.Config.EscapeLockedHint ?? string.Empty;
                    if (hint.Length != 0) SafeMessage(() => player.ShowHint(hint, 5f));
                }
                return;
            }

            if (!BeginAttempt(player)) return;
            Role original = player.Role;
            // Finish the native Escaping dispatch before changing anybody's class.
            Defer(() =>
            {
                try
                {
                    if (SameLife(player, original) && state.IsEscapeUnlocked &&
                        Escape.CanEscape(player.ReferenceHub, out _, out _))
                        TryConvert(player, original, false);
                }
                finally { pending.Remove(player); }
            });
        }

        public void OnEscaped(EscapedEventArgs ev)
        {
            if (!ActiveRound || !Present(ev.Player) || ev.EscapeScenario == EscapeScenario.None ||
                (ev.OldRole.Type != RoleTypeId.ClassD && ev.OldRole.Type != RoleTypeId.Scientist) ||
                ReferenceEquals(ev.OldRole, ev.Player.Role) ||
                (SpawnReason)ev.Player.Role.SpawnReason != SpawnReason.Escaped ||
                (ev.Player.Role.Team != Team.FoundationForces && ev.Player.Role.Team != Team.ChaosInsurgency)) return;

            if (countedHumanLives.Add(ev.OldRole)) state.RecordHumanEscape();
        }

        public void OnDied(DiedEventArgs ev) => QueueTransferCheck();

        public void OnSpawned(SpawnedEventArgs ev)
        {
            QueueTransferCheck();
            Role spawnedRole = ev.Player.Role;
            if (state.IsEscapeUnlocked && spawnedRole.Team == Team.SCPs)
                Defer(() =>
                {
                    if (SameLife(ev.Player, spawnedRole)) PlayerBroadcast(ev.Player, plugin.Config.GatesOpenedBroadcast);
                });
        }

        public void OnLeft(LeftEventArgs ev)
        {
            departed.Add(ev.Player);
            pending.Remove(ev.Player);
            nextAttempt.Remove(ev.Player);
            nextHint.Remove(ev.Player);
            QueueTransferCheck();
        }

        public void OnEndingRound(EndingRoundEventArgs ev)
        {
            if (ev.IsAllowed && state.HasScpEscaped)
                ev.LeadingTeam = state.HasHumanEscaped ? LeadingTeam.Draw : LeadingTeam.Anomalies;
        }

        private void QueueTransferCheck()
        {
            if (!ActiveRound || !state.HasScpEscaped || transferCheckQueued || disposed) return;
            transferCheckQueued = true;
            Defer(() =>
            {
                transferCheckQueued = false;
                Player[] scps = LivingScps();
                if (scps.Length != 1 || scps[0].Role.Type != RoleTypeId.Scp079 || !BeginAttempt(scps[0])) return;
                Player computer = scps[0];
                try { TryConvert(computer, computer.Role, true); }
                finally { pending.Remove(computer); }
            });
        }

        private bool TryConvert(Player player, Role original, bool networkTransfer)
        {
            if (!SameLife(player, original)) return false;
            int currentGeneration = generation;
            CustomRole[] customRoles = MatchingRoles(player).ToArray();
            if (!networkTransfer && plugin.Config.AllowedScps?.Contains(original.Type) != true &&
                (!plugin.Config.AllowCustomRoles || customRoles.Length == 0)) return false;

            string roleName = customRoles.FirstOrDefault()?.Name ?? original.Name;
            var attempt = new ScpEscapingEventArgs(player, original.Type, roleName, networkTransfer);
            plugin.RaiseEscaping(attempt);
            if (!attempt.IsAllowed || generation != currentGeneration || !SameLife(player, original)) return false;
            if (networkTransfer && (!state.HasScpEscaped || LivingScps().Length != 1)) return false;
            customRoles = MatchingRoles(player).ToArray();

            // Keep CustomRoles intact if ChangingRole is subsequently denied.
            bool[] keepFlags = customRoles.Select(role => role.KeepRoleOnChangingRole).ToArray();
            try
            {
                foreach (CustomRole role in customRoles) role.KeepRoleOnChangingRole = true;
                // Escaped would incorrectly increment the native Class-D escape counter.
                player.Role.Set(RoleTypeId.ChaosConscript, SpawnReason.ForceClass, RoleSpawnFlags.All);
            }
            catch (Exception error)
            {
                Log.Error($"SCP escape class change failed: {error}");
                return false;
            }
            finally
            {
                for (int i = 0; i < customRoles.Length; i++) customRoles[i].KeepRoleOnChangingRole = keepFlags[i];
            }

            if (generation != currentGeneration || !IsConverted(player, original)) return false;
            foreach (CustomRole role in customRoles)
            {
                if (!role.Check(player)) continue;
                bool kills = role.RemovalKillsPlayer;
                try
                {
                    role.RemovalKillsPlayer = false;
                    role.RemoveRole(player);
                }
                catch (Exception error)
                {
                    Log.Error($"SCP escape cleanup failed for {role.Name}: {error}");
                    return false;
                }
                finally { role.RemovalKillsPlayer = kills; }
                if (role.Check(player) || !IsConverted(player, original)) return false;
            }

            if (generation != currentGeneration || !IsConverted(player, original)) return false;
            if (MatchingRoles(player).Any())
            {
                Log.Error("SCP escape cleanup left a registered custom role active; success was not recorded.");
                return false;
            }
            state.RecordScpEscape();
            QueueTransferCheck();
            string message = networkTransfer ? plugin.Config.Scp079TransferredBroadcast ?? string.Empty :
                (plugin.Config.ScpEscapedBroadcast ?? string.Empty).Replace("{scp}", roleName);
            if (message.Length != 0) SafeMessage(() => Map.Broadcast((ushort)(networkTransfer ? 10 : 7), message));
            plugin.RaiseEscaped(new ScpEscapedEventArgs(attempt));
            return true;
        }

        private bool BeginAttempt(Player player)
        {
            float now = Time.realtimeSinceStartup;
            if (pending.Contains(player) || (nextAttempt.TryGetValue(player, out float next) && now < next)) return false;
            nextAttempt[player] = now + Config.ValidInterval(plugin.Config.EscapeRetrySeconds, 2f);
            return pending.Add(player);
        }

        private bool Present(Player player) => player != null && player.IsConnected && !departed.Contains(player);
        private bool SameLife(Player player, Role original) => !disposed && ActiveRound && Present(player) && player.IsAlive && ReferenceEquals(player.Role, original);
        private bool IsConverted(Player player, Role original) => !disposed && ActiveRound && Present(player) && player.IsAlive &&
            !ReferenceEquals(player.Role, original) && player.Role.Type == RoleTypeId.ChaosConscript;
        private Player[] LivingScps() => Player.List.Where(p => Present(p) && p.IsAlive && p.Role.Team == Team.SCPs).ToArray();
        private static IEnumerable<CustomRole> MatchingRoles(Player player) => CustomRole.Registered.Where(role => role.Check(player));

        private void Defer(Action callback)
        {
            int currentGeneration = generation;
            Timing.CallDelayed(0f, () =>
            {
                if (disposed || generation != currentGeneration || !ActiveRound) return;
                try { callback(); }
                catch (Exception error) { Log.Error($"SCP escape deferred operation failed: {error}"); }
            });
        }

        private static void PlayerBroadcast(Player player, string? message)
        {
            if (!string.IsNullOrEmpty(message)) SafeMessage(() => player.Broadcast(7, message!));
        }

        private static void SafeMessage(Action show)
        {
            try { show(); }
            catch (Exception error) { Log.Error($"SCP escape message failed: {error}"); }
        }
    }
}
