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
        protected IDisposable subscription;

        public StopAndWait(IControlledObservable<T> observable)
        {
            this.source = observable;
            this.controller = new Subject<int>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            IDisposable subscription = this.source.Subscribe(new StopAndWaitObserver<T>(observer, this));
            IDisposable controllerSubscription = this.source.ControlledBy(this.controller);

            this.subscription = new CompositeDisposable(subscription, controllerSubscription);

            //start requesting the first value
            DefaultScheduler.Instance.Schedule(() => this.controller.OnNext(1));

            return this.subscription;
        }

        class StopAndWaitObserver<T> : IObserver<T>, IDisposable
        {
            private IObserver<T> observer;
            private readonly StopAndWait<T> observable;

            public StopAndWaitObserver(IObserver<T> observer, StopAndWait<T> observable)
            {
                this.observer = observer;
                this.observable = observable;
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

                var cancel = Interlocked.Exchange(ref observable.subscription, null);
                if (cancel != null)
                {
                    cancel.Dispose();
                }
            }
        }
    }
}
