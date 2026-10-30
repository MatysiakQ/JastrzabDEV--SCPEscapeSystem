using System;
using Exiled.API.Features;
using PlayerRoles;

namespace ScpEscapeSystem
{
    /// <summary>A custom SCP escape, distinct from a Class-D or Scientist escape.</summary>
    public sealed class ScpEscapingEventArgs : EventArgs
    {
        internal ScpEscapingEventArgs(Player player, RoleTypeId oldRole, string roleName, bool isNetworkTransfer)
        {
            Player = player;
            OldRole = oldRole;
            RoleName = roleName;
            IsNetworkTransfer = isNetworkTransfer;
        }

        public Player Player { get; }
        public RoleTypeId OldRole { get; }
        public string RoleName { get; }
        public bool IsNetworkTransfer { get; }
        public bool IsAllowed { get; set; } = true;
    }

    /// <summary>Raised after the Chaos class and registered CustomRole cleanup are confirmed.</summary>
    public sealed class ScpEscapedEventArgs : EventArgs
    {
        internal ScpEscapedEventArgs(ScpEscapingEventArgs attempt)
        {
            Player = attempt.Player;
            OldRole = attempt.OldRole;
            RoleName = attempt.RoleName;
            IsNetworkTransfer = attempt.IsNetworkTransfer;
        }

        public Player Player { get; }
        public RoleTypeId OldRole { get; }
        public string RoleName { get; }
        public bool IsNetworkTransfer { get; }
    }
}
