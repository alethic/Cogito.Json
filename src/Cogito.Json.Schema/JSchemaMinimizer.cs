using System;
using System.Collections.Generic;
using System.Linq;

using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides methods to reduce <see cref="JSchema"/>.
    /// </summary>
    public class JSchemaMinimizer
    {

        /// <summary>
        /// Default set of reductions.
        /// </summary>
        readonly static JSchemaReducer[] DefaultReductions =
            typeof(JSchemaMinimizingTransformer).Assembly.GetTypes()
                .Where(i => typeof(JSchemaReducer).IsAssignableFrom(i))
                .Where(i => i.IsAbstract == false)
                .Select(i => (JSchemaReducer)Activator.CreateInstance(i))
                .ToArray();

        readonly List<JSchemaReducer> reductions;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="reductions"></param>
        public JSchemaMinimizer(IEnumerable<JSchemaReducer> reductions)
        {
            this.reductions = new List<JSchemaReducer>(reductions);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public JSchemaMinimizer() :
            this(DefaultReductions)
        {

        }

        /// <summary>
        /// Gets the set of reductions to use when reducing.
        /// </summary>
        public ICollection<JSchemaReducer> Reductions => reductions;

        /// <summary>
        /// Reduces the given <see cref="JSchema"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public JSchema Reduce(JSchema schema)
        {
            return new JSchemaMinimizingTransformer(reductions).Transform(schema);
        }

    }

}
