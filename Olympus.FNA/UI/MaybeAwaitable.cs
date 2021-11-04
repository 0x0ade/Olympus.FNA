using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OlympUI {
    // Based on the MaybeAwaitable code found in Everest's MainThreadHelper.
    public struct MaybeAwaitable {

        private MaybeAwaiter _Awaiter;
        public readonly bool IsValid;

        public MaybeAwaitable(TaskAwaiter task) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._Task = task;
            _Awaiter._CanGetResult = null;
            IsValid = true;
        }

        public MaybeAwaitable(Func<bool> canGetResult) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._MRE = new ManualResetEventSlim(false);
            _Awaiter._CanGetResult = canGetResult;
            IsValid = true;
        }

        public MaybeAwaitable(TaskAwaiter task, Func<bool> canGetResult) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._Task = task;
            _Awaiter._CanGetResult = canGetResult;
            IsValid = true;
        }

        public MaybeAwaiter GetAwaiter() => _Awaiter;
        public void GetResult() => _Awaiter.GetResult();
        public void SetResult() => _Awaiter.SetResult();

        public struct MaybeAwaiter : ICriticalNotifyCompletion {

            internal bool _IsImmediate;
            internal TaskAwaiter _Task;
            internal ManualResetEventSlim? _MRE;
            internal Func<bool>? _CanGetResult;

            public bool IsCompleted => _IsImmediate || (_MRE?.IsSet ?? _Task.IsCompleted);

            private bool WaitForMRE() {
                ManualResetEventSlim? mre = _MRE;
                if (mre != null) {
                    try {
                        mre.Wait();
                        mre.Dispose();
                    } catch (Exception) {
                        try {
                            mre.Dispose();
                        } catch (Exception) {
                        }
                    }
                    _IsImmediate = true;
                    _MRE = null;
                    return true;
                }

                return false;
            }

            public void GetResult() {
                if (_IsImmediate)
                    return;

                if (!(_CanGetResult?.Invoke() ?? true))
                    throw new Exception("Cannot obtain the result - potential deadlock!");

                if (WaitForMRE())
                    return;

                _Task.GetResult();
            }

            public void SetResult() {
                if (_MRE == null)
                    throw new InvalidOperationException("Cannot set a result on a MaybeAwaiter that doesn't expect one!");
                _IsImmediate = true;
                _MRE.Set();
            }

            public void OnCompleted(Action continuation) {
                if (_IsImmediate) {
                    continuation();
                    return;
                }

                if (WaitForMRE()) {
                    continuation();
                    return;
                }

                _Task.OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                if (_IsImmediate) {
                    continuation();
                    return;
                }

                if (WaitForMRE()) {
                    continuation();
                    return;
                }

                _Task.UnsafeOnCompleted(continuation);
            }

        }

    }

    public struct MaybeAwaitable<T> {

        private MaybeAwaiter _Awaiter;
        public readonly bool IsValid;

        public MaybeAwaitable(T? result) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = true;
            _Awaiter._Result = result;
            _Awaiter._CanGetResult = null;
            IsValid = true;
        }

        public MaybeAwaitable(TaskAwaiter<T> task) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._Result = default;
            _Awaiter._Task = task;
            _Awaiter._CanGetResult = null;
            IsValid = true;
        }

        public MaybeAwaitable(Func<bool> canGetResult) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._Result = default;
            _Awaiter._MRE = new ManualResetEventSlim(false);
            _Awaiter._CanGetResult = canGetResult;
            IsValid = true;
        }

        public MaybeAwaitable(TaskAwaiter<T> task, Func<bool> canGetResult) {
            _Awaiter = new MaybeAwaiter();
            _Awaiter._IsImmediate = false;
            _Awaiter._Result = default;
            _Awaiter._Task = task;
            _Awaiter._CanGetResult = canGetResult;
            IsValid = true;
        }

        public MaybeAwaiter GetAwaiter() => _Awaiter;
        public T? GetResult() => _Awaiter.GetResult();
        public void SetResult(T result) => _Awaiter.SetResult(result);

        public struct MaybeAwaiter : ICriticalNotifyCompletion {

            internal bool _IsImmediate;
            internal T? _Result;
            internal TaskAwaiter<T> _Task;
            internal ManualResetEventSlim? _MRE;
            internal Func<bool>? _CanGetResult;

            public bool IsCompleted => _IsImmediate || (_MRE?.IsSet ?? _Task.IsCompleted);

            private bool WaitForMRE() {
                ManualResetEventSlim? mre = _MRE;
                if (mre != null) {
                    try {
                        mre.Wait();
                        mre.Dispose();
                    } catch (Exception) {
                        try {
                            mre.Dispose();
                        } catch (Exception) {
                        }
                    }
                    _IsImmediate = true;
                    _MRE = null;
                    return true;
                }

                return false;
            }

            public T? GetResult() {
                if (_IsImmediate)
                    return _Result;

                if (!(_CanGetResult?.Invoke() ?? true))
                    throw new Exception("Cannot obtain the result - potential deadlock!");

                if (WaitForMRE())
                    return _Result;

                return _Task.GetResult();
            }

            public void SetResult(T result) {
                if (_MRE == null)
                    throw new InvalidOperationException("Cannot set a result on a MaybeAwaiter that doesn't expect one!");
                _Result = result;
                _IsImmediate = true;
                _MRE.Set();
            }

            public void OnCompleted(Action continuation) {
                if (_IsImmediate) {
                    continuation();
                    return;
                }

                if (WaitForMRE()) {
                    continuation();
                    return;
                }

                _Task.OnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                if (_IsImmediate) {
                    continuation();
                    return;
                }

                if (WaitForMRE()) {
                    continuation();
                    return;
                }

                _Task.UnsafeOnCompleted(continuation);
            }

        }

    }
}
