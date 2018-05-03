using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides methods to reduce <see cref="JSchema"/>.
    /// </summary>
    public class JSchemaReducer
    {

        /// <summary>
        /// Default set of reductions.
        /// </summary>
        readonly static ImmutableList<JSchemaReduction> DefaultReductions =
            typeof(JSchemaReducingVisitor).Assembly.GetTypes()
                .Where(i => typeof(JSchemaReduction).IsAssignableFrom(i))
                .Where(i => i.IsAbstract == false)
                .Select(i => (JSchemaReduction)Activator.CreateInstance(i))
                .ToImmutableList();

        readonly ImmutableList<JSchemaReduction> reductions;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="reductions"></param>
        public JSchemaReducer(IEnumerable<JSchemaReduction> reductions)
        {
            this.reductions = ImmutableList<JSchemaReduction>.Empty.AddRange(reductions);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public JSchemaReducer()
        {
            this.reductions = DefaultReductions;
        }

        /// <summary>
        /// Gets the set of reductions to use when reducing.
        /// </summary>
        public ICollection<JSchemaReduction> Reductions => reductions;

        /// <summary>
        /// Reduces the given <see cref="JSchema"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public JSchema Reduce(JSchema schema)
        {
            return new JSchemaReducingVisitor(reductions).Visit(schema);
        }

    }

}
