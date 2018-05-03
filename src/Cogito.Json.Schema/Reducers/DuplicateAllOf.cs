using System.Collections.Generic;

using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    class DuplicateAllOf : JSchemaReduction
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.AllOf.Count > 0)
            {
                var l = new List<JSchema>();
                var h = new HashSet<JToken>(new JTokenEqualityComparer());

                foreach (var i in schema.AllOf)
                    if (h.Add(JToken.FromObject(i)))
                        l.Add(i);

                // number of items were changed
                if (l.Count != schema.AllOf.Count)
                {
                    schema = JSchema.Load(JObject.FromObject(schema).CreateReader());
                    schema.AllOf.Clear();
                    schema.AllOf.AddRange(l);
                }
            }

            return schema;
        }

    }

}
