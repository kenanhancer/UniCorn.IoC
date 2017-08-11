using System;
using System.Threading;

namespace UniCorn.Core
{
    public class Synchronized<T> where T : class
    {
        private T value;
        //public T Value => value;

        public Synchronized(T value)
        {
            this.value = value;
        }

        public bool Invoke(Action<T> operation)
        {
            bool lockTaken = false;

            try
            {
                Monitor.Enter(value, ref lockTaken);
                operation(value);
            }
            //catch
            //{
            //}
            finally
            {
                if (lockTaken)
                    Monitor.Exit(value);
            }

            return lockTaken;
        }

        public R Invoke<R>(Func<T, R> operation)
        {
            bool lockTaken = false;
            R result = default(R);

            try
            {
                Monitor.Enter(value, ref lockTaken);
                result = operation(value);
            }
            //catch
            //{
            //}
            finally
            {
                if (lockTaken)
                    Monitor.Exit(value);
            }

            return result;
        }
    }
}