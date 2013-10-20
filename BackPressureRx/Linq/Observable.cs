using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackPressureRx.Linq.Query;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace BackPressureRx.Linq
{
    public static class ObservableExtensions
    {
        public static IControlledObservable<T> Controlled<T>(this IObservable<T> This, bool enableQueue = true)
        {
            return new Controlled<T>(This, enableQueue);
        }

        public static IObservable<T> StopAndWait<T>(this IControlledObservable<T> This)
        {
            return new StopAndWait<T>(This);
        }

        public static IObservable<T> Pausable<T>(this IObservable<T> This, IObservable<bool> pauser)
        {
            return Observable.Create<T>(observer =>
            {
                var conn = This.Publish();
                var subscription = conn.Subscribe(observer);
                var connection = Disposable.Empty;
                var pausable = pauser.DistinctUntilChanged().Subscribe(b => 
                {
                    if(b)
                    {
                        connection = conn.Connect();
                    }
                    else                        
                    {
                        connection.Dispose();
                        connection = Disposable.Empty;
                    }
                });
                return new CompositeDisposable(subscription, connection, pausable);
            });
        }
    }
}
