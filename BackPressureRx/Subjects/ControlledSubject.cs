using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackPressureRx.Subjects
{
    public class ControlledSubject<T> : ISubject<T>
    {
        private readonly ISubject<T> subject;
        private readonly object gate;
        private Queue<T> queue;
        private IDisposable controllerDisp;

        private bool hasCompleted;
        private bool hasFailed;
        private Exception error;
        private readonly bool enableQueue;

        private Requested requested;
        private IDisposable requestedDisposable;

        public ControlledSubject(bool enableQueue = true)
        {
            this.enableQueue = enableQueue;
            this.subject = new Subject<T>();
            this.gate = new object();
            this.queue = enableQueue ? new Queue<T>() : null;
            this.requested = Requested.Empty;
            this.requestedDisposable = Disposable.Empty;
            this.hasFailed = false;
            this.hasCompleted = false;
            this.controllerDisp = Disposable.Empty;
        }

        public void OnCompleted()
        {
            lock (gate)
            {
                this.hasCompleted = true;
                //if queueing is disabled or queue is empty
                //send completes immediately
                if (!enableQueue || this.queue.Count == 0)
                {
                    this.subject.OnCompleted();
                }
            }
        }

        public void OnError(Exception error)
        {
            lock (gate)
            {
                this.hasFailed = true;
                this.error = error;
                //if queueing is disabled or queue is empty
                //send errors immediately
                if (!enableQueue || this.queue.Count == 0)
                {
                    this.subject.OnError(error);
                }
            }
        }

        public void OnNext(T value)
        {
            bool hasRequested = true;

            lock (gate)
            {
                Requested req = this.requested;

                if (req == Requested.Empty)
                {
                    if (enableQueue)
                    {
                        this.queue.Enqueue(value);
                    }
                }
                else
                {
                    if (req != Requested.Unbounded)
                    {
                        if (Interlocked.Decrement(ref req.count) == 0)
                        {
                            this.DisposeCurrentRequest();
                        }
                    }
                    hasRequested = true;
                }
            }

            if (hasRequested)
            {
                this.subject.OnNext(value);
            }
        }

        private bool ProcessRequest(ref int numberOfItems)
        {
            if (enableQueue)
            {
                // as long as we have queued items, send them
                while (queue.Count >= numberOfItems && numberOfItems > 0)
                {
                    this.subject.OnNext(queue.Dequeue());
                    numberOfItems--;
                }
                //if the queue is not empty, wait for new request and return
                if (queue.Count != 0)
                {
                    //we could send the requested number of item immediately
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //queue is empty
            //error and completion are not considered items and can be send immediately
            if (hasFailed)
            {
                this.subject.OnError(this.error);
                this.controllerDisp.Dispose();
                this.controllerDisp = Disposable.Empty;
            }
            else if (this.hasCompleted)
            {
                this.subject.OnCompleted();
                this.controllerDisp.Dispose();
                this.controllerDisp = Disposable.Empty;
            }
            return false;
        }

        public IDisposable Request(int number)
        {
            lock (gate)
            {
                this.DisposeCurrentRequest();

                if (!ProcessRequest(ref number))
                {
                    this.requested = new Requested() { count = number };
                    this.requestedDisposable = Disposable.Create(() =>
                    {
                        lock (gate)
                        {
                            this.requested = Requested.Empty;
                        }
                    });
                    return this.requestedDisposable;
                }
                else
                {
                    return Disposable.Empty;
                }
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.subject.Subscribe(observer);
        }

        private void DisposeCurrentRequest()
        {
            this.requestedDisposable.Dispose();
            this.requestedDisposable = Disposable.Empty;
        }

        class Requested
        {
            public int count;

            public static Requested Empty = new Requested();

            public static Requested Unbounded = new Requested() { count = -1 };
        }
    }
}
