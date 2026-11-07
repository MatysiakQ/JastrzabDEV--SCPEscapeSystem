using System;
using Exiled.API.Features;
using PlayerRoles;

namespace ScpEscapeSystem
{
    public abstract class ScpEscapeEventArgs : EventArgs
    {
        internal ScpEscapeEventArgs(Player player, RoleTypeId oldRole, string roleName, bool isNetworkTransfer)
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
    }

    public sealed class ScpEscapingEventArgs : ScpEscapeEventArgs
    {
        internal ScpEscapingEventArgs(Player player, RoleTypeId oldRole, string roleName, bool isNetworkTransfer)
            : base(player, oldRole, roleName, isNetworkTransfer)
        {
        }

        public bool IsAllowed { get; set; } = true;
    }

    public sealed class ScpEscapedEventArgs : ScpEscapeEventArgs
    {
        internal ScpEscapedEventArgs(ScpEscapingEventArgs attempt)
            : base(attempt.Player, attempt.OldRole, attempt.RoleName, attempt.IsNetworkTransfer)
        {
        }
    }
}
