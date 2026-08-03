namespace Infrastructure.RoutePlanning.Rgv;

internal sealed class ReservoirSampler<T>(int capacity, Random random)
{
    private readonly List<T> _items = new(capacity);

    private int _seen;

    public void Add(Func<T> createItem)
    {
        _seen++;

        if (_items.Count < capacity)
        {
            _items.Add(createItem());
            return;
        }

        int index = random.Next(_seen);

        if (index < capacity)
        {
            _items[index] = createItem();
        }
    }

    public List<T> ToList() => _items;
}
