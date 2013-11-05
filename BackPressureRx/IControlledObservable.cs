using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPressureRx
{
    /// <summary>
    /// Represents an observable which only produces a value if requested by a controller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IControlledObservable<out T> : IObservable<T>
    {
        IDisposable Request(int numberOfItems = -1);
    }
}
