using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DS4AudioStreamer.Sound
{
    class AccurateTimer: IDisposable
    {
        private delegate void TimerEventDel(int id, int msg, IntPtr user, int dw1, int dw2);

        private const int TimePeriodic = 1;
        private const int EventType = TimePeriodic; // + 0x100;  // TIME_KILL_SYNCHRONOUS causes a hang ?!

        [DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int msec);

        [DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int msec);

        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, IntPtr del, IntPtr user,
            int eventType);

        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        private readonly Action _action;
        private readonly int _timerId;

        // NOTE: declare at class scope so garbage collector doesn't release it!!!
        private GCHandle _delegateHandle; 

        public AccurateTimer(Action action, int delay)
        {
            _action = action;
            timeBeginPeriod(1);
            var del = new TimerEventDel(TimerCallback);
            _delegateHandle = GCHandle.Alloc(del);
            _timerId = timeSetEvent(delay, 0, Marshal.GetFunctionPointerForDelegate(del), IntPtr.Zero, EventType);
        }

        public void Stop()
        {
            var err = timeKillEvent(_timerId);
            timeEndPeriod(1);
            Thread.Sleep(100); // Ensure callbacks are drained
        }

        private void TimerCallback(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (_timerId != 0)
                _action();
        }

        public void Dispose()
        {
            _delegateHandle.Free();
        }
    }
}