using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AppBroker.maxi;

public static class IObservableExtension
{
    public static IObservable<T> Serial<T>(this IObservable<IObservable<T>> observable)
    {
        return Observable
            .Using(
                () => new SerialContext<T>(observable),
                context => context.Select()
            );
    }


    private class SerialContext<T> : IDisposable
    {
        public Subject<T> Subject { get; }
        public SerialDisposable SerialSubscription { get; }

        private IDisposable? internalSub;

        private readonly IObservable<IObservable<T>> internalStream;

        public SerialContext(IObservable<IObservable<T>> serialStream)
        {
            Subject = new Subject<T>();
            SerialSubscription = new SerialDisposable();

            internalStream = serialStream;
        }

        public IObservable<T> Select()
        {
            internalSub
                = internalStream
                .Subscribe(
                    serialEelement => SerialSubscription.Disposable = serialEelement.Subscribe(Subject),
                    Subject.OnError,
                    Subject.OnCompleted
                );

            return Subject;
        }


        public void Dispose()
        {
            SerialSubscription.Dispose();
            Subject.Dispose();
            internalSub?.Dispose();
        }
    }
}

