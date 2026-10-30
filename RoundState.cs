using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine.SceneManagement;

namespace ScpEscapeSystem
{
    // Only BCL values are stored in EXILED, so a newly loaded assembly can read them.
    internal sealed class RoundState
    {
        private const string SessionKey = "ScpEscapeSystem.RoundState.v1";
        private Dictionary<string, object> values = null!;

        public RoundState()
        {
            if (Round.IsStarted && !Round.IsEnded &&
                Server.SessionVariables.TryGetValue(SessionKey, out object? saved) &&
                saved is Dictionary<string, object> dictionary &&
                dictionary.TryGetValue("scene", out object? scene) &&
                scene is int handle && handle == SceneManager.GetActiveScene().handle)
                values = dictionary;
            else
                Reset();
        }

        public bool IsEscapeUnlocked
        {
            get => ReadBool("unlocked");
            set => values["unlocked"] = value;
        }

        public bool HasScpEscaped => ReadBool("scp");
        public bool HasHumanEscaped => ReadBool("human");
        public void RecordScpEscape() => values["scp"] = true;
        public void RecordHumanEscape() => values["human"] = true;

        public void Reset()
        {
            values = new Dictionary<string, object>
            {
                ["scene"] = SceneManager.GetActiveScene().handle,
                ["unlocked"] = false,
                ["scp"] = false,
                ["human"] = false,
            };
            Server.SessionVariables[SessionKey] = values;
        }

        private bool ReadBool(string key) => values.TryGetValue(key, out object? value) && value is bool flag && flag;
    }
}
