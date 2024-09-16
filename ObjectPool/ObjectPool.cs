namespace ObjectPool;

public abstract class ObjectPool<T>
{
	private int _size;
	private T[] _objects;

	public ObjectPool(int size)
	{
		this._size = size;
		_objects = new T[size];
	}
	
	public int Size => _size;
	
	public abstract T Create();
	
	private volatile int _pushIndex = 0;
	private volatile int _pullIndex = 0;
	private volatile int _okObjectCount = 0;
	private readonly object _pushLockObject = new object();
	private readonly object _pullLockObject = new object();

	public void UpdateSize(int size)
	{
		if (size == _size) return;
		
		lock (_pushLockObject) { lock (_pullLockObject) 
			{
				T[] temp = new T[size];
				if (0 < _okObjectCount)
				{
					if (_size < size)
					{
						if (_pullIndex < _pushIndex)
						{
							/* h = 可用  o = 旧   n = 空 / 旧的
							 * h = ok    o = old  n = null / old
							 *                |-----copy-----|
							 *oooooooooooooooohhhhhhhhhhhhhhhhnnnnnnnnnnnnnnnn
							 *               ^               ^
							 *           _pullIndex      _pushIndex
							 *
							 *
							 */
							

							Array.Copy(_objects, _pullIndex, temp, 0, _okObjectCount);
							_pushIndex -= _pullIndex;
						}
						else
						{

							/* h = 可用  o = 旧   n = 空 / 旧的
							 * h = ok    o = old  n = null / old
                             *
							 *                              _pullIndex
							 * 0           _pushIndex          | _size - _pullIndex + 1|
							 * |  _pushIndex  |                |            ------------
							 * ∨              ∨|--- no copy---|∨            ∨
							 * hhhhhhhhhhhhhhhhoooooooooooooooohhhhhhhhhhhhhh
							 *                ^                ^
							 *            _pushIndex      _pullIndex
							 */
							int length = _size - _pullIndex;
							
							Array.Copy(_objects, _pullIndex, temp, 0, length);
							
							

							Array.Copy(_objects, 0, temp, length, _pushIndex);

							_pushIndex += length;
						}

					}
					else
					{
						if (_pullIndex < _pushIndex)
						{
							/* h = 可用  o = 旧   n = 空 / 旧的
							 * h = ok    o = old  n = null / old
							 *                |-----copy-----|
							 *oooooooooooooooohhhhhhhhhhhhhhhhnnnnnnnnnnnnnnnn
							 *               ^               ^
							 *           _pullIndex      _pushIndex
							 *
							 *
							 */
							int length = _okObjectCount;
							if (size < length)
							{
								length = size;
								_pushIndex = 0;
								_okObjectCount = size;

							}else if (size == length)
							{
								_pushIndex = 0;
							}
							else
							{
								_pushIndex = length;
							}
							

							Array.Copy(_objects, _pullIndex, temp, 0, length);

						}
						else
						{

							/* h = 可用  o = 旧   n = 空 / 旧的
							 * h = ok    o = old  n = null / old
							 *
							 *                              _pullIndex
							 * 0           _pushIndex          | _size - _pullIndex + 1|
							 * |  _pushIndex  |                |            ------------
							 * ∨              ∨|--- no copy---|∨            ∨
							 * hhhhhhhhhhhhhhhhoooooooooooooooohhhhhhhhhhhhhh
							 *                ^                ^
							 *            _pushIndex      _pullIndex
							 */
							int length = _size - _pullIndex;
							if (size < length)
							{
								length = size;
								_pushIndex = length;
								_okObjectCount = size;
								

								Array.Copy(_objects, _pullIndex, temp, 0, length);

							}else if (size == length)
							{
								_pushIndex = 0;
								_okObjectCount = size;

								

								Array.Copy(_objects, _pullIndex, temp, 0, length);

							}
							else
							{
								

								Array.Copy(_objects, _pullIndex, temp, 0, length);

								int index = length;
								int length2 = length + _pushIndex;
								if (size < length2)
								{
									length = size - length;
									_pushIndex = 0;
									_okObjectCount = size;


								}else if (size == length2)
								{
									length = _pushIndex;
									_pushIndex = 0;
									_okObjectCount = size;

								}
								else
								{
									length = _pushIndex;
									_pushIndex = length2;
								}
								

								Array.Copy(_objects, 0, temp, index, length);


							}

						}
					}
				}
				else
				{
					_pushIndex = 0;
				}
				

				_pullIndex = 0;
				_objects = temp;
				_size = size;

				//locked
			}
		}
	}


	
	public void Push(T obj)
	{
		int index;
		if (_okObjectCount == _size)
		{
			return;
		}
		
		Monitor.Enter(_pushLockObject);
		if (_okObjectCount == _size)
		{
			Monitor.Exit(_pushLockObject);
			
			return;
		}
		if (_pushIndex == _size)
		{
			_pushIndex = 1;
			index = 0;
		}
		else
		{
			index = _pushIndex;
			_pushIndex++;
		}

		

		_objects[index] = obj;
		try
		{
			Interlocked.Add(ref _okObjectCount, 1);
		}
		finally
		{
			Monitor.Exit(_pushLockObject);
		}

		

	}


	public T Pull()
	{
		int index;
		if (_okObjectCount == 0)
		{
			
			return Create();
		}
        Monitor.Enter(_pullLockObject);
        if (_okObjectCount == 0)
        {
	        Monitor.Exit(_pullLockObject);
	        

	        return Create();
        }
        if (_pullIndex == _size)
        {
	        _pullIndex = 1;
	        index = 0;
        }
        else
        {
	        index = _pullIndex++;
        }

        


        T pull = _objects[index];
        try
        {
	        Interlocked.Add(ref _okObjectCount, -1);
        }
        finally
        {
	        Monitor.Exit(_pullLockObject);
        }
        

        return pull;
		
	}

	

}