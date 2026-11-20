using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CustomCollections.Collections;

public class LinkedListNode<T>(ushort owner, T value)
{
	public ushort Owner = owner;
	public T? Value = value;
	public LinkedListNode<T>? Next;
	public LinkedListNode<T>? Previous;
		
	public bool IsHead => Previous == null;
	public bool IsTail => Next == null;

	public bool IsSingleNode => IsHead && IsTail;
}

public class MyLinkedList<T>() : IList<T>
{
	public bool IsReadOnly => false;

	protected ushort OwnerId = (ushort)Random.Shared.Next(1, ushort.MaxValue + 1);
	protected ulong Version;
	protected LinkedListNode<T>? Head;
	protected LinkedListNode<T>? Tail;
	
	public int Count { get; protected set; }
	public bool IsEmpty => Head == null;
	
	public MyLinkedList(params IEnumerable<T> collection) : this()
	{
		ArgumentNullException.ThrowIfNull(collection);
		AddRange(collection);
	}
	
	protected static bool MoveNext(LinkedListNode<T> current, bool reverse, [NotNullWhen(true)] out LinkedListNode<T>? next)
	{
		next = null;
		var tmp = reverse ? current.Previous : current.Next;
		if (tmp == null) return false;
		next = tmp;
		return true;
	}

	protected bool Find(LinkedListNode<T>? start, T value, ref int index, [NotNullWhen(true)] out LinkedListNode<T>? node)
	{
		node = null;
		
		if (IsEmpty)
			return false;
		
		var curr = start ?? Head;
		
		if (start != null)
		{
			if (start.Owner != OwnerId)
				throw new InvalidOperationException("Attempt to start from a node that is not owned by this instance.");
			
			if (index == -1)
				throw new InvalidOperationException("Cannot start from node without index");
		}
		
		do
		{
			index++;
			if (!EqualityComparer<T>.Default.Equals(curr!.Value, value)) continue;
			node = curr;
			return true;
		} while (MoveNext(curr, false, out curr));
		
		return false;
	}
	
	protected bool FindFirst(T value, out int index, [NotNullWhen(true)] out LinkedListNode<T>? node)
	{
		index = -1;
		return Find(null, value, ref index, out node);
	}

	protected bool TryGetAtIndex(int index, [NotNullWhen(true)] out LinkedListNode<T>? node)
	{
		node = null;
		
		if (index < 0 || index >= Count)
			return false;
		
		var iterateReverse = index >= Count / 2.0;
		var curr = iterateReverse ? Tail : Head;
		var steps = iterateReverse ? Count - 1 - index : index;
		for(var i = 0; i < steps; i++)
			MoveNext(curr!, iterateReverse, out curr);
		
		node = curr!;
		return true;
	}
	
	protected void Remove(LinkedListNode<T> node)
	{
		if (node.Owner != OwnerId)
			throw new InvalidOperationException("Attempt to remove a node that is not owned by this instance.");
		
		if (node.IsHead)
			Head = node.Next;
		else
			node.Previous!.Next = node.Next;
		
		if (node.IsTail)
			Tail = node.Previous;
		else
			node.Next!.Previous = node.Previous;

		node.Owner = 0;
		
		Version++;
		Count--;
	}

	public void Add(T item)
	{
		if (Tail == null)
			Head = Tail = new LinkedListNode<T>(OwnerId, item);
		else
			Tail = Tail.Next = new LinkedListNode<T>(OwnerId, item)
			{
				Previous = Tail
			};
		Version++;
		Count++;
	}

	public void AddRange(IEnumerable<T> collection)
	{
        ArgumentNullException.ThrowIfNull(collection);
        using var en = collection.GetEnumerator();
        while (en.MoveNext())
	        Add(en.Current);
	}
	
	public void Clear() => (Head, Tail, Count, OwnerId, Version) 
		= (null, null, 0, (ushort)Random.Shared.Next(1, ushort.MaxValue + 1),Version + 1);

	public bool Contains(T item) => FindFirst(item, out _, out _);
	
	public void CopyTo(T[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array was not long enough.");
		
		if(IsEmpty)
			return;
		
		var curr = Head;
		do
			array[arrayIndex++] = curr!.Value!;
		while (MoveNext(curr, false, out curr));
	}

	public bool Remove(T item)
	{
		if (!FindFirst(item, out _, out var node)) return false;
		Remove(node);
		return true;
	}

	public IEnumerator<T> GetEnumerator(bool reverse)
	{
		if (IsEmpty)
			yield break;
		var initialVersion = Version;
		var curr = reverse ? Tail : Head;
		do
		{
			if (Version != initialVersion)
				throw new InvalidOperationException("Collection was modified during Enumeration");
			yield return curr!.Value!;
		} while (MoveNext(curr, reverse, out curr));
	}
	
	public IEnumerator<T> GetEnumerator() => GetEnumerator(false);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int IndexOf(T item)
	{
		if (!FindFirst(item, out var idx, out _)) return -1;
		return idx;
	}

	public void Insert(int index, T item)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
		
		if (IsEmpty)
			Add(item);
		
		else if (index == 0)
		{
			Head = Head!.Previous = new LinkedListNode<T>(OwnerId, item)
			{
				Next = Head
			};
			Count++;
			Version++;
		}
		else if (index == Count)
			Add(item);
		else if (TryGetAtIndex(index, out var node))
		{
			var res = new LinkedListNode<T>(OwnerId, item)
			{
				Previous = node.Previous,
				Next = node
			};
			node.Previous = node.Previous!.Next = res;
			Count++;
			Version++;
		}
		else 
			throw new IndexOutOfRangeException($"The specified index ({index}) is out of range.");
	}

	public void RemoveAt(int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		
		if (TryGetAtIndex(index, out var node))
			Remove(node);
		else 
			throw new IndexOutOfRangeException($"The specified index ({index}) is out of range.");
	}

	public T this[int index]
	{
		get => 
			TryGetAtIndex(index, out var node) 
				? node.Value! 
				: throw new IndexOutOfRangeException($"The specified index ({index}) is out of range.");
		set
		{
			if (TryGetAtIndex(index, out var node)) node.Value = value;
			else throw new IndexOutOfRangeException($"The specified index ({index}) is out of range.");
		}
	}
}