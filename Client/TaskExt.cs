using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApplication {
    // by noseratio - stackoverflow.com/users/1768303/noseratio
    class TaskExt {
        // use TaskExt.Delay the same way as Task.Delay
        public static Task Delay(int delay, CancellationToken token = default(CancellationToken)) {
            if (delay == 0)
                return Task.Delay(0, token);
            if (delay == Timeout.Infinite)
                return Task.Delay(Timeout.Infinite, token);

            return DelayImplAsync(delay, token);
        }

        private static async Task DelayImplAsync(int delay, CancellationToken token) {
            using (var timerAwaiter = new TimerAwaiter(delay, token )) {
                using (token.Register(() =>
                timerAwaiter.ContinueWithCancellation(),
                useSynchronizationContext: false)) {
                    await timerAwaiter;
                }
            }
        }

        // custom awaiter for Win32 timer
        private class TimerAwaiter :
        System.Runtime.CompilerServices.ICriticalNotifyCompletion,
        IDisposable {
            readonly uint _delay;
            readonly CancellationToken _token;

            Action _continuation;
            ExecutionContext _capturedExecutionContext;

            IntPtr _timerHandle;
            NativeMethods.WaitOrTimerCallbackProc _timerCallback;
            GCHandle _gcHandle;

            bool _completed = false;
            bool _continuationQueued = false;

            internal TimerAwaiter(int delay, CancellationToken token) {
                _delay = (uint)delay;
                _token = token;
            }

            internal void ContinueWithCancellation() {
                Cleanup();
                Continue(); // this can safely race with the timer callback
            }

            void Continue() {
                lock (this) {
                    if (_continuation == null || _continuationQueued)
                        return;
                    _continuationQueued = true;
                }

                // can be optimized to use the state args
                // instead of capturing lambdas
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                ExecutionContext.Run(
                _capturedExecutionContext,
                __ => {
                    Volatile.Write(ref _completed, true);
                    _continuation();
                },
                null), null);
            }

            void Cleanup() {
                lock (this) {
                    if (_timerHandle != IntPtr.Zero) {
                        NativeMethods.DeleteTimerQueueTimer(IntPtr.Zero, _timerHandle, IntPtr.Zero);
                        _timerHandle = IntPtr.Zero;
                    }

                    if (_gcHandle.IsAllocated)
                        _gcHandle.Free();
                }
            }

            // Awaiter methods
            public TimerAwaiter GetAwaiter() {
                return this;
            }

            public bool IsCompleted {
                get { return _token.IsCancellationRequested || Volatile.Read(ref _completed); }
            }

            public void GetResult() {
                _token.ThrowIfCancellationRequested();
            }

            // INotifyCompletion
            public void OnCompleted(Action continuation) {
                throw new NotImplementedException();
            }

            // ICriticalNotifyCompletion
            public void UnsafeOnCompleted(Action continuation) {
                lock (this) {
                    _token.ThrowIfCancellationRequested();
                    _continuation = continuation;
                }

                // by default, ExecutionContext.Capture does
                // captures SynchronizationContext, avoid that
                _capturedExecutionContext = TaskExt.WithoutSynchronizationContext(() =>
                ExecutionContext.Capture());

                using (ExecutionContext.SuppressFlow()) {
                    _timerCallback = delegate { this.Continue(); };
                    _gcHandle = GCHandle.Alloc(_timerCallback);

                    if (!NativeMethods.CreateTimerQueueTimer(
                    out _timerHandle,
                    IntPtr.Zero,
                    _timerCallback,
                    IntPtr.Zero, _delay, 0,
                    (UIntPtr)(NativeMethods.WT_EXECUTEINTIMERTHREAD | NativeMethods.WT_EXECUTEONLYONCE))) {
                        throw new System.ComponentModel.Win32Exception(
                        Marshal.GetLastWin32Error());
                    }
                }
            }

            // IDisposable
            public void Dispose() {
                Cleanup();
            }

            // p/invoke
            static class NativeMethods {
                public const uint WT_EXECUTEINTIMERTHREAD = 0x00000020;
                public const uint WT_EXECUTEONLYONCE = 0x00000008;

                public delegate void WaitOrTimerCallbackProc(IntPtr lpParameter, bool TimerOrWaitFired);

                [DllImport("kernel32.dll", SetLastError = true)]
                public static extern bool CreateTimerQueueTimer(out IntPtr phNewTimer,
                IntPtr TimerQueue, WaitOrTimerCallbackProc Callback, IntPtr Parameter,
                uint DueTime, uint Period, UIntPtr Flags);

                [DllImport("kernel32.dll", SetLastError = true)]
                public static extern bool DeleteTimerQueueTimer(IntPtr TimerQueue, IntPtr Timer, IntPtr CompletionEvent);
            }
        }

        // execute func without SynchronizationContext
        public static TResult WithoutSynchronizationContext<TResult>(Func<TResult> func) {
            var savedSC = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try {
                return func();
            } finally {
                SynchronizationContext.SetSynchronizationContext(savedSC);
            }
        }
    }

    // testing
    class Program {
        static void Main2(string[] args) {
            var cs = new CancellationTokenSource(1000);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try {
                TaskExt.Delay(500, cs.Token).Wait();
                Console.WriteLine("lapse: " + sw.ElapsedMilliseconds);

                TaskExt.Delay(600, cs.Token).Wait();
                Console.WriteLine("lapse: " + sw.ElapsedMilliseconds);
            } catch (Exception ex) {
                Console.WriteLine(ex.InnerException.Message);
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
} 
