using System;
using Exiled.API.Features;

namespace ScpEscapeSystem
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "ScpEscapeSystem";
        public override string Prefix => "scp_escape_system";
        public override string Author => "Matysiak";
        public override Version Version => new Version(1, 4, 0);
        public override Version RequiredExiledVersion => new Version(9, 12, 6);

        public static Plugin Instance { get; private set; } = null!;
        private EventHandlers? handlers;

        /// <summary>Cancellable attempt, after the vanilla escape event has finished dispatching.</summary>
        public event EventHandler<ScpEscapingEventArgs>? ScpEscaping;
        /// <summary>Confirmed Chaos conversion and completed registered CustomRole cleanup.</summary>
        public event EventHandler<ScpEscapedEventArgs>? ScpEscaped;

        public override void OnEnabled()
        {
            Instance = this;
            Config.Normalize();
            handlers = new EventHandlers(this);
            Exiled.Events.Handlers.Player.Escaping += handlers.OnEscaping;
            Exiled.Events.Handlers.Player.Escaped += handlers.OnEscaped;
            Exiled.Events.Handlers.Player.Died += handlers.OnDied;
            Exiled.Events.Handlers.Player.Spawned += handlers.OnSpawned;
            Exiled.Events.Handlers.Player.Left += handlers.OnLeft;
            Exiled.Events.Handlers.Server.RespawnedTeam += handlers.OnRespawnedTeam;
            Exiled.Events.Handlers.Server.EndingRound += handlers.OnEndingRound;
            Exiled.Events.Handlers.Server.RoundStarted += handlers.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += handlers.OnRestartingRound;
            Exiled.Events.Handlers.Server.WaitingForPlayers += handlers.OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.ReloadedConfigs += OnReloadedConfigs;
            handlers.Resume();
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            if (handlers != null)
            {
                Exiled.Events.Handlers.Player.Escaping -= handlers.OnEscaping;
                Exiled.Events.Handlers.Player.Escaped -= handlers.OnEscaped;
                Exiled.Events.Handlers.Player.Died -= handlers.OnDied;
                Exiled.Events.Handlers.Player.Spawned -= handlers.OnSpawned;
                Exiled.Events.Handlers.Player.Left -= handlers.OnLeft;
                Exiled.Events.Handlers.Server.RespawnedTeam -= handlers.OnRespawnedTeam;
                Exiled.Events.Handlers.Server.EndingRound -= handlers.OnEndingRound;
                Exiled.Events.Handlers.Server.RoundStarted -= handlers.OnRoundStarted;
                Exiled.Events.Handlers.Server.RestartingRound -= handlers.OnRestartingRound;
                Exiled.Events.Handlers.Server.WaitingForPlayers -= handlers.OnWaitingForPlayers;
                Exiled.Events.Handlers.Server.ReloadedConfigs -= OnReloadedConfigs;
                handlers.Dispose();
                handlers = null;
            }
            ScpEscaping = null;
            ScpEscaped = null;
            if (ReferenceEquals(Instance, this)) Instance = null!;
            base.OnDisabled();
        }

        private void OnReloadedConfigs() => Config.Normalize();

        internal void RaiseEscaping(ScpEscapingEventArgs ev)
        {
            if (ScpEscaping == null) return;
            bool allowed = true;
            foreach (EventHandler<ScpEscapingEventArgs> listener in ScpEscaping.GetInvocationList())
            {
                try { listener(this, ev); }
                catch (Exception error)
                {
                    ev.IsAllowed = false;
                    Log.Error($"ScpEscaping listener failed; attempt cancelled: {error}");
                }
                allowed &= ev.IsAllowed;
                ev.IsAllowed = allowed;
            }
        }

        internal void RaiseEscaped(ScpEscapedEventArgs ev)
        {
            if (ScpEscaped == null) return;
            foreach (EventHandler<ScpEscapedEventArgs> listener in ScpEscaped.GetInvocationList())
            {
                try { listener(this, ev); }
                catch (Exception error) { Log.Error($"ScpEscaped listener failed: {error}"); }
            }
        }
    }
}
