using System;
using System.Text;
using Newtonsoft.Json;

namespace FormFlow.State
{
    public class JsonStateSerializer : IStateSerializer
    {
        private static readonly Encoding _encoding = Encoding.UTF8;

        private readonly JsonSerializerSettings _serializerSettings;

        public JsonStateSerializer()
            : this(CreateDefaultSerializerSettings())
        {
        }

        public JsonStateSerializer(JsonSerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
        }

        public static JsonSerializerSettings CreateDefaultSerializerSettings() =>
            new JsonSerializerSettings()
            {
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.Auto
            };

        public object Deserialize(byte[] bytes) =>
            JsonConvert.DeserializeObject(_encoding.GetString(bytes), typeof(object), _serializerSettings);

        public byte[] Serialize(object state) =>
            _encoding.GetBytes(JsonConvert.SerializeObject(state, typeof(object), _serializerSettings));
    }
}
