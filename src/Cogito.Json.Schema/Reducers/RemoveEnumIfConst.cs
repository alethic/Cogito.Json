using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema.Reducers
{

    /// <summary>
    /// If const is specified, and enum contains const, then only possible value is const; remove enum
    /// </summary>
    class RemoveEnumIfConst : JSchemaReduction
    {

        public override JSchema Reduce(JSchema schema)
        {
            if (schema.Const != null &&
                schema.Enum.Count > 1 &&
                schema.Enum.Contains(schema.Const))
            {
                schema = schema.Clone();
                schema.Enum.Clear();
            }

            return schema;
        }

    }

}
