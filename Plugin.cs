using Exiled.API.Features;
using System;

namespace ScpEscapeSystem
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "ScpEscapeSystem";
        public override string Author => "Matysiak";
        public override Version Version => new Version(1, 3, 0);

        public static Plugin Instance { get; private set; } = null!;
        private EventHandlers _handlers = null!;

        public override void OnEnabled()
        {
            Instance = this;
            _handlers = new EventHandlers();

            Exiled.Events.Handlers.Player.Escaping += _handlers.OnEscaping;
            Exiled.Events.Handlers.Player.Dying += _handlers.OnDying;
            Exiled.Events.Handlers.Server.RespawningTeam += _handlers.OnRespawningTeam;
            Exiled.Events.Handlers.Server.EndingRound += _handlers.OnEndingRound;
            Exiled.Events.Handlers.Server.RoundStarted += _handlers.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += _handlers.OnRestartingRound;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Escaping -= _handlers.OnEscaping;
            Exiled.Events.Handlers.Player.Dying -= _handlers.OnDying;
            Exiled.Events.Handlers.Server.RespawningTeam -= _handlers.OnRespawningTeam;
            Exiled.Events.Handlers.Server.EndingRound -= _handlers.OnEndingRound;
            Exiled.Events.Handlers.Server.RoundStarted -= _handlers.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= _handlers.OnRestartingRound;

            _handlers = null!;
            Instance = null!;

            base.OnDisabled();
        }
    }
}