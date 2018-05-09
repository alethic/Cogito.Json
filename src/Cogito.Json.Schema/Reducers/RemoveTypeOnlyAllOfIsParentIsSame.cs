using System.Linq;

using Cogito.Collections;
using Cogito.Json.Schema;
using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Schema;

namespace FileAndServe.Efm.Components.Schema.Reducers
{

    class RemoveTypeOnlyAllOfIsParentIsSame : JSchemaReducer
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.Type != null &&
                schema.AllOf.Count > 0)
            {
                var l = schema.AllOf.Except(schema.AllOf.Where(i => i.Type == schema.Type && i.ToJObject().Count == 1)).ToList();
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
