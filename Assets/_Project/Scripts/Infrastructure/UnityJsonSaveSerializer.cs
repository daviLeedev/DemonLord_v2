using System;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Infrastructure
{
    public sealed class UnityJsonSaveSerializer : ISaveJsonSerializer
    {
        public string SerializeEnvelope(SaveEnvelopeDto envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            return JsonUtility.ToJson(envelope, false);
        }

        public bool TryDeserializeEnvelope(string json, out SaveEnvelopeDto envelope, out string diagnosticMessage)
        {
            try
            {
                envelope = Deserialize<SaveEnvelopeDto>(json);
                diagnosticMessage = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                envelope = null;
                diagnosticMessage = exception.Message;
                return false;
            }
        }

        public string SerializePayload(GameSavePayloadDto payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return JsonUtility.ToJson(payload, false);
        }

        public bool TryDeserializePayload(string json, out GameSavePayloadDto payload, out string diagnosticMessage)
        {
            try
            {
                payload = Deserialize<GameSavePayloadDto>(json);
                diagnosticMessage = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                payload = null;
                diagnosticMessage = exception.Message;
                return false;
            }
        }

        private static T Deserialize<T>(string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save JSON must not be empty.", nameof(json));
            }

            T value = JsonUtility.FromJson<T>(json);
            if (value == null)
            {
                throw new ArgumentException("Save JSON did not produce data.", nameof(json));
            }

            return value;
        }
    }
}
