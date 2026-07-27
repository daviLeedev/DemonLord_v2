using System;
using DemonLord.Domain;

namespace DemonLord.Application
{
    /// <summary>
    /// Persists a checkpoint atomically from the runtime player's active save.
    /// The session updates only after the repository confirms the write succeeded.
    /// </summary>
    public sealed class SaveGameProgressUseCase
    {
        private readonly ISaveRepository saveRepository;
        private readonly IClock clock;

        public SaveGameProgressUseCase(ISaveRepository saveRepository, IClock clock)
        {
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public SaveWriteResult Execute(IPlayerSession playerSession, string checkpointId)
        {
            return Execute(playerSession, checkpointId, playerSession?.CurrentSave?.Location);
        }

        public SaveWriteResult Execute(
            IPlayerSession playerSession,
            string checkpointId,
            ExplorationLocation location)
        {
            if (playerSession == null)
            {
                throw new ArgumentNullException(nameof(playerSession));
            }

            GameSave currentSave = playerSession.CurrentSave;
            if (currentSave == null)
            {
                return SaveWriteResult.Failure(
                    SaveWriteStatus.ValidationFailure,
                    "active_save_missing",
                    "Cannot save gameplay progress without an active save.");
            }

            if (!GameEntryPoint.TryCreate(currentSave.Progress.EntryId, checkpointId, out GameEntryPoint progress, out string errorCode))
            {
                return SaveWriteResult.Failure(SaveWriteStatus.ValidationFailure, errorCode, "The requested checkpoint is invalid.");
            }

            if (location == null)
            {
                return SaveWriteResult.Failure(
                    SaveWriteStatus.ValidationFailure,
                    "exploration_location_missing",
                    "Cannot save gameplay progress without a stable exploration location.");
            }

            GameSave updatedSave = currentSave.WithProgress(progress, location, clock.UtcNow);
            SaveWriteResult writeResult = saveRepository.Save(updatedSave);
            if (writeResult.IsSuccess)
            {
                playerSession.SetCurrentSave(updatedSave);
            }

            return writeResult;
        }
    }
}
