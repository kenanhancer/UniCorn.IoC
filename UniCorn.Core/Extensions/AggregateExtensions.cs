using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace UniCorn.Core
{
    public static class AggregateExtensions
    {
        private static ConcurrentDictionary<string, object> invokerList = new ConcurrentDictionary<string, object>();

        public static Func<dynamic, Task<R>> Aggregate<T, R>(this IList<T> handlers, Func<T, Task<R>> operation, Func<AggregateBase, dynamic, string, bool> preInvokeCallback = null, string methodName = null, int index = 0)
        {
            if (index == handlers.Count) return null;

            T currentHandler = handlers[index];

            AggregateBase currentBaseHandler = currentHandler as AggregateBase;

            Func<dynamic, Task<R>> nextHandler = handlers.Aggregate<T, R>(operation, preInvokeCallback, methodName, index + 1);

            Func<dynamic, Task<R>> retVal = (dynamic prm) =>
            {
                currentBaseHandler.Context = prm;

                Task<R> operationResult;

                if (preInvokeCallback == null || preInvokeCallback(currentHandler as AggregateBase, prm, methodName))
                    operationResult = operation(currentHandler);
                else
                    operationResult = Task.FromResult(default(R));

                operationResult = operationResult.ContinueWith(t =>
                {
                    if (nextHandler != null && currentBaseHandler.Continue)
                    {
                        Task<R> t1 = nextHandler(currentBaseHandler.Context);

                        return t1.Result;
                    }

                    return t.Result;
                }, TaskContinuationOptions.AttachedToParent);

                return operationResult;
            };

            return retVal;
        }

        public static Func<dynamic, Task> Aggregate<T>(this IList<T> handlers, Func<T, Task> operation, Func<AggregateBase, dynamic, string, bool> preInvokeCallback = null, string methodName = null, int index = 0)
        {
            Func<T, Task<object>> innerOperation = f =>
            {
                operation(f);
                return Task.FromResult<object>(null);
            };

            return handlers.Aggregate(innerOperation, preInvokeCallback, methodName, index);
        }

        public static Func<dynamic, Task<R>> AggregateWithExp<T, R>(this IList<T> handlers, Expression<Func<T, Task<R>>> operation, Func<AggregateBase, dynamic, string, bool> preInvokeCallback = null, int index = 0)
        {
            MethodCallExpression mce = operation.Body as MethodCallExpression;

            if (mce != null)
            {
                string methodName = mce.Method.Name;

                object invoker = null;

                Func<T, Task<R>> func = null;

                if (invokerList.TryGetValue(methodName, out invoker))
                {
                    func = invoker as Func<T, Task<R>>;
                }
                else
                {
                    func = operation.Compile();
                    invokerList.TryAdd(methodName, func);
                }

                if (func != null)
                {
                    return handlers.Aggregate(func, preInvokeCallback, methodName, 0);
                }
            }

            return null;
        }

        public static Func<dynamic, Task> AggregateWithExp<T>(this IList<T> handlers, Expression<Func<T, Task>> operation, Func<AggregateBase, dynamic, string, bool> preInvokeCallback = null, int index = 0)
        {
            return handlers.AggregateWithExp(operation, preInvokeCallback, index);
        }
    }

    public abstract class AggregateBase
    {
        [JsonIgnore]
        public dynamic Context { get; set; }

        [JsonIgnore]
        public bool Continue { get; set; } = true;
    }
}