using System.Collections.Generic;

using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    public static class JSchemaExtensions
    {

        static readonly JSchemaCopyTransformer copy = new JSchemaCopyTransformer();

        /// <summary>
        /// Creates a copy of the given <see cref="JSchema"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JSchema Clone(this JSchema schema)
        {
            return schema != null ? copy.Transform(schema) : null;
        }

        /// <summary>
        /// Minimizes the JSON schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JSchema Minimize(this JSchema schema)
        {
            return schema != null ? new JSchemaMinimizer().Minimize(schema) : null;
        }

        /// <summary>
        /// Minimizes the JSON schema using the specified reducers.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="reducers"></param>
        /// <returns></returns>
        public static JSchema Minimize(this JSchema schema, IEnumerable<JSchemaReducer> reducers)
        {
            return schema != null ? new JSchemaMinimizer(reducers).Minimize(schema) : null;
        }

        /// <summary>
        /// Converts a <see cref="JSchema"/> to a <see cref="JObject"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JToken ToJToken(this JSchema schema)
        {
            return schema != null ? JToken.FromObject(schema) : null;
        }

        /// <summary>
        /// Converts a <see cref="JSchema"/> to a <see cref="JObject"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JObject ToJObject(this JSchema schema)
        {
            return schema != null ? JObject.FromObject(schema) : null;
        }

    }

}
