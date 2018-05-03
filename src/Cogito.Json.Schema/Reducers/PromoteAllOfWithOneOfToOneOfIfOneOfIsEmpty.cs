using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    /// <summary>
    /// Schema contains no oneOf references, but does contain a single allOf reference, copy to oneOf.
    /// </summary>
    class PromoteAllOfWithOneOfToOneOfIfOneOfIsEmpty : JSchemaReducer
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.OneOf.Count == 0 &&
                schema.AllOf.Count == 1 &&
                schema.AllOf[0].OneOf.Count > 0)
            {
                if (schema.AllOf[0].ToJObject().Count == 1)
                {
                    var s = schema.Clone();
                    s.OneOf.AddRange(schema.AllOf[0].OneOf);
                    s.AllOf.Clear();
                    return s;
                }
            }

            return schema;
        }

    }

}
