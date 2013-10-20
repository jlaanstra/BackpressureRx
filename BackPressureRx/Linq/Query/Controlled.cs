using BackPressureRx.Subjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace BackPressureRx.Linq.Query
{
    class Controlled<T> : IControlledObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly ControlledSubject<T> subject;
        private readonly object gate;
        private IDisposable subscription;
        private IDisposable controller;

        public Controlled(IObservable<T> source, bool enableQueue)
        {
            this.source = source;
            this.subject = new ControlledSubject<T>(enableQueue);
            this.gate = new object();
        }

        public IDisposable ControlledBy(IObservable<int> controller)
        {
            lock(this.gate)
            {
                if(this.controller != null)
                {
                    throw new InvalidOperationException("Observable can only be controlled by a single controller.");
                }
                this.subscription = source.Subscribe(this.subject);
                this.controller = this.subject.ControlledBy(controller);
                return new CompositeDisposable(this.subscription, this.controller);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
