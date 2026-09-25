public interface IResettable
{
    object SaveState();
    void LoadState(object state);
}