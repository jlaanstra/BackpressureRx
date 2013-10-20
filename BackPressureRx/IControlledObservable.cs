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
        /// <summary>
        /// Controls when the observable produces a new value. New values are only produced as long as the controller requests them.
        /// </summary>
        /// <param name="controller">The controller that requests new values.</param>
        /// <returns>An IDisposable to stop the observable from listening to the controller.</returns>
        IDisposable ControlledBy(IObservable<int> controller);
    }
}
