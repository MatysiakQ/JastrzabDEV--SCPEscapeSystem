using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using System.Linq;
using System.Collections.Generic;

namespace ScpEscapeSystem
{
    public class EventHandlers
    {
        private bool IsEscapeUnlocked = false;
        private bool HasScpEscaped = false;
        private int HumansEscaped = 0;

        public void OnRoundStarted()
        {
            IsEscapeUnlocked = false;
            HasScpEscaped = false;
            HumansEscaped = 0;
        }

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (!IsEscapeUnlocked)
            {
                IsEscapeUnlocked = true;
                Log.Debug("Pojawiło się wsparcie - brama ucieczki dla SCP została otwarta.");

                foreach (Player player in Player.List.Where(p => p.Role.Team == Team.SCPs))
                {
                    player.Broadcast(7, "<b><size=40><color=red>[SYSTEM]</color></size></b>\n<size=35>Zewnętrzne bramy placówki zostały otwarte.\n<b><color=green>Droga ucieczki jest wolna.</color></b></size>");
                }
            }
        }

        public void OnEscaping(EscapingEventArgs ev)
        {
            if (ev.Player.IsHuman && ev.IsAllowed)
            {
                HumansEscaped++;
                return;
            }

            if (ev.Player.Role.Team == Team.SCPs)
            {
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

                string scpName = isCustomRole ? playerCustomRole!.Name : ev.Player.Role.Name;

                ev.Player.Role.Set(RoleTypeId.ChaosConscript, SpawnReason.Escaped, RoleSpawnFlags.All);

                string broadcastMsg = Plugin.Instance.Config.ScpEscapedBroadcast.Replace("{scp}", scpName);
                Map.Broadcast(7, broadcastMsg);

                Check079Logic();
            }
        }

        public void OnDying(DyingEventArgs ev)
        {
            if (ev.Player != null && ev.Player.Role.Team == Team.SCPs)
            {
                Check079Logic(ev.Player);
            }
        }

        private void Check079Logic(Player? dyingScp = null)
        {
            var aliveScps = Player.List.Where(p => p.Role.Team == Team.SCPs && p != dyingScp).ToList();

            if (aliveScps.Count == 1 && aliveScps.First().Role.Type == RoleTypeId.Scp079)
            {
                Player scp079 = aliveScps.First();

                scp079.Role.Set(RoleTypeId.Spectator);
                Map.Broadcast(10, Plugin.Instance.Config.Scp079TransferredBroadcast);
            }
        }

        public void OnEndingRound(EndingRoundEventArgs ev)
        {
            if (ev.IsAllowed && HasScpEscaped)
            {
                if (HumansEscaped > 0)
                {
                    ev.LeadingTeam = LeadingTeam.Draw;
                }
                else
                {
                    ev.LeadingTeam = LeadingTeam.Anomalies;
                }
            }
        }
    }
}