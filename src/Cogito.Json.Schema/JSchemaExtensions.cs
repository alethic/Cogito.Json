using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    public static class JSchemaExtensions
    {

        /// <summary>
        /// Creates a copy of the given <see cref="JSchema"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static JSchema Clone(this JSchema schema)
        {
            return schema != null ? JSchema.Load(JObject.FromObject(schema).CreateReader()) : null;
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
