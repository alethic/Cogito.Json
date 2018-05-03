using System.Linq;

using Cogito.Collections;

using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    class RemoveEmptySchemaFromAllOf : JSchemaReducer
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.AllOf.Count > 1)
            {
                var l = schema.AllOf.Where(i => i.ToJObject().Count != 0).ToList();
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
