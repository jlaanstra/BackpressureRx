using BackPressureRx.Subjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace BackPressureRx.Linq.Query
{
    class Controlled<T> : IControlledObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly ControlledSubject<T> subject;
        private IDisposable subscription;

        public Controlled(IObservable<T> source, bool enableQueue)
        {
            this.subject = new ControlledSubject<T>(enableQueue);
            this.source = source.Multicast(subject).RefCount();
        }

        public IDisposable Request(int numberOfItems = -1)
        {
            return this.subject.Request(numberOfItems);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.source.Subscribe(observer);
        }
    }
}
