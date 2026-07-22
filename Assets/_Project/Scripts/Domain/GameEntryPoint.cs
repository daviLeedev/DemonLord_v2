using System;

namespace DemonLord.Domain
{
    public sealed class GameEntryPoint
    {
        public const string PrologueStartId = "prologue_start";
        public const string DungeonHubId = "dungeon_hub";

        private GameEntryPoint(string entryId, string checkpointId)
        {
            EntryId = entryId;
            CheckpointId = checkpointId;
        }

        public string EntryId { get; }

        public string CheckpointId { get; }

        public static bool TryCreate(string entryId, string checkpointId, out GameEntryPoint entryPoint, out string errorCode)
        {
            if (!IsValidStableId(entryId))
            {
                entryPoint = null;
                errorCode = "invalid_entry_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(checkpointId) || checkpointId.Length > 64 || ContainsControlCharacter(checkpointId))
            {
                entryPoint = null;
                errorCode = "invalid_checkpoint_id";
                return false;
            }

            entryPoint = new GameEntryPoint(entryId, checkpointId);
            errorCode = null;
            return true;
        }

        public static bool IsKnownEntryId(string entryId)
        {
            return string.Equals(entryId, PrologueStartId, StringComparison.Ordinal)
                || string.Equals(entryId, DungeonHubId, StringComparison.Ordinal);
        }

        private static bool IsValidStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!((character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
