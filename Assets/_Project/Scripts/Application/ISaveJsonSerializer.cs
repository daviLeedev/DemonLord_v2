using DemonLord.Domain;

namespace DemonLord.Application
{
    public interface ISaveJsonSerializer
    {
        string SerializeEnvelope(SaveEnvelopeDto envelope);

        bool TryDeserializeEnvelope(string json, out SaveEnvelopeDto envelope, out string diagnosticMessage);

        string SerializePayload(GameSavePayloadDto payload);

        bool TryDeserializePayload(string json, out GameSavePayloadDto payload, out string diagnosticMessage);
    }
}
