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

    }

}
