﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Xpand.Utils.Linq{
    public static class Extensions{
//        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int takeCount){
//            if (source == null) throw new ArgumentNullException(nameof(source));
//            if (takeCount < 0) throw new ArgumentOutOfRangeException(nameof(takeCount), "must not be negative");
//            if (takeCount == 0) yield break;
//
//            var result = new T[takeCount];
//            var i = 0;
//
//            var sourceCount = 0;
//            foreach (var element in source){
//                result[i] = element;
//                i = (i + 1) % takeCount;
//                sourceCount++;
//            }
//
//            if (sourceCount < takeCount){
//                takeCount = sourceCount;
//                i = 0;
//            }
//
//            for (var j = 0; j < takeCount; ++j) yield return result[(i + j) % takeCount];
//        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts) {
            int i = 0;
            return list.GroupBy(item => i++%parts).Select(part => part.AsEnumerable());
        }

        public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n) {
            using (var it = source.GetEnumerator()){
                bool hasRemainingItems;
                var cache = new Queue<T>(n + 1);

                do{
                    var b = hasRemainingItems = it.MoveNext();
                    if (b) {
                        cache.Enqueue(it.Current);
                        if (cache.Count > n)
                            yield return cache.Dequeue();
                    }
                } while (hasRemainingItems);
            }
        }
        public static IEnumerable<TResult> SelectNonNull<T, TResult>(this IEnumerable<T> sequence,
            Func<T, TResult> projection) where TResult : class{
            return sequence.Select(projection).Where(e => e != null);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> sequence) where T : class{
            return sequence.Where(e => e != null);
        }

        public static TResult NullSafeEval<TSource, TResult>(this TSource source,
            Expression<Func<TSource, TResult>> expression, TResult defaultValue){
            Expression<Func<TSource, TResult>> safeExp = Expression.Lambda<Func<TSource, TResult>>(
                NullSafeEvalWrapper(expression.Body, Expression.Constant(defaultValue)),
                expression.Parameters[0]);

            Func<TSource, TResult> safeDelegate = safeExp.Compile();
            return safeDelegate(source);
        }

        private static Expression NullSafeEvalWrapper(Expression expr, Expression defaultValue){
            Expression safe = expr;

            while (!IsNullSafe(expr, out var obj)){
                BinaryExpression isNull = Expression.Equal(obj, Expression.Constant(null));

                safe =
                    Expression.Condition
                        (
                            isNull,
                            defaultValue,
                            safe
                        );

                expr = obj;
            }
            return safe;
        }

        private static bool IsNullSafe(Expression expr, out Expression nullableObject){
            nullableObject = null;

            if (expr is MemberExpression || expr is MethodCallExpression){
                Expression obj;
                var memberExpr = expr as MemberExpression;
                var callExpr = expr as MethodCallExpression;

                if (memberExpr != null){
                    // Static fields don't require an instance
                    var field = memberExpr.Member as FieldInfo;
                    if (field != null && field.IsStatic)
                        return true;

                    // Static properties don't require an instance
                    var property = memberExpr.Member as PropertyInfo;
                    MethodInfo getter = property?.GetGetMethod();
                    if (getter != null && getter.IsStatic)
                        return true;
                    obj = memberExpr.Expression;
                }
                else{
                    // Static methods don't require an instance
                    if (callExpr.Method.IsStatic)
                        return true;

                    obj = callExpr.Object;
                }

                // Value types can't be null
                if (obj != null && obj.Type.IsValueType)
                    return true;

                // Instance member access or instance method call is not safe
                nullableObject = obj;
                return false;
            }
            return true;
        }

        public static void ThrowIfNull<T>(this T container) where T : class {
            if (container == null) {
                throw new ArgumentNullException(nameof(container));
            }
            NullChecker<T>.Check(container);
        }

        private static class NullChecker<T> where T : class {
            private static readonly List<Func<T, bool>> Checkers;
            private static readonly List<string> Names;

            static NullChecker() {
                Checkers = new List<Func<T, bool>>();
                Names = new List<string>();
                // We can't rely on the order of the properties, but we 
                // can rely on the order of the constructor parameters 
                // in an anonymous type - and that there'll only be 
                // one constructor. 
                var constructorInfo = typeof(T).GetConstructors().FirstOrDefault();
                if (constructorInfo != null)
                    foreach (string name in constructorInfo.GetParameters().Select(p => p.Name)) {
                        Names.Add(name);
                        PropertyInfo property = typeof(T).GetProperty(name);
                        // I've omitted a lot of error checking, but here's 
                        // at least one bit... 
                        if (property != null && property.PropertyType.IsValueType) {
                            throw new ArgumentException
                                ("Property " + property + " is a value type");
                        }
                        ParameterExpression param = Expression.Parameter(typeof(T), "container");
                        Expression propertyAccess = Expression.Property(param, property ?? throw new InvalidOperationException());
                        Expression nullValue = Expression.Constant(null, property.PropertyType);
                        Expression equality = Expression.Equal(propertyAccess, nullValue);
                        var lambda = Expression.Lambda<Func<T, bool>>(equality, param);
                        Checkers.Add(lambda.Compile());
                    }
            }

            internal static void Check(T item) {
                for (int i = 0; i < Checkers.Count; i++) {
                    if (Checkers[i](item)) {
                        throw new ArgumentNullException(Names[i]);
                    }
                }
            }
        } 
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence)
            where T : struct{
            return sequence.Where(e => e != null).Select(e => e.Value);
        }

    }
}