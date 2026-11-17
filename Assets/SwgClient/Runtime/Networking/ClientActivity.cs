using Swg.Client.Core.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Clears server AFK state on player input, before UI and movement send their actions.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ClientActivity : MonoBehaviour
    {
        CurrentPlayer player;
        ClientCommands commands;
        bool clearRequested;

        public void Initialize(CurrentPlayer player, ClientCommands commands)
        {
            this.player = player;
            this.commands = commands;
        }

        void Update()
        {
            var mouse = Mouse.current;
            bool active = Keyboard.current?.anyKey.wasPressedThisFrame == true
                || mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0);
            UpdateActivity(active);
        }

        internal void UpdateActivity(bool active)
        {
            if (!player.IsAfk)
            {
                clearRequested = false;
                return;
            }
            // The server command toggles, so never send it twice while awaiting the flags delta.
            if (!active || clearRequested) return;
            clearRequested = true;
            commands.Enqueue("toggleAwayFromKeyBoard", completed: result =>
            {
                if (result.ErrorType != 0) clearRequested = false;
            });
        }
    }
}
