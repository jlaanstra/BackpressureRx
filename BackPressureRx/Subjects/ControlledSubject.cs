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
        private bool enableQueue;

        private int requested;

        public ControlledSubject(bool enableQueue = true)
        {
            this.enableQueue = enableQueue;
            this.subject = new Subject<T>();
            this.gate = new object();
            this.queue = enableQueue ? new Queue<T>() : null;
            this.requested = 0;
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
            bool isRequesting = false;

            lock(gate)
            {                
                isRequesting = requested > 0;
                //only queue if enabled and not requesting
                if(!isRequesting)
                {
                    if (enableQueue)
                    {
                        this.queue.Enqueue(value);
                    }
                }
                else
                {
                    this.requested--;
                }
            }

            if(isRequesting)
            {
                this.subject.OnNext(value);
            }
        }

        private void OnRequest(int numberOfItems)
        {
            lock(gate)
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
                    if(queue.Count != 0)
                    {
                        return;
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
                this.requested += numberOfItems;
            }
        }

        public IDisposable ControlledBy(IObservable<int> controller)
        {
            lock(gate)
            {
                if(controllerDisp != Disposable.Empty)
                {
                    throw new InvalidOperationException("Observable can only be controlled by a single controller.");
                }
                if(this.hasCompleted && this.queue.Count == 0)
                {
                    throw new InvalidOperationException("Observable has already completed");
                }
                this.controllerDisp = controller.Subscribe(OnRequest, OnError, OnCompleted);

                return this.controllerDisp;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return this.subject.Subscribe(observer);
        }
    }
}
