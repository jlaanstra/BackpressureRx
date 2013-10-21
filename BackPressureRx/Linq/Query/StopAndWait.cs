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
    class StopAndWait<T> : IObservable<T>
    {
        private readonly IControlledObservable<T> source;
        protected readonly ISubject<int> controller;
        protected IDisposable controllerSubscription;
        private readonly object gate;
        private int count;

        public StopAndWait(IControlledObservable<T> observable)
        {
            this.source = observable;
            this.controller = new Subject<int>();
            this.count = 0;
            this.gate = new object();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            SerialDisposable subscription = new SerialDisposable();
            subscription.Disposable = this.source.Subscribe(new StopAndWaitObserver<T>(observer, this, subscription));

            lock (gate)
            {
                if (count++ == 0)
                {
                    controllerSubscription = this.source.ControlledBy(this.controller);
                }
            }

            //start requesting the first value
            DefaultScheduler.Instance.Schedule(() => this.controller.OnNext(1));

            return Disposable.Create(() =>
            {
                subscription.Dispose();

                lock(gate)
                {
                    if(--count == 0)
                    {
                        controllerSubscription.Dispose();
                    }
                }
            });
        }

        class StopAndWaitObserver<T> : IObserver<T>, IDisposable
        {
            private readonly StopAndWait<T> observable;
            private IObserver<T> observer;
            private IDisposable cancel;

            public StopAndWaitObserver(IObserver<T> observer, StopAndWait<T> observable, IDisposable cancel)
            {
                this.observer = observer;
                this.observable = observable;
                this.cancel = cancel;
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
                //request new value after processing of the current one completed
                DefaultScheduler.Instance.Schedule(() => this.observable.controller.OnNext(1));
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
