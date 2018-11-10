using System.Linq.Expressions;
using System.Reflection;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Validation
{

    public abstract class ExpressionProviderBase : IJSchemaExpressionProvider
    {

        public abstract Expression Build(JSchema schema, Expression token);

        protected static readonly Expression True = Expression.Constant(true);
        protected static readonly Expression False = Expression.Constant(false);
        protected static readonly Expression Null = Expression.Constant(null);

        /// <summary>
        /// Returns an expression that calls the given method on this class.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected Expression CallThis(string methodName, params Expression[] args) =>
            Expression.Call(
                GetType().GetMethod(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                args);

        /// <summary>
        /// Returns an expression that returns <c>true</c> if JSON token type of the specified expression.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        protected static Expression TokenType(Expression o) =>
            Expression.Property(o, typeof(JToken).GetProperty(nameof(JToken.Type)));

        /// <summary>
        /// Returns an expression that returns <c>true</c> if the specified expression of the the given token type.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected static Expression IsTokenType(Expression o, JTokenType type) =>
            Expression.Equal(TokenType(o), Expression.Constant(type));

        /// <summary>
        /// Returns an expression that returns <c>true</c> if the given test is false, else executes the <paramref name="ifTrue"/> condition.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="ifTrue"></param>
        /// <returns></returns>
        protected static Expression IfThenElseTrue(Expression test, Expression ifTrue) =>
            Expression.Condition(test, ifTrue, True);

    }

}
