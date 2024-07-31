using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Application;

public class BackgroundQueue<T>
{
    private readonly BlockingCollection<T> _queue;

    public BackgroundQueue()
    {
        _queue = new BlockingCollection<T>();
    }

    public void Queue(T workItem)
    {
        _queue.Add(workItem);
    }
    
    public IEnumerable<T> GetEnumerable()
    {
        return _queue.GetConsumingEnumerable();
    }

    public T Dequeue(CancellationToken cancellationToken)
    {
        return _queue.Take();
    }
}