namespace DZNativeCSharp
{
    public interface Listener
    {
        void Notify(DZPlayerEvent playerEvent, System.Object eventData);
    }
}
