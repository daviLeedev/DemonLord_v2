using DemonLord.Application;
using UnityEngine;

namespace DemonLord.Presentation
{
    public sealed class GameShellSessionView : MonoBehaviour
    {
        private IPlayerSession playerSession;

        public void SetSession(IPlayerSession session)
        {
            playerSession = session;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(24, 24, 420, 148), "DemonLord GameShell");
            if (playerSession == null || playerSession.CurrentSave == null)
            {
                GUI.Label(new Rect(44, 64, 380, 28), "No active save session.");
                return;
            }

            GUI.Label(new Rect(44, 64, 380, 28), "Profile: " + playerSession.CurrentSave.Profile.ProfileName);
            GUI.Label(new Rect(44, 94, 380, 28), "Entry: " + playerSession.CurrentSave.Progress.EntryId);
            GUI.Label(new Rect(44, 124, 380, 28), "Checkpoint: " + playerSession.CurrentSave.Progress.CheckpointId);
        }
    }
}
