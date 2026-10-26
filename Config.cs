using Exiled.API.Interfaces;
using PlayerRoles;
using System.Collections.Generic;
using System.ComponentModel;

namespace ScpEscapeSystem
{
    public class Config : IConfig
    {
        [Description("Whether or not the plugin is enabled.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether or not debug logs are enabled.")]
        public bool Debug { get; set; } = false;

        [Description("Which base SCPs are allowed to escape? (Custom Roles are allowed by default).")]
        public List<RoleTypeId> AllowedScps { get; set; } = new List<RoleTypeId>
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp939,
            RoleTypeId.Scp3114
        };

        [Description("Hint displayed when an SCP enters the escape zone BEFORE MTF/Chaos arrives.")]
        public string EscapeLockedHint { get; set; } = "<b><color=orange>[SYSTEM]</color></b> Escape route is currently locked.\nAwait the opening of the exterior gates...";

        [Description("Broadcast shown to SCPs when the exterior gates open (after MTF/Chaos arrives).")]
        public string GatesOpenedBroadcast { get; set; } = "<b><size=40><color=red>[SYSTEM]</color></size></b>\n<size=35>The facility's exterior gates have been opened.\n<b><color=green>The escape route is clear.</color></b></size>";

        [Description("Global broadcast message when an SCP escapes. The {scp} variable will be replaced with the class/role name.")]
        public string ScpEscapedBroadcast { get; set; } = "<b><size=40><color=red>[WARNING]</color></size></b>\n<size=35><b><color=yellow>{scp}</color></b> has successfully escaped the facility!</size>";

        [Description("Broadcast message when SCP-079 successfully escapes into the network (when it is the last SCP left and at least one SCP has escaped).")]
        public string Scp079TransferredBroadcast { get; set; } = "<b><size=40><color=#00FFFF>[NETWORK TRANSFER]</color></size></b>\n<size=35><b>SCP-079</b> has successfully transferred its database outside the facility.</size>";
    }
}