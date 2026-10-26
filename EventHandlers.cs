using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using System.Linq;

namespace ScpEscapeSystem
{
    public class EventHandlers
    {
        private bool IsEscapeUnlocked = false;
        private bool HasScpEscaped = false;
        private int HumansEscaped = 0;

        private void ResetState()
        {
            IsEscapeUnlocked = false;
            HasScpEscaped = false;
            HumansEscaped = 0;
        }

        public void OnRoundStarted() => ResetState();

        public void OnRestartingRound() => ResetState();

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (!ev.IsAllowed || IsEscapeUnlocked) return;

            IsEscapeUnlocked = true;
            Log.Debug("Support has arrived - the SCP escape gate has been opened.");

            foreach (Player player in Player.List.Where(p => p.Role.Team == Team.SCPs))
            {
                player.Broadcast(7, Plugin.Instance.Config.GatesOpenedBroadcast);
            }
        }

        public void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.IsHuman && ev.IsAllowed)
            {
                HumansEscaped++;
                return;
            }

            if (ev.Player.Role.Team != Team.SCPs) return;

            CustomRole? playerCustomRole = null;
            foreach (var role in CustomRole.Registered)
            {
                if (role.Check(ev.Player))
                {
                    playerCustomRole = role;
                    break;
                }
            }

            bool isAllowedScp = Plugin.Instance.Config.AllowedScps.Contains(ev.Player.Role.Type);
            bool isCustomRole = playerCustomRole != null;

            if (!isAllowedScp && !isCustomRole) return;

            ev.IsAllowed = false;

            if (!IsEscapeUnlocked)
            {
                ev.Player.ShowHint(Plugin.Instance.Config.EscapeLockedHint, 5f);
                return;
            }

            HasScpEscaped = true;

            Player escapedScp = ev.Player;
            string scpName = isCustomRole ? playerCustomRole!.Name : escapedScp.Role.Name;

            // Uciekający SCP zawsze zostaje Chaosem.
            escapedScp.Role.Set(RoleTypeId.ChaosConscript, SpawnReason.Escaped, RoleSpawnFlags.All);

            string broadcastMsg = Plugin.Instance.Config.ScpEscapedBroadcast.Replace("{scp}", scpName);
            Map.Broadcast(7, broadcastMsg);

            // Sprawdzamy, czy 079 został ostatnim SCP w placówce.
            CheckScp079Transfer(escapedScp);
        }

        public void OnDying(DyingEventArgs ev)
        {
            // Transfer tylko jeśli ktoś już uciekł, żeby nie wrócił bug z fałszywym broadcastem.
            if (!ev.IsAllowed || !HasScpEscaped) return;
            if (ev.Player == null || ev.Player.Role.Team != Team.SCPs) return;

            CheckScp079Transfer(ev.Player);
        }

        /// <summary>
        /// leavingScp = SCP, który właśnie opuszcza placówkę (przez ucieczkę lub śmierć).
        /// 079 uciekает, gdy po jego odejściu jest ostatnim żywym SCP.
        /// </summary>
        private void CheckScp079Transfer(Player leavingScp)
        {
            var aliveScps = Player.List
                .Where(p => p != leavingScp && p.IsAlive && p.Role.Team == Team.SCPs)
                .ToList();

            if (aliveScps.Count != 1 || aliveScps[0].Role.Type != RoleTypeId.Scp079)
                return;

            // 079 również zostaje Chaosem, tak jak pozostałe SCP.
            aliveScps[0].Role.Set(RoleTypeId.ChaosConscript, SpawnReason.Escaped, RoleSpawnFlags.All);
            Map.Broadcast(10, Plugin.Instance.Config.Scp079TransferredBroadcast);
        }

        public void OnEndingRound(EndingRoundEventArgs ev)
        {
            if (ev.IsAllowed && HasScpEscaped)
            {
                ev.LeadingTeam = HumansEscaped > 0 ? LeadingTeam.Draw : LeadingTeam.Anomalies;
            }
        }
    }
}