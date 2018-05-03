using System;
using System.Collections.Generic;

using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Transformor implementation that applies a series of reductions to a <see cref="JSchema"/>.
    /// </summary>
    class JSchemaReducingTransformor :
        JSchemaTransformor
    {

        readonly IList<JSchemaReduction> reductions;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="reductions"></param>
        public JSchemaReducingTransformor(IList<JSchemaReduction> reductions)
        {
            this.reductions = reductions ?? throw new ArgumentNullException(nameof(reductions));
        }

        public override JSchema Transform(JSchema schema)
        {
            // null input
            if (schema == null)
                return null;

            // depth first
            var a = JObject.FromObject(base.Transform(schema));
            if (a == null)
                return null;

            // apply reductions
            for (int i = 0; i < reductions.Count; i++)
            {
                var r = reductions[i];
                if (r == null)
                    continue;

                // reduce current schema
                var b = JObject.FromObject(r.Reduce(JSchema.Load(a.CreateReader())));

                // resulting schema has been changed?
                if (!JTokenEquals(a, b))
                {
                    // start over using changed
                    a = b;
                    i = 0;
                }
            }

            // resulting schema has been fully reduced
            return JSchema.Load(a.CreateReader());
        }

        /// <summary>
        ///  Returns <c>true</c> if the two <see cref="JSchema"/> objects are equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool JTokenEquals(JToken a, JToken b)
        {
            return Equals(a, b) || JToken.DeepEquals(a, b);
        }

    }

}
