namespace ArtOfSimRally.Testing
{
    internal sealed class CaptureBuffer<T> where T : struct
    {
        public readonly T[] Items;
        public int Count { get; private set; }
        public bool Truncated { get; private set; }
        public CaptureBuffer(int capacity) { Items = new T[capacity]; }
        public void Add(T item)
        {
            if (Count == Items.Length) { Truncated = true; return; }
            Items[Count++] = item;
        }
    }
}
