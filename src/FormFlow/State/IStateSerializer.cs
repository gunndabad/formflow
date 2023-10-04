using System;

namespace FormFlow.State;

public interface IStateSerializer
{
    object Deserialize(Type type, byte[] bytes);
    byte[] Serialize(Type type, object state);
}
