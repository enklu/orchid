using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Static helper class providing utility working with the <see cref="Expression"/> class for generating runtime
    /// delegates.
    /// </summary>
    public static class ExpressionHelper
    {
        /// <summary>
        /// Wraps the expression in a type conversion expression.
        /// </summary>
        public static Expression ConvertExpression(Expression call, Type toType)
        {
            if (typeof(void) == toType)
            {
                // Just add the expression to a block, use the empty expression as the last expression
                return Expression.Block(call, Expression.Empty());
            }

            return Expression.Convert(call, toType);
        }

        /// <summary>
        /// Returns an empty catch block which has to match the return type of the try block.
        /// </summary>
        public static CatchBlock CatchBlock(ParameterExpression parameter, Expression expression, Type type)
        {
            if (type == typeof(void))
            {
                return Expression.Catch(parameter, Expression.Block(expression, Expression.Empty()));
            }

            return Expression.Catch(parameter, Expression.Block(expression, Expression.Default(type)));
        }

        /// <summary>
        /// Returns an empty catch block which has to match the return type of the try block.
        /// </summary>
        public static CatchBlock EmptyCatch<T>(Type type) where T : Exception
        {
            if (type == typeof(void))
            {
                return Expression.Catch(typeof(T), Expression.Empty());
            }

            return Expression.Catch(typeof(T), Expression.Default(type));
        }

        /// <summary>
        /// Converts delegate parameters into <see cref="ParameterExpression"/> instances to be used for
        /// creating a wrapping expression tree.
        /// </summary>
        public static ParameterExpression[] ToParameterExpressions(ParameterInfo[] parameters)
        {
            var parameterExpressions = new ParameterExpression[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameterExpressions[i] = Expression.Parameter(parameters[i].ParameterType, $"{parameters[i].Name}_{i}");
            }

            return parameterExpressions;
        }
    }
}