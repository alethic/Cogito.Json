using System.Collections.Generic;

using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    class RemoveDuplicateAnyOf : JSchemaReduction
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.AnyOf.Count > 0)
            {
                var l = new List<JSchema>();
                var h = new HashSet<JToken>(new JTokenEqualityComparer());

                foreach (var i in schema.AnyOf)
                    if (h.Add(i.ToJObject()))
                        l.Add(i);

                // number of items were changed
                if (l.Count != schema.AnyOf.Count)
                {
                    schema = schema.Clone();
                    schema.AnyOf.Clear();
                    schema.AnyOf.AddRange(l);
                }
            }

            return schema;
        }

    }

}
