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
        private const float HintSeconds = 5f;
        private const ushort BroadcastSeconds = 7;
        private const ushort NetworkTransferBroadcastSeconds = 10;

        private readonly Plugin plugin;
        private readonly RoundState state = new RoundState();
        private readonly HashSet<Player> activeAttempts = new HashSet<Player>();
        private readonly HashSet<Player> departed = new HashSet<Player>();
        private readonly Dictionary<Player, float> nextAttempt = new Dictionary<Player, float>();
        private readonly Dictionary<Player, float> nextHint = new Dictionary<Player, float>();
        private bool disposed;
        private bool transferCheckQueued;
        private int generation;

        public EventHandlers(Plugin plugin) => this.plugin = plugin;

        private static bool ActiveRound => Round.IsStarted && !Round.IsEnded;
        private Config Settings => plugin.Config;

        public void ResetState()
        {
            ClearTracking();
            transferCheckQueued = false;
            state.Reset();
        }

        public void Dispose()
        {
            disposed = true;
            ClearTracking();
        }

        public void OnRespawnedTeam(RespawnedTeamEventArgs ev)
        {
            if (!ActiveRound || state.IsEscapeUnlocked) return;

            Player[] arrivals = ev.Players.ToArray();
            Defer(() =>
            {
                if (state.IsEscapeUnlocked || !arrivals.Any(IsLivingRescuer)) return;

                state.UnlockEscape();
                foreach (Player scp in LivingScps())
                    ShowBroadcast(scp, Settings.GatesOpenedBroadcast);
            });
        }

        public void OnEscaping(EscapingEventArgs ev)
        {
            Player player = ev.Player;
            if (!ActiveRound || !Present(player) || !player.IsAlive || player.Role.Team != Team.SCPs ||
                player.Role.Type == RoleTypeId.Scp079 || !MayEscape(player)) return;

            ev.IsAllowed = false;

            if (!state.IsEscapeUnlocked)
            {
                ShowLockedHint(player);
                return;
            }

            if (!BeginAttempt(player)) return;

            Role original = player.Role;
            Defer(() =>
            {
                try
                {
                    if (SameLife(player, original) && state.IsEscapeUnlocked &&
                        Escape.CanEscape(player.ReferenceHub, out _, out _))
                        TryConvert(player, original, false);
                }
                finally
                {
                    activeAttempts.Remove(player);
                }
            });
        }

        public void OnEscaped(EscapedEventArgs ev)
        {
            Player player = ev.Player;
            if (!ActiveRound || !Present(player) || ev.EscapeScenario == EscapeScenario.None) return;
            if (ev.OldRole.Type is not (RoleTypeId.ClassD or RoleTypeId.Scientist)) return;
            if (ReferenceEquals(ev.OldRole, player.Role) ||
                (SpawnReason)player.Role.SpawnReason != SpawnReason.Escaped ||
                !IsRescueTeam(player.Role.Team)) return;

            state.RecordHumanEscape();
        }

        public void OnDied(DiedEventArgs ev) => QueueTransferCheck();

        public void OnSpawned(SpawnedEventArgs ev)
        {
            QueueTransferCheck();

            Player player = ev.Player;
            Role role = player.Role;
            if (!state.IsEscapeUnlocked || role.Team != Team.SCPs) return;

            Defer(() =>
            {
                if (SameLife(player, role)) ShowBroadcast(player, Settings.GatesOpenedBroadcast);
            });
        }

        public void OnLeft(LeftEventArgs ev)
        {
            departed.Add(ev.Player);
            activeAttempts.Remove(ev.Player);
            nextAttempt.Remove(ev.Player);
            nextHint.Remove(ev.Player);
            QueueTransferCheck();
        }

        public void OnEndingRound(EndingRoundEventArgs ev)
        {
            if (!ev.IsAllowed || !state.HasScpEscaped) return;

            ev.LeadingTeam = state.HasHumanEscaped ? LeadingTeam.Draw : LeadingTeam.Anomalies;
        }

        public void QueueTransferCheck()
        {
            if (!ActiveRound || !state.HasScpEscaped || transferCheckQueued || disposed) return;

            transferCheckQueued = true;
            Defer(TransferLastComputer);
        }

        private void TransferLastComputer()
        {
            transferCheckQueued = false;

            Player[] scps = LivingScps();
            if (scps.Length != 1) return;

            Player computer = scps[0];
            if (computer.Role.Type != RoleTypeId.Scp079 || !BeginAttempt(computer)) return;

            try
            {
                TryConvert(computer, computer.Role, true);
            }
            finally
            {
                activeAttempts.Remove(computer);
            }
        }

        private void TryConvert(Player player, Role original, bool networkTransfer)
        {
            if (!SameLife(player, original) || (!networkTransfer && !MayEscape(player))) return;

            int startGeneration = generation;
            string roleName = MatchingRoles(player).FirstOrDefault()?.Name ?? original.Name;
            var attempt = new ScpEscapingEventArgs(player, original.Type, roleName, networkTransfer);
            plugin.RaiseEscaping(attempt);

            if (!attempt.IsAllowed || generation != startGeneration || !SameLife(player, original)) return;
            if (networkTransfer && (!state.HasScpEscaped || LivingScps().Length != 1)) return;

            bool ConversionHolds() => generation == startGeneration && IsConverted(player, original);

            CustomRole[] customRoles = MatchingRoles(player).ToArray();
            if (!ChangeToChaos(player, customRoles) || !ConversionHolds()) return;
            if (!RemoveCustomRoles(player, customRoles, ConversionHolds)) return;

            state.RecordScpEscape();
            QueueTransferCheck();
            AnnounceEscape(roleName, networkTransfer);
            plugin.RaiseEscaped(new ScpEscapedEventArgs(attempt));
        }

        private static bool ChangeToChaos(Player player, CustomRole[] customRoles)
        {
            bool[] keepFlags = customRoles.Select(role => role.KeepRoleOnChangingRole).ToArray();
            try
            {
                foreach (CustomRole role in customRoles) role.KeepRoleOnChangingRole = true;
                player.Role.Set(RoleTypeId.ChaosConscript, SpawnReason.ForceClass, RoleSpawnFlags.All);
                return true;
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
        }

        private static bool RemoveCustomRoles(Player player, CustomRole[] customRoles, Func<bool> conversionHolds)
        {
            foreach (CustomRole role in customRoles)
            {
                if (!role.Check(player)) continue;

                bool killsPlayer = role.RemovalKillsPlayer;
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
                finally
                {
                    role.RemovalKillsPlayer = killsPlayer;
                }

                if (role.Check(player) || !conversionHolds()) return false;
            }

            if (!MatchingRoles(player).Any()) return true;

            Log.Error("SCP escape cleanup left a registered custom role active; success was not recorded.");
            return false;
        }

        private void AnnounceEscape(string roleName, bool networkTransfer)
        {
            string template = networkTransfer ? Settings.Scp079TransferredBroadcast : Settings.ScpEscapedBroadcast;
            if (string.IsNullOrEmpty(template)) return;

            string message = networkTransfer ? template : template.Replace("{scp}", roleName);
            ushort seconds = networkTransfer ? NetworkTransferBroadcastSeconds : BroadcastSeconds;
            SafeMessage(() => Map.Broadcast(seconds, message));
        }

        private void ShowLockedHint(Player player)
        {
            float now = Time.realtimeSinceStartup;
            if (nextHint.TryGetValue(player, out float next) && now < next) return;

            nextHint[player] = now + Settings.LockedHintInterval;
            string hint = Settings.EscapeLockedHint;
            if (!string.IsNullOrEmpty(hint)) SafeMessage(() => player.ShowHint(hint, HintSeconds));
        }

        private bool BeginAttempt(Player player)
        {
            float now = Time.realtimeSinceStartup;
            if ((nextAttempt.TryGetValue(player, out float next) && now < next) || !activeAttempts.Add(player))
                return false;

            nextAttempt[player] = now + Settings.EscapeRetrySeconds;
            return true;
        }

        private bool MayEscape(Player player) =>
            Settings.AllowedScps.Contains(player.Role.Type) ||
            (Settings.AllowCustomRoles && MatchingRoles(player).Any());

        private void ClearTracking()
        {
            generation++;
            activeAttempts.Clear();
            departed.Clear();
            nextAttempt.Clear();
            nextHint.Clear();
        }

        private bool Present(Player player) => player != null && player.IsConnected && !departed.Contains(player);

        private bool IsLiving(Player player) => !disposed && ActiveRound && Present(player) && player.IsAlive;

        private bool IsLivingRescuer(Player player) => IsLiving(player) && IsRescueTeam(player.Role.Team);

        private bool SameLife(Player player, Role original) =>
            IsLiving(player) && ReferenceEquals(player.Role, original);

        private bool IsConverted(Player player, Role original) =>
            IsLiving(player) && !ReferenceEquals(player.Role, original) && player.Role.Type == RoleTypeId.ChaosConscript;

        private Player[] LivingScps() =>
            Player.List.Where(player => IsLiving(player) && player.Role.Team == Team.SCPs).ToArray();

        private static bool IsRescueTeam(Team team) => team is Team.FoundationForces or Team.ChaosInsurgency;

        private static IEnumerable<CustomRole> MatchingRoles(Player player) =>
            CustomRole.Registered.Where(role => role.Check(player));

        private void Defer(Action callback)
        {
            int scheduledGeneration = generation;
            Timing.CallDelayed(0f, () =>
            {
                if (disposed || generation != scheduledGeneration || !ActiveRound) return;

                try
                {
                    callback();
                }
                catch (Exception error)
                {
                    Log.Error($"SCP escape deferred operation failed: {error}");
                }
            });
        }

        private static void ShowBroadcast(Player player, string message)
        {
            if (!string.IsNullOrEmpty(message)) SafeMessage(() => player.Broadcast(BroadcastSeconds, message));
        }

        private static void SafeMessage(Action show)
        {
            try
            {
                show();
            }
            catch (Exception error)
            {
                Log.Error($"SCP escape message failed: {error}");
            }
        }
    }
}
