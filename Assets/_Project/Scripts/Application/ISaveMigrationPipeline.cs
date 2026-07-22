using DemonLord.Domain;

namespace DemonLord.Application
{
    public interface ISaveMigrationPipeline
    {
        bool TryMigrate(
            SaveEnvelopeDto source,
            int targetSchemaVersion,
            out SaveEnvelopeDto migrated,
            out string errorCode);
    }
}
