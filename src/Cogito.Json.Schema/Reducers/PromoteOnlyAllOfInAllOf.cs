﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cogito.Collections;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    /// <summary>
    /// Promotes allOfs inside schemas inside of allOf to the parent.
    /// </summary>
    class PromoteOnlyAllOfInAllOf : JSchemaReduction
    {

        public override JSchema Reduce(JSchema schema)
        {
            // parent schema has allOf, and contains at least one child schema that itself has an allOf
            if (schema.AllOf.Count > 0 &&
                schema.AllOf.Any(i => i.AllOf.Count > 0))
            {
                // create local copy for modification
                schema = schema.Clone();

                // allOf collection to modify
                var l = new List<JSchema>(schema.AllOf);

                foreach (var s in schema.AllOf)
                {
                    // nested schema has allOf and nothing else
                    if (s.AllOf.Count > 0 &&
                        JObject.FromObject(s).Count == 1)
                    {
                        l.AddRange(s.AllOf);
                        l.Remove(s);
                    }
                }

                // replace with modified copy
                schema.AllOf.Clear();
                schema.AllOf.AddRange(l);
            }

            return schema;
        }

    }

}
