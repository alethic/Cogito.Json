using System.Linq;

using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    public class RemoveDuplicateEnum : JSchemaReduction
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.Enum.Count > 0)
            {
                var l = schema.Enum.Distinct(new JTokenEqualityComparer()).ToList();
                if (l.Count != schema.Enum.Count)
                {
                    schema = schema.Clone();
                    schema.Enum.Clear();
                    schema.Enum.AddRange(l);
                }
            }

            return schema;
        }

    }

}
