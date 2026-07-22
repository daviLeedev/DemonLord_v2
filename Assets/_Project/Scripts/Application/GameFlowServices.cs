using System;
using System.Collections.Generic;
using DemonLord.Domain;

namespace DemonLord.Application
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public interface IPlayerSession
    {
        GameSave CurrentSave { get; }

        void SetCurrentSave(GameSave save);

        void Clear();
    }

    public interface IEntryPointResolver
    {
        bool TryResolve(GameEntryPoint entryPoint, out EntryDestination destination, out string errorCode);
    }

    public sealed class EntryDestination
    {
        public EntryDestination(string sceneKey, string spawnKey)
        {
            SceneKey = sceneKey;
            SpawnKey = spawnKey;
        }

        public string SceneKey { get; }

        public string SpawnKey { get; }
    }

    public sealed class ListSaveSlotsUseCase
    {
        private readonly ISaveRepository saveRepository;

        public ListSaveSlotsUseCase(ISaveRepository saveRepository)
        {
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
        }

        public IReadOnlyList<SaveSlotSummary> Execute()
        {
            return saveRepository.ListSlots();
        }
    }

    public sealed class CreateNewGameUseCase
    {
        private readonly ISaveRepository saveRepository;
        private readonly IClock clock;

        public CreateNewGameUseCase(ISaveRepository saveRepository, IClock clock)
        {
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public CreateNewGameResult Execute(SaveSlotId slotId, NewGameSettings settings, string buildVersion)
        {
            GameSave save = GameSave.CreateNew(slotId, settings, buildVersion, clock.UtcNow);
            SaveWriteResult writeResult = saveRepository.Save(save);
            return writeResult.IsSuccess
                ? CreateNewGameResult.Success(save)
                : CreateNewGameResult.Failure(writeResult.ErrorCode, writeResult.DiagnosticMessage);
        }
    }

    public sealed class LoadGameUseCase
    {
        private readonly ISaveRepository saveRepository;

        public LoadGameUseCase(ISaveRepository saveRepository)
        {
            this.saveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
        }

        public SaveReadResult Execute(SaveSlotId slotId)
        {
            return saveRepository.Load(slotId);
        }
    }

    public sealed class CreateNewGameResult
    {
        private CreateNewGameResult(GameSave save, string errorCode, string diagnosticMessage)
        {
            Save = save;
            ErrorCode = errorCode;
            DiagnosticMessage = diagnosticMessage;
        }

        public GameSave Save { get; }

        public string ErrorCode { get; }

        public string DiagnosticMessage { get; }

        public bool IsSuccess => Save != null;

        public static CreateNewGameResult Success(GameSave save)
        {
            return new CreateNewGameResult(save ?? throw new ArgumentNullException(nameof(save)), null, null);
        }

        public static CreateNewGameResult Failure(string errorCode, string diagnosticMessage)
        {
            return new CreateNewGameResult(null, errorCode, diagnosticMessage);
        }
    }
}
