using System;
using Exiled.API.Features;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;

namespace ScpEscapeSystem
{
    public class Plugin : Plugin<Config>
    {
        private EventHandlers? handlers;

        public override string Name => "ScpEscapeSystem";
        public override string Prefix => "scp_escape_system";
        public override string Author => "Matysiak";
        public override Version Version => new Version(1, 4, 0);
        public override Version RequiredExiledVersion => new Version(9, 12, 6);

        public event EventHandler<ScpEscapingEventArgs>? ScpEscaping;
        public event EventHandler<ScpEscapedEventArgs>? ScpEscaped;

        public override void OnEnabled()
        {
            Config.Normalize();
            handlers = new EventHandlers(this);
            Subscribe(handlers);
            ServerEvents.ReloadedConfigs += OnReloadedConfigs;
            handlers.QueueTransferCheck();
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            ServerEvents.ReloadedConfigs -= OnReloadedConfigs;
            if (handlers != null)
            {
                Unsubscribe(handlers);
                handlers.Dispose();
                handlers = null;
            }

            ScpEscaping = null;
            ScpEscaped = null;
            base.OnDisabled();
        }

        internal void RaiseEscaping(ScpEscapingEventArgs ev)
        {
            if (ScpEscaping == null) return;

            bool allowed = true;
            foreach (EventHandler<ScpEscapingEventArgs> listener in ScpEscaping.GetInvocationList())
            {
                try
                {
                    listener(this, ev);
                }
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
                try
                {
                    listener(this, ev);
                }
                catch (Exception error)
                {
                    Log.Error($"ScpEscaped listener failed: {error}");
                }
            }
        }

        private void OnReloadedConfigs() => Config.Normalize();

        private static void Subscribe(EventHandlers handlers)
        {
            PlayerEvents.Escaping += handlers.OnEscaping;
            PlayerEvents.Escaped += handlers.OnEscaped;
            PlayerEvents.Died += handlers.OnDied;
            PlayerEvents.Spawned += handlers.OnSpawned;
            PlayerEvents.Left += handlers.OnLeft;
            ServerEvents.RespawnedTeam += handlers.OnRespawnedTeam;
            ServerEvents.EndingRound += handlers.OnEndingRound;
            ServerEvents.RoundStarted += handlers.ResetState;
            ServerEvents.RestartingRound += handlers.ResetState;
            ServerEvents.WaitingForPlayers += handlers.ResetState;
        }

        private static void Unsubscribe(EventHandlers handlers)
        {
            PlayerEvents.Escaping -= handlers.OnEscaping;
            PlayerEvents.Escaped -= handlers.OnEscaped;
            PlayerEvents.Died -= handlers.OnDied;
            PlayerEvents.Spawned -= handlers.OnSpawned;
            PlayerEvents.Left -= handlers.OnLeft;
            ServerEvents.RespawnedTeam -= handlers.OnRespawnedTeam;
            ServerEvents.EndingRound -= handlers.OnEndingRound;
            ServerEvents.RoundStarted -= handlers.ResetState;
            ServerEvents.RestartingRound -= handlers.ResetState;
            ServerEvents.WaitingForPlayers -= handlers.ResetState;
        }
    }
}
