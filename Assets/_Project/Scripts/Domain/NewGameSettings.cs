using System;

namespace DemonLord.Domain
{
    public sealed class NewGameSettings
    {
        private static readonly char[] InvalidProfileNameCharacters =
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*',
        };

        private NewGameSettings(string profileName, DifficultyId difficultyId, bool tutorialEnabled)
        {
            ProfileName = profileName;
            DifficultyId = difficultyId;
            TutorialEnabled = tutorialEnabled;
        }

        public string ProfileName { get; }

        public DifficultyId DifficultyId { get; }

        public bool TutorialEnabled { get; }

        public static bool TryCreate(
            string profileName,
            string difficultyId,
            bool tutorialEnabled,
            out NewGameSettings settings,
            out string errorCode)
        {
            string normalizedProfileName = profileName == null ? string.Empty : profileName.Trim();
            if (normalizedProfileName.Length < 1 || normalizedProfileName.Length > 16)
            {
                settings = null;
                errorCode = "invalid_profile_name_length";
                return false;
            }

            if (ContainsInvalidProfileNameCharacter(normalizedProfileName))
            {
                settings = null;
                errorCode = "invalid_profile_name_character";
                return false;
            }

            if (!DifficultyId.TryCreate(difficultyId, out DifficultyId parsedDifficultyId))
            {
                settings = null;
                errorCode = "invalid_difficulty_id";
                return false;
            }

            settings = new NewGameSettings(normalizedProfileName, parsedDifficultyId, tutorialEnabled);
            errorCode = null;
            return true;
        }

        private static bool ContainsInvalidProfileNameCharacter(string profileName)
        {
            foreach (char character in profileName)
            {
                if (char.IsControl(character) || Array.IndexOf(InvalidProfileNameCharacters, character) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
