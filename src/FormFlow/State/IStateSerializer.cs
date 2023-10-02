namespace FormFlow.State;

public interface IStateSerializer
{
    object Deserialize(byte[] bytes);
    byte[] Serialize(object state);
}
