using System.Linq.Expressions;

using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Validation
{

    /// <summary>
    /// Provides <see cref="Expression"/> instances that contribute to validation.
    /// </summary>
    public interface IJSchemaExpressionProvider
    {

        /// <summary>
        /// Builds and returns an <see cref="Expression"/> that should be a condition of validity of the schema
        /// against the specified token.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Expression Build(JSchema schema, Expression token);

    }

}