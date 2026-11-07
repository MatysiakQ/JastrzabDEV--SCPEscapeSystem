using System.Collections.Generic;
using Exiled.API.Features;
using UnityEngine.SceneManagement;

namespace ScpEscapeSystem
{
    internal sealed class RoundState
    {
        private const string SessionKey = "ScpEscapeSystem.RoundState.v1";
        private const string SceneKey = "scene";
        private const string UnlockedKey = "unlocked";
        private const string ScpEscapedKey = "scp";
        private const string HumanEscapedKey = "human";

        private Dictionary<string, object> values;

        public RoundState() => values = Restore() ?? Create();

        public bool IsEscapeUnlocked => ReadFlag(UnlockedKey);
        public bool HasScpEscaped => ReadFlag(ScpEscapedKey);
        public bool HasHumanEscaped => ReadFlag(HumanEscapedKey);

        public void UnlockEscape() => values[UnlockedKey] = true;
        public void RecordScpEscape() => values[ScpEscapedKey] = true;
        public void RecordHumanEscape() => values[HumanEscapedKey] = true;
        public void Reset() => values = Create();

        private static Dictionary<string, object>? Restore()
        {
            if (!Round.IsStarted || Round.IsEnded ||
                !Server.SessionVariables.TryGetValue(SessionKey, out object? saved) ||
                saved is not Dictionary<string, object> dictionary ||
                !dictionary.TryGetValue(SceneKey, out object? scene) ||
                scene is not int handle || handle != SceneManager.GetActiveScene().handle)
                return null;

            return dictionary;
        }

        private static Dictionary<string, object> Create()
        {
            var created = new Dictionary<string, object>
            {
                [SceneKey] = SceneManager.GetActiveScene().handle,
                [UnlockedKey] = false,
                [ScpEscapedKey] = false,
                [HumanEscapedKey] = false,
            };
            Server.SessionVariables[SessionKey] = created;
            return created;
        }

        private bool ReadFlag(string key) => values.TryGetValue(key, out object? value) && value is true;
    }
}
