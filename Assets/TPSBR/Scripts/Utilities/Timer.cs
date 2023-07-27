namespace TPSBR
{
	using System;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;

	public sealed partial class Timer
	{
		public enum EState
		{
			Stopped = 0,
			Running = 1,
			Paused  = 2,
		}

		// PUBLIC MEMBERS

		public readonly int    ID;
		public readonly string Name;

		public EState   State      { get { return _state;   } }
		public int      Counter    { get { return _counter; } }
		public TimeSpan TotalTime  { get { if (_state == EState.Running) { Update(); } return new TimeSpan(_totalTicks);  } }
		public TimeSpan RecentTime { get { if (_state == EState.Running) { Update(); } return new TimeSpan(_recentTicks); } }
		public TimeSpan PeakTime   { get { if (_state == EState.Running) { Update(); } return new TimeSpan(_peakTicks);   } }
		public TimeSpan LastTime   { get { if (_state == EState.Running) { Update(); } return new TimeSpan(_lastTicks);   } }

		// PRIVATE MEMBERS

		private EState _state;
		private int    _counter;
		private long   _baseTicks;
		private long   _totalTicks;
		private long   _recentTicks;
		private long   _peakTicks;
		private long   _lastTicks;

		private static readonly Pool<Timer> _pool = new Pool<Timer>();

		// CONSTRUCTORS

		public Timer() : this(null)
		{
		}

		public Timer(string name) : this(-1, name)
		{
		}

		public Timer(int id, string name)
		{
			ID   = id;
			Name = name;
		}

		// PUBLIC METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Start()
		{
			if (_state == EState.Running)
				return;

			if (_state != EState.Paused)
			{
				if (_recentTicks != 0)
				{
					_lastTicks   = _recentTicks;
					_recentTicks = 0;
				}

				++_counter;
			}

			_baseTicks = Stopwatch.GetTimestamp();
			_state     = EState.Running;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Pause()
		{
			if (_state != EState.Running)
				return;

			Update();

			_state = EState.Paused;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Stop()
		{
			if (_state == EState.Running)
			{
				Update();
			}

			_state = EState.Stopped;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Restart()
		{
			if (_recentTicks != 0)
			{
				_lastTicks = _recentTicks;
			}

			_state       = EState.Running;
			_counter     = 1;
			_baseTicks   = Stopwatch.GetTimestamp();
			_recentTicks = 0;
			_totalTicks  = 0;
			_peakTicks   = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			if (_recentTicks != 0)
			{
				_lastTicks = _recentTicks;
			}

			_state       = EState.Stopped;
			_counter     = 0;
			_baseTicks   = 0;
			_recentTicks = 0;
			_totalTicks  = 0;
			_peakTicks   = 0;
		}

		public void Combine(Timer other)
		{
			if (other._state == EState.Running)
			{
				other.Update();
			}

			_totalTicks += other._totalTicks;

			if (_state == EState.Stopped)
			{
				_recentTicks = other._recentTicks;
				if (_recentTicks > _peakTicks)
				{
					_peakTicks = _recentTicks;
				}
			}

			if (other._peakTicks > _peakTicks)
			{
				_peakTicks = other._peakTicks;
			}

			_counter += other._counter;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetTotalSeconds()
		{
			return (float)TotalTime.TotalSeconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetTotalMilliseconds()
		{
			return (float)TotalTime.TotalMilliseconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetRecentSeconds()
		{
			return (float)RecentTime.TotalSeconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetRecentMilliseconds()
		{
			return (float)RecentTime.TotalMilliseconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetPeakSeconds()
		{
			return (float)PeakTime.TotalSeconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetPeakMilliseconds()
		{
			return (float)PeakTime.TotalMilliseconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetLastSeconds()
		{
			return (float)LastTime.TotalSeconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float GetLastMilliseconds()
		{
			return (float)LastTime.TotalMilliseconds;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Timer Get(bool start = false)
		{
			Timer timer = _pool.Get();
			if (start == true)
			{
				timer.Restart();
			}
			return timer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return(Timer timer)
		{
			timer.Reset();
			_pool.Return(timer);
		}

		// PRIVATE METHODS

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Update()
		{
			long ticks = Stopwatch.GetTimestamp();

			_totalTicks  += ticks - _baseTicks;
			_recentTicks += ticks - _baseTicks;

			_baseTicks = ticks;

			if (_recentTicks > _peakTicks)
			{
				_peakTicks = _recentTicks;
			}

			if (_totalTicks < 0L)
			{
				_totalTicks = 0L;
			}
		}
	}
}
