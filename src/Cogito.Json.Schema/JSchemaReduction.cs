using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    /// <summary>
    /// Implementation of a particular reduction algorithm.
    /// </summary>
    public abstract class JSchemaReduction
    {

        /// <summary>
        /// Produces a reduction of the given <see cref="JSchema"/>, if possible.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public abstract JSchema Reduce(JSchema schema);

    }

}
