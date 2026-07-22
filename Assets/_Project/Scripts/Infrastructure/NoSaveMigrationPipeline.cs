using DemonLord.Application;
using DemonLord.Domain;

namespace DemonLord.Infrastructure
{
    public sealed class NoSaveMigrationPipeline : ISaveMigrationPipeline
    {
        public bool TryMigrate(
            SaveEnvelopeDto source,
            int targetSchemaVersion,
            out SaveEnvelopeDto migrated,
            out string errorCode)
        {
            migrated = null;
            errorCode = "unsupported_schema_version";
            return false;
        }
    }
}
