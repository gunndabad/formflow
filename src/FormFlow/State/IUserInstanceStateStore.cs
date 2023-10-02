namespace FormFlow.State
{
    public interface IUserInstanceStateStore
    {
        void DeleteState(string key);
        void SetState(string key, byte[] data);
        bool TryGetState(string key, out byte[] data);
    }
}
