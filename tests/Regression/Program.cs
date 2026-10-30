using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.CustomRoles.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using Plugin = ScpEscapeSystem.Plugin;
using PE = Exiled.Events.Handlers.Player;
using SE = Exiled.Events.Handlers.Server;

class RegressionProgram
{
    static Plugin? plugin;
    static int passed, failed;
    static Plugin Fresh()
    {
        plugin?.OnDisabled();
        Player.List.Clear(); Map.Broadcasts.Clear(); Map.FailBroadcast = false;
        CustomRole.Registered.Clear(); Server.SessionVariables.Clear(); Log.Errors.Clear();
        AuditNative.EscapedClassD = 0; Timing.Queue.Clear(); UnityEngine.Time.realtimeSinceStartup = 0;
        UnityEngine.SceneManagement.SceneManager.Handle++;
        Round.IsStarted = true; Round.IsEnded = false;
        plugin = new Plugin(); plugin.OnEnabled(); SE.RaiseStart(); return plugin;
    }
    static void Assert(bool condition, string message = "Unexpected result")
    {
        if (!condition) throw new Exception(message);
    }
    static void Run(string name, Action body)
    {
        try { Fresh(); body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
    }
    static void Tick() => Timing.Drain();
    static void Unlock()
    {
        var ev = new RespawnedTeamEventArgs();
        ev.Players.Add(new Player(RoleTypeId.ChaosConscript));
        SE.RaiseRespawned(ev); Tick();
    }
    static EscapingEventArgs EscapePlayer(Player p, bool allowed = false)
    {
        var old = p.Role;
        var ev = new EscapingEventArgs { Player = p, IsAllowed = allowed }; PE.RaiseEscape(ev);
        if (ev.IsAllowed)
        {
            p.Role.Set(old.Type == RoleTypeId.Scientist ? RoleTypeId.NtfPrivate : RoleTypeId.ChaosConscript, SpawnReason.Escaped, RoleSpawnFlags.All);
            PE.RaiseEscaped(new EscapedEventArgs { Player = p, OldRole = old });
        }
        return ev;
    }
    static void Die(Player p)
    {
        var ev = new DyingEventArgs { Player = p }; PE.RaiseDying(ev);
        if (ev.IsAllowed)
        {
            p.Role.Set(RoleTypeId.Spectator, SpawnReason.ForceClass, RoleSpawnFlags.All);
            PE.RaiseDied(new DiedEventArgs { Player = p });
        }
    }
    static LeadingTeam Ending()
    {
        var ev = new EndingRoundEventArgs(); SE.RaiseEnding(ev); return ev.LeadingTeam;
    }
    static Player Scp(RoleTypeId type = RoleTypeId.Scp173) => new Player(type);
    static void PriorEscape()
    {
        Unlock(); var p = Scp(); EscapePlayer(p); Tick(); Map.Broadcasts.Clear();
    }
    static void Main()
    {
        Run("locked gate and hint throttling", () => {
            var p = Scp(); for (int i = 0; i < 100; i++) EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173 && p.Hints.Count == 1);
            UnityEngine.Time.realtimeSinceStartup = 4; EscapePlayer(p);
            Assert(p.Hints.Count == 2);
        });
        Run("unlocked SCP converts once after event dispatch", () => {
            Unlock(); var p = Scp(); int successes = 0;
            plugin!.ScpEscaped += (_, _) => successes++;
            EscapePlayer(p); Assert(p.Role.Type == RoleTypeId.Scp173);
            for (int i = 0; i < 30; i++) EscapePlayer(p);
            Tick(); Assert(p.Role.Type == RoleTypeId.ChaosConscript && successes == 1 && Ending() == LeadingTeam.Anomalies);
        });
        Run("cancelled ChangingRole does not award or broadcast success", () => {
            Unlock(); var p = Scp(); p.AcceptRoleChange = _ => false; EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173 && Map.Broadcasts.Count == 0 && Ending() == LeadingTeam.FacilityForces);
        });
        Run("failed class changes have a retry cooldown", () => {
            Unlock(); var p = Scp(); int calls = 0;
            p.AcceptRoleChange = _ => { calls++; return false; };
            EscapePlayer(p); Tick(); EscapePlayer(p); Tick(); Assert(calls == 1);
            UnityEngine.Time.realtimeSinceStartup = 3; EscapePlayer(p); Tick(); Assert(calls == 2);
        });
        Run("SCP escape has a cancellable public policy event", () => {
            Unlock(); var p = Scp(); plugin!.ScpEscaping += (_, ev) => ev.IsAllowed = false;
            EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173 && Map.Broadcasts.Count == 0);
        });
        Run("a throwing policy listener cancels the attempt", () => {
            Unlock(); var p = Scp(); plugin!.ScpEscaping += (_, _) => throw new Exception("Policy failure");
            EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173 && Log.Errors.Count == 1);
        });
        Run("a later listener cannot undo a policy cancellation", () => {
            Unlock(); var p = Scp();
            plugin!.ScpEscaping += (_, ev) => ev.IsAllowed = false;
            plugin.ScpEscaping += (_, ev) => ev.IsAllowed = true;
            EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("custom completion event does not fake a human escape", () => {
            Unlock(); var p = Scp(); int custom = 0, native = 0;
            plugin!.ScpEscaped += (_, ev) => { Assert(ev.OldRole == RoleTypeId.Scp173 && !ev.IsNetworkTransfer); custom++; };
            Action<EscapedEventArgs> listener = _ => native++; PE.Escaped += listener;
            try { EscapePlayer(p); Tick(); } finally { PE.Escaped -= listener; }
            Assert(custom == 1 && native == 0 && AuditNative.EscapedClassD == 0);
        });
        Run("cancelled respawn attempt does not unlock escape", () => {
            Action<RespawningTeamEventArgs> cancel = ev => ev.IsAllowed = false;
            SE.RespawningTeam += cancel;
            try { SE.RaiseRespawn(new RespawningTeamEventArgs()); } finally { SE.RespawningTeam -= cancel; }
            Tick(); var p = Scp(); EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("empty completed wave does not unlock escape", () => {
            SE.RaiseRespawned(new RespawnedTeamEventArgs()); Tick(); var p = Scp(); EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("spawn candidates still spectating do not unlock escape", () => {
            var ev = new RespawnedTeamEventArgs(); ev.Players.Add(new Player(RoleTypeId.Spectator));
            SE.RaiseRespawned(ev); Tick(); var p = Scp(); EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("079 does not transfer without a prior SCP escape", () => {
            var p = Scp(); var computer = Scp(RoleTypeId.Scp079); Die(p); Tick();
            Assert(computer.Role.Type == RoleTypeId.Scp079 && Map.Broadcasts.Count == 0);
        });
        Run("079 transfers after last physical escape without polluting Class-D counters", () => {
            Unlock(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079); EscapePlayer(p); Tick();
            Assert(computer.Role.Type == RoleTypeId.ChaosConscript && AuditNative.EscapedClassD == 0 && Map.Broadcasts.Count == 2);
        });
        Run("079 transfers after a confirmed death", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            Die(p); Tick(); Assert(computer.Role.Type == RoleTypeId.ChaosConscript);
        });
        Run("cancelled death leaves 079 in the facility", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            Action<DyingEventArgs> cancel = ev => ev.IsAllowed = false; PE.Dying += cancel;
            try { Die(p); } finally { PE.Dying -= cancel; }
            Tick(); Assert(p.Role.Type == RoleTypeId.Scp173 && computer.Role.Type == RoleTypeId.Scp079 && Map.Broadcasts.Count == 0);
        });
        Run("079 transfer rejected by ChangingRole has no success broadcast", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            computer.AcceptRoleChange = _ => false; Die(p); Tick();
            Assert(computer.Role.Type == RoleTypeId.Scp079 && Map.Broadcasts.Count == 0);
        });
        Run("079 transfer policy is independently cancellable", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            plugin!.ScpEscaping += (_, ev) => { if (ev.IsNetworkTransfer) ev.IsAllowed = false; };
            Die(p); Tick(); Assert(computer.Role.Type == RoleTypeId.Scp079 && Map.Broadcasts.Count == 0);
        });
        Run("last SCP disconnect triggers 079 transfer even before list removal", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            PE.RaiseLeft(new LeftEventArgs { Player = p }); Tick();
            Assert(computer.Role.Type == RoleTypeId.ChaosConscript);
        });
        Run("administrative team change triggers 079 transfer", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            p.Role.Set(RoleTypeId.ChaosConscript, SpawnReason.ForceClass, RoleSpawnFlags.All); Tick();
            Assert(computer.Role.Type == RoleTypeId.ChaosConscript);
        });
        Run("new SCP arriving during transfer policy prevents early transfer", () => {
            PriorEscape(); var p = Scp(); var computer = Scp(RoleTypeId.Scp079);
            plugin!.ScpEscaping += (_, ev) => { if (ev.IsNetworkTransfer) Scp(); };
            Die(p); Tick(); Assert(computer.Role.Type == RoleTypeId.Scp079);
        });
        Run("cancelled human escape does not turn an SCP victory into a draw", () => {
            PriorEscape(); Action<EscapingEventArgs> cancel = ev => ev.IsAllowed = false; PE.Escaping += cancel;
            try { EscapePlayer(new Player(RoleTypeId.ClassD), true); } finally { PE.Escaping -= cancel; }
            Tick(); Assert(Ending() == LeadingTeam.Anomalies);
        });
        Run("human conversion denied even if native Escaped fires is not counted", () => {
            PriorEscape(); var p = new Player(RoleTypeId.ClassD) { AcceptRoleChange = _ => false };
            EscapePlayer(p, true); Tick(); Assert(Ending() == LeadingTeam.Anomalies);
        });
        Run("confirmed Class-D escape yields a draw", () => {
            PriorEscape(); EscapePlayer(new Player(RoleTypeId.ClassD), true); Tick(); Assert(Ending() == LeadingTeam.Draw);
        });
        Run("confirmed scientist escape yields a draw", () => {
            PriorEscape(); EscapePlayer(new Player(RoleTypeId.Scientist), true); Tick(); Assert(Ending() == LeadingTeam.Draw);
        });
        Run("reload preserves unlocked gate and prior SCP result", () => {
            PriorEscape(); plugin!.OnDisabled(); plugin = new Plugin(); plugin.OnEnabled();
            Assert(Ending() == LeadingTeam.Anomalies);
            var p = Scp(); EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.ChaosConscript);
        });
        Run("reload preserves prior human escape", () => {
            PriorEscape(); EscapePlayer(new Player(RoleTypeId.ClassD), true);
            plugin!.OnDisabled(); plugin = new Plugin(); plugin.OnEnabled(); Assert(Ending() == LeadingTeam.Draw);
        });
        Run("different scene cannot inherit a disabled plugin's old round score", () => {
            PriorEscape(); plugin!.OnDisabled(); UnityEngine.SceneManagement.SceneManager.Handle++;
            plugin = new Plugin(); plugin.OnEnabled(); Assert(Ending() == LeadingTeam.FacilityForces);
            var p = Scp(); EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("disabling cancels queued conversions", () => {
            Unlock(); var p = Scp(); EscapePlayer(p); plugin!.OnDisabled(); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173 && Map.Broadcasts.Count == 0);
        });
        Run("restart invalidates queued work and old results", () => {
            Unlock(); var p = Scp(); EscapePlayer(p); SE.RaiseRestart(); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173 && Ending() == LeadingTeam.FacilityForces);
        });
        Run("ended rounds do not convert or announce escapes", () => {
            Unlock(); var p = Scp(); Round.IsEnded = true; EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173 && Map.Broadcasts.Count == 0);
        });
        Run("leaving the zone while queued prevents conversion", () => {
            Unlock(); var p = Scp(); EscapePlayer(p); p.InEscapeZone = false; Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("a different life cannot inherit a queued escape", () => {
            Unlock(); var p = Scp(); EscapePlayer(p);
            p.Role.Set(RoleTypeId.Scp173, SpawnReason.ForceClass, RoleSpawnFlags.All); Tick();
            Assert(p.Role.Type == RoleTypeId.Scp173);
        });
        Run("null message cannot block the 079 transfer", () => {
            Unlock(); plugin!.Config.ScpEscapedBroadcast = null!;
            var p = Scp(); var computer = Scp(RoleTypeId.Scp079); EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.ChaosConscript && computer.Role.Type == RoleTypeId.ChaosConscript && Log.Errors.Count == 0);
        });
        Run("broadcast errors do not block state or transfer", () => {
            Unlock(); Map.FailBroadcast = true; var p = Scp(); var computer = Scp(RoleTypeId.Scp079); EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.ChaosConscript && computer.Role.Type == RoleTypeId.ChaosConscript && Ending() == LeadingTeam.Anomalies);
        });
        Run("config reload normalizes nulls and non-finite intervals", () => {
            plugin!.Config.AllowedScps = null!; plugin.Config.ScpEscapedBroadcast = null!;
            plugin.Config.EscapeRetrySeconds = float.NaN; plugin.Config.LockedHintInterval = float.PositiveInfinity;
            SE.RaiseReloadedConfigs();
            Assert(plugin.Config.AllowedScps != null && plugin.Config.ScpEscapedBroadcast == "" && plugin.Config.EscapeRetrySeconds == 2f && plugin.Config.LockedHintInterval == 3f);
        });
        Run("retained custom role is removed without killing the Chaos player", () => {
            Unlock(); var p = Scp(RoleTypeId.Scp0492);
            var role = new CustomRole { KeepRoleOnChangingRole = true, RemovalKillsPlayer = true };
            role.TrackedPlayers.Add(p); CustomRole.Registered.Add(role); EscapePlayer(p); Tick();
            Assert(p.Role.Type == RoleTypeId.ChaosConscript && !role.Check(p) && role.Removed == 1 && role.KeepRoleOnChangingRole && role.RemovalKillsPlayer);
        });
        Run("rejected class change preserves custom role and provider flags", () => {
            Unlock(); var p = Scp(RoleTypeId.Scp0492); p.AcceptRoleChange = _ => false;
            var role = new CustomRole { KeepRoleOnChangingRole = false, RemovalKillsPlayer = true };
            role.TrackedPlayers.Add(p); CustomRole.Registered.Add(role); EscapePlayer(p); Tick();
            Assert(role.Check(p) && !role.KeepRoleOnChangingRole && role.RemovalKillsPlayer && Map.Broadcasts.Count == 0);
        });
        Run("custom role opt-out is enforced outside the base allow-list", () => {
            Unlock(); plugin!.Config.AllowedScps.Clear(); plugin.Config.AllowCustomRoles = false;
            var p = Scp(); var role = new CustomRole(); role.TrackedPlayers.Add(p); CustomRole.Registered.Add(role);
            EscapePlayer(p); Tick(); Assert(p.Role.Type == RoleTypeId.Scp173 && role.Check(p));
        });
        plugin?.OnDisabled();
        Console.WriteLine($"RESULT {passed} passed; {failed} failed");
        Environment.ExitCode = failed == 0 ? 0 : 1;
    }
}
