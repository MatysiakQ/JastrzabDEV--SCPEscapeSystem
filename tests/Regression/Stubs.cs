using System;
using System.Collections.Generic;
using PlayerRoles;

namespace PlayerRoles
{
    public enum Team { Dead, SCPs, Scientists, ClassD, FoundationForces, ChaosInsurgency }
    public enum RoleTypeId { Spectator, Scp049, Scp0492, Scp079, Scp096, Scp106, Scp173, Scp939, Scp3114, Scientist, ClassD, NtfPrivate, ChaosConscript }
    public enum RoleSpawnFlags { None, All }
}
namespace UnityEngine
{
    public struct Bounds { }
    public static class Time { public static float realtimeSinceStartup { get; set; } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int handle; }
    public static class SceneManager
    {
        public static int Handle = 1;
        public static Scene GetActiveScene() => new Scene { handle = Handle };
    }
}
namespace MEC
{
    public static class Timing
    {
        public static readonly Queue<Action> Queue = new();
        public static void CallDelayed(float delay, Action callback) => Queue.Enqueue(callback);
        public static void Drain()
        {
            int limit = 200;
            while (Queue.Count > 0 && --limit > 0) Queue.Dequeue()();
            if (limit == 0) throw new Exception("Unbounded deferred work");
        }
    }
}
public class ReferenceHub
{
    public Exiled.API.Features.Player Player = null!;
}
public static class Escape
{
    public static bool CanEscape(ReferenceHub hub, out object role, out UnityEngine.Bounds zone)
    {
        role = hub.Player.Role; zone = default; return hub.Player.InEscapeZone;
    }
}
namespace Exiled.API.Interfaces { public interface IConfig { bool IsEnabled { get; set; } bool Debug { get; set; } } }
namespace Exiled.API.Enums
{
    public enum SpawnReason { ForceClass, Escaped }
    public enum LeadingTeam { FacilityForces, ChaosInsurgency, Anomalies, Draw }
    public enum EscapeScenario { None, ClassD, Scientist, CustomEscape }
}
namespace Exiled.API.Features
{
    public class Plugin<T> where T : new()
    {
        public virtual string Name => GetType().Assembly.GetName().Name!;
        public virtual string Author => "";
        public virtual string Prefix => "";
        public virtual Version RequiredExiledVersion => new Version(9, 12, 6);
        public virtual Version Version => new Version(1, 0);
        public T Config { get; } = new T();
        public virtual void OnEnabled() { }
        public virtual void OnDisabled() { }
    }
    public class Player
    {
        public static List<Player> List { get; } = new();
        public Exiled.API.Features.Roles.Role Role { get; internal set; }
        public ReferenceHub ReferenceHub { get; }
        public bool IsConnected { get; set; } = true;
        public bool InEscapeZone { get; set; } = true;
        public bool IsAlive => Role.Type != RoleTypeId.Spectator;
        public bool IsHuman => Role.Team != Team.SCPs && Role.Team != Team.Dead;
        public List<string> Hints { get; } = new();
        public List<string> Broadcasts { get; } = new();
        public Func<RoleTypeId, bool>? AcceptRoleChange { get; set; }
        public Player(RoleTypeId role)
        {
            Role = new Exiled.API.Features.Roles.Role(this, role);
            ReferenceHub = new ReferenceHub { Player = this }; List.Add(this);
        }
        public void ShowHint(string text, float duration) => Hints.Add(text);
        public void Broadcast(ushort duration, string text) => Broadcasts.Add(text);
    }
    public static class Round
    {
        public static bool IsStarted = true;
        public static bool IsEnded;
    }
    public static class Server { public static Dictionary<string, object> SessionVariables { get; } = new(); }
    public static class Map
    {
        public static List<string> Broadcasts { get; } = new();
        public static bool FailBroadcast;
        public static void Broadcast(ushort seconds, string text)
        {
            if (FailBroadcast) throw new Exception("Message failure");
            Broadcasts.Add(text);
        }
    }
    public static class Log
    {
        public static List<string> Errors { get; } = new();
        public static void Debug(string message) { }
        public static void Error(string message) => Errors.Add(message);
    }
    public static class AuditNative { public static int EscapedClassD; }
}
namespace Exiled.API.Features.Roles
{
    using Exiled.API.Features;
    public class Role
    {
        private readonly Player owner;
        public RoleTypeId Type { get; private set; }
        public string Name => Type.ToString();
        public Exiled.API.Enums.SpawnReason SpawnReason { get; private set; }
        public static Team GetTeam(RoleTypeId r) => r switch
        {
            RoleTypeId.Spectator => Team.Dead,
            RoleTypeId.Scientist => Team.Scientists,
            RoleTypeId.ClassD => Team.ClassD,
            RoleTypeId.NtfPrivate => Team.FoundationForces,
            RoleTypeId.ChaosConscript => Team.ChaosInsurgency,
            _ => Team.SCPs,
        };
        public Team Team => GetTeam(Type);
        public Role(Player owner, RoleTypeId type) { this.owner = owner; Type = type; }
        public void Set(RoleTypeId role, Exiled.API.Enums.SpawnReason reason, RoleSpawnFlags flags)
        {
            if (owner.AcceptRoleChange != null && !owner.AcceptRoleChange(role)) return;
            foreach (var custom in Exiled.CustomRoles.API.Features.CustomRole.Registered)
                if (custom.Check(owner) && !custom.KeepRoleOnChangingRole) custom.RemoveRole(owner);
            // Model of the native RoundSummary.OnServerRoleSet rule inspected in Assembly-CSharp.
            if (reason == Exiled.API.Enums.SpawnReason.Escaped && GetTeam(role) == Team.ChaosInsurgency)
                AuditNative.EscapedClassD++;
            var old = this;
            owner.Role = new Role(owner, role) { SpawnReason = reason };
            Exiled.Events.Handlers.Player.RaiseSpawned(new Exiled.Events.EventArgs.Player.SpawnedEventArgs { Player = owner, OldRole = old });
        }
    }
}
namespace Exiled.CustomRoles.API.Features
{
    public class CustomRole
    {
        public static HashSet<CustomRole> Registered { get; } = new();
        public string Name { get; set; } = "Custom SCP";
        public HashSet<Exiled.API.Features.Player> TrackedPlayers { get; } = new();
        public bool Check(Exiled.API.Features.Player player) => TrackedPlayers.Contains(player);
        public bool KeepRoleOnChangingRole { get; set; }
        public bool RemovalKillsPlayer { get; set; } = true;
        public int Removed { get; private set; }
        public void RemoveRole(Exiled.API.Features.Player player)
        {
            if (!TrackedPlayers.Remove(player)) return;
            Removed++;
            if (RemovalKillsPlayer)
                player.Role.Set(RoleTypeId.Spectator, Exiled.API.Enums.SpawnReason.ForceClass, RoleSpawnFlags.All);
        }
    }
}
namespace Exiled.Events.EventArgs.Player
{
    public class EscapingEventArgs
    {
        public Exiled.API.Features.Player Player { get; set; } = null!;
        public bool IsAllowed { get; set; }
    }
    public class DyingEventArgs
    {
        public Exiled.API.Features.Player Player { get; set; } = null!;
        public bool IsAllowed { get; set; } = true;
    }
    public class DiedEventArgs { public Exiled.API.Features.Player Player { get; set; } = null!; }
    public class LeftEventArgs { public Exiled.API.Features.Player Player { get; set; } = null!; }
    public class SpawnedEventArgs
    {
        public Exiled.API.Features.Player Player { get; set; } = null!;
        public Exiled.API.Features.Roles.Role OldRole { get; set; } = null!;
    }
    public class EscapedEventArgs : SpawnedEventArgs
    {
        public Exiled.API.Enums.EscapeScenario EscapeScenario { get; set; } = Exiled.API.Enums.EscapeScenario.ClassD;
    }
}
namespace Exiled.Events.EventArgs.Server
{
    public class RespawningTeamEventArgs
    {
        public bool IsAllowed { get; set; } = true;
        public List<Exiled.API.Features.Player> Players { get; } = new();
        public int MaximumRespawnAmount { get; set; } = 10;
    }
    public class EndingRoundEventArgs
    {
        public bool IsAllowed { get; set; } = true;
        public Exiled.API.Enums.LeadingTeam LeadingTeam { get; set; } = Exiled.API.Enums.LeadingTeam.FacilityForces;
    }
    public class RespawnedTeamEventArgs
    {
        public List<Exiled.API.Features.Player> Players { get; } = new();
    }
}
namespace Exiled.Events.Handlers
{
    public static class Player
    {
        public static event Action<Exiled.Events.EventArgs.Player.EscapingEventArgs>? Escaping;
        public static event Action<Exiled.Events.EventArgs.Player.DyingEventArgs>? Dying;
        public static event Action<Exiled.Events.EventArgs.Player.DiedEventArgs>? Died;
        public static event Action<Exiled.Events.EventArgs.Player.LeftEventArgs>? Left;
        public static event Action<Exiled.Events.EventArgs.Player.SpawnedEventArgs>? Spawned;
        public static event Action<Exiled.Events.EventArgs.Player.EscapedEventArgs>? Escaped;
        public static void RaiseEscape(Exiled.Events.EventArgs.Player.EscapingEventArgs ev) => Escaping?.Invoke(ev);
        public static void RaiseDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev) => Dying?.Invoke(ev);
        public static void RaiseDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev) => Died?.Invoke(ev);
        public static void RaiseLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev) => Left?.Invoke(ev);
        public static void RaiseSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev) => Spawned?.Invoke(ev);
        public static void RaiseEscaped(Exiled.Events.EventArgs.Player.EscapedEventArgs ev) => Escaped?.Invoke(ev);
    }
    public static class Server
    {
        public static event Action<Exiled.Events.EventArgs.Server.RespawningTeamEventArgs>? RespawningTeam;
        public static event Action<Exiled.Events.EventArgs.Server.EndingRoundEventArgs>? EndingRound;
        public static event Action? RoundStarted;
        public static event Action? RestartingRound;
        public static event Action? WaitingForPlayers;
        public static event Action? ReloadedConfigs;
        public static event Action<Exiled.Events.EventArgs.Server.RespawnedTeamEventArgs>? RespawnedTeam;
        public static void RaiseRespawn(Exiled.Events.EventArgs.Server.RespawningTeamEventArgs ev) => RespawningTeam?.Invoke(ev);
        public static void RaiseEnding(Exiled.Events.EventArgs.Server.EndingRoundEventArgs ev) => EndingRound?.Invoke(ev);
        public static void RaiseStart() => RoundStarted?.Invoke();
        public static void RaiseRestart() => RestartingRound?.Invoke();
        public static void RaiseWaiting() => WaitingForPlayers?.Invoke();
        public static void RaiseReloadedConfigs() => ReloadedConfigs?.Invoke();
        public static void RaiseRespawned(Exiled.Events.EventArgs.Server.RespawnedTeamEventArgs ev) => RespawnedTeam?.Invoke(ev);
    }
}
