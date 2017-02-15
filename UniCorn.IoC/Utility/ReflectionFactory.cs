using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace UniCorn.IoC
{
    public static class ReflectionFactory
    {
        public static Func<object[], object> GetAnonymousInstantiator(Type type)
        {
            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor == null) return null;

            var paramExpr = Expression.Parameter(typeof(object[]));

            return Expression.Lambda<Func<object[], object>>
            (
                Expression.New
                (
                    ctor,
                    ctor.GetParameters().Select
                    (
                        (x, i) => Expression.Convert
                        (
                            Expression.ArrayIndex(paramExpr, Expression.Constant(i)),
                            x.ParameterType
                        )
                    )
                ), paramExpr).Compile();
        }

        public static LambdaExpression GetAnonymousInstantiatorLambda(Type type)
        {
            var ctor = type.GetConstructors().FirstOrDefault();

            if (ctor == null) return null;

            ParameterInfo[] parameterList = ctor.GetParameters();

            List<ParameterExpression> parameterExpList = new List<ParameterExpression>();

            foreach (ParameterInfo prmInfo in parameterList)
            {
                parameterExpList.Add(Expression.Parameter(prmInfo.ParameterType));
            }

            return Expression.Lambda
            (
                Expression.New
                (
                    ctor, parameterExpList
                ),
                parameterExpList
            );
        }
    }
}