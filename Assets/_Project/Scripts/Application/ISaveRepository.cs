using System.Collections.Generic;
using DemonLord.Domain;

namespace DemonLord.Application
{
    public interface ISaveRepository
    {
        IReadOnlyList<SaveSlotSummary> ListSlots();

        SaveReadResult Load(SaveSlotId slotId);

        SaveWriteResult Save(GameSave save);

        SaveWriteResult Delete(SaveSlotId slotId);
    }
}
