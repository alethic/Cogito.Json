using System.Linq;

using Cogito.Collections;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    class RemoveEmptySchemaFromAllOf : JSchemaReducer
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.AllOf.Count > 0)
            {
                var r = schema.AllOf.Where(i => i.Valid == true || i.AsJToken() is JObject o && o.Count == 0);
                var l = schema.AllOf.Except(r).ToList();
                if (l.Count != schema.AllOf.Count)
                {
                    schema = schema.Clone();
                    schema.AllOf.Clear();
                    schema.AllOf.AddRange(l);
                }
            }

            return schema;
        }

    }

}
