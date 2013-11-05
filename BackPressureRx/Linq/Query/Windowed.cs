using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackPressureRx.Linq.Query
{
    class Windowed<T> : IObservable<T>
    {
        private readonly IControlledObservable<T> source;
        private IDisposable subscription;
        private readonly object gate;

        private int window;

        public Windowed(IControlledObservable<T> observable, int window)
        {
            this.source = observable;
            this.window = window;
            this.gate = new object();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            this.subscription = this.source.Subscribe(new WindowedObserver<T>(observer, this, subscription));

            //start requesting the first value
            DefaultScheduler.Instance.Schedule(() => this.source.Request(this.window));

            return this.subscription;
        }

        class WindowedObserver<T> : IObserver<T>, IDisposable
        {
            private readonly Windowed<T> observable;
            private IObserver<T> observer;
            private IDisposable cancel;
            private int received;

            public WindowedObserver(IObserver<T> observer, Windowed<T> observable, IDisposable cancel)
            {
                this.observer = observer;
                this.observable = observable;
                this.cancel = cancel;
                this.received = 0;
            }

            public void OnCompleted()
            {
                this.observer.OnCompleted();
                this.Dispose();
            }

            public void OnError(Exception error)
            {
                this.observer.OnError(error);
                this.Dispose();
            }

            public void OnNext(T value)
            {
                this.observer.OnNext(value);

                this.received = ++this.received % this.observable.window;
                if (this.received == 0)
                {
                    DefaultScheduler.Instance.Schedule(() => this.observable.source.Request(this.observable.window));
                }
            }

            public void Dispose()
            {
                observer = NopObserver<T>.Instance;

                var cancel = Interlocked.Exchange(ref this.cancel, null);
                if (cancel != null)
                {
                    cancel.Dispose();
                }
            }
        }
    }
}
