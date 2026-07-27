using System;

namespace DemonLord.Domain
{
    /// <summary>
    /// Stable checkpoints for the World Adjustment Bureau laboratory slice.
    /// These values are persisted in existing saves, so never rename an existing one.
    /// </summary>
    public static class LabCheckpointId
    {
        public const string Start = "start";
        public const string ResearcherBriefed = "researcher_briefed";
        public const string TaxLedgerReviewed = "tax_ledger_reviewed";
        public const string ArchiveCatalogued = "archive_catalogued";

        public static bool IsKnown(string checkpointId)
        {
            return string.Equals(checkpointId, Start, StringComparison.Ordinal)
                || string.Equals(checkpointId, ResearcherBriefed, StringComparison.Ordinal)
                || string.Equals(checkpointId, TaxLedgerReviewed, StringComparison.Ordinal)
                || string.Equals(checkpointId, ArchiveCatalogued, StringComparison.Ordinal);
        }
    }
}
