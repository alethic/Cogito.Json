using System;
using System.Collections.Generic;
using System.Linq;

using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides methods that transforms each node in a <see cref="JSchema"/> rewriting it as they go.
    /// </summary>
    public abstract class JSchemaTransformor
    {

        /// <summary>
        /// Transforms a <see cref="JSchema"/> node. Default implementation dispatches to Transform methods for each sub-item and returns a copy of the original object.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public virtual JSchema Transform(JSchema schema)
        {
            var s = new JSchema();
            s.AdditionalItems = TransformAdditionalItems(schema, schema.AdditionalItems);
            s.AdditionalProperties = TransformAdditionalProperties(schema, schema.AdditionalProperties);
            s.AllOf.AddRange(TransformAllOf(schema, schema.AllOf));
            s.AllowAdditionalItems = TransformAllowAdditionalItems(schema, schema.AllowAdditionalItems);
            s.AllowAdditionalProperties = TransformAllowAdditionalProperties(schema, schema.AllowAdditionalProperties);
            s.AnyOf.AddRange(TransformAnyOf(schema, schema.AnyOf));
            s.Const = TransformConst(schema, schema.Const);
            s.Contains = TransformContains(schema, schema.Contains);
            s.Default = TransformDefault(schema, schema.Default);
            s.Dependencies.AddRange(TransformDependencies(schema, schema.Dependencies));
            s.Description = TransformDescription(schema, schema.Description);
            s.Enum.AddRange(TransformEnum(schema, schema.Enum));
            s.ExclusiveMaximum = TransformExclusiveMaximum(schema, schema.ExclusiveMaximum);
            s.ExclusiveMinimum = TransformExclusiveMinimum(schema, schema.ExclusiveMinimum);
            s.ExtensionData.AddRange(TransformExtensionData(schema, schema.ExtensionData));
            s.Format = TransformFormat(schema, schema.Format);
            s.Id = TransformId(schema, schema.Id);
            s.Items.AddRange(TransformItems(schema, schema.Items));
            s.ItemsPositionValidation = TransformItemsPositionValidation(schema, schema.ItemsPositionValidation);
            s.Maximum = TransformMaximum(schema, schema.Maximum);
            s.MaximumItems = TransformMaximumItems(schema, schema.MaximumItems);
            s.MaximumLength = TransformMaximumLength(schema, schema.MaximumLength);
            s.MaximumProperties = TransformMaximumProperties(schema, schema.MaximumProperties);
            s.Minimum = TransformMinimum(schema, schema.Minimum);
            s.MinimumItems = TransformMinimumItems(schema, schema.MinimumItems);
            s.MinimumLength = TransformMinimumLength(schema, schema.MinimumLength);
            s.MinimumProperties = TransformMinimumProperties(schema, schema.MinimumProperties);
            s.MultipleOf = TransformMultipleOf(schema, schema.MultipleOf);
            s.Not = TransformNot(schema, schema.Not);
            s.OneOf.AddRange(TransformOneOf(schema, schema.OneOf));
            s.Pattern = TransformPattern(schema, schema.Pattern);
            s.PatternProperties.AddRange(TransformPatternProperties(schema, schema.PatternProperties));
            s.Properties.AddRange(TransformProperties(schema, schema.Properties));
            s.PropertyNames = TransformPropertyNames(schema, schema.PropertyNames);
            s.Required.AddRange(TransformRequired(schema, schema.Required));
            s.SchemaVersion = TransformSchemaVersion(schema, schema.SchemaVersion);
            s.Title = TransformTitle(schema, schema.Title);
            s.Type = TransformType(schema, schema.Type);
            s.UniqueItems = TransformUniqueItems(schema, schema.UniqueItems);
            s.Valid = TransformValid(schema, schema.Valid);
            s.Validators.AddRange(TransformValidators(schema, schema.Validators));
            s.If = TransformIf(schema, schema.If);
            s.Then = TransformThen(schema, schema.Then);
            s.Else = TransformElse(schema, schema.Else);
            return s;
        }

        protected virtual JSchema TransformAdditionalItems(JSchema schema, JSchema additionalItems)
        {
            return additionalItems != null ? Transform(additionalItems) : null;
        }

        protected virtual JSchema TransformAdditionalProperties(JSchema schema, JSchema additionalProperties)
        {
            return additionalProperties != null ? Transform(additionalProperties) : null;
        }

        protected virtual IEnumerable<JSchema> TransformAllOf(JSchema schema, IList<JSchema> allOf)
        {
            return allOf.Select(i => TransformAllOfSchema(schema, i));
        }

        protected virtual JSchema TransformAllOfSchema(JSchema parent, JSchema allOf)
        {
            return allOf != null ? Transform(allOf) : null;
        }

        protected virtual bool TransformAllowAdditionalItems(JSchema schema, bool allowAdditionalItems)
        {
            return allowAdditionalItems;
        }

        protected virtual bool TransformAllowAdditionalProperties(JSchema schema, bool allowAdditionalProperties)
        {
            return allowAdditionalProperties;
        }

        protected virtual IEnumerable<JSchema> TransformAnyOf(JSchema schema, IList<JSchema> anyOf)
        {
            return anyOf.Select(i => TransformAnyOfSchema(schema, i));
        }

        protected virtual JSchema TransformAnyOfSchema(JSchema parent, JSchema anyOf)
        {
            return anyOf != null ? Transform(anyOf) : null;
        }

        protected virtual JToken TransformConst(JSchema schema, JToken @const)
        {
            return @const != null ? TransformToken(schema, @const) : null;
        }

        protected virtual JSchema TransformContains(JSchema schema, JSchema contains)
        {
            return contains != null ? Transform(contains) : null;
        }

        protected virtual JToken TransformDefault(JSchema schema, JToken @default)
        {
            return @default != null ? TransformToken(schema, @default) : null;
        }

        protected virtual IEnumerable<KeyValuePair<string, object>> TransformDependencies(JSchema schema, IDictionary<string, object> dependencies)
        {
            foreach (var kvp in dependencies)
            {
                (var propertyName, var value) = TransformDependency(schema, kvp.Key, kvp.Value);
                yield return new KeyValuePair<string, object>(propertyName, value);
            }
        }

        protected virtual (string PropertyName, object Dependency) TransformDependency(JSchema schema, string propertyName, object value)
        {
            switch (value)
            {
                case JSchema d:
                    return TransformSchemaDependency(schema, propertyName, d);
                case string[] d:
                    return TransformPropertyDependency(schema, propertyName, d);
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual (string PropertyName, IEnumerable<string> Dependencies) TransformPropertyDependency(JSchema schema, string propertyName, string[] dependencies)
        {
            return (propertyName, dependencies);
        }

        protected virtual (string PropertyName, JSchema Schema) TransformSchemaDependency(JSchema schema, string propertyName, JSchema dependency)
        {
            return (propertyName, dependency != null ? Transform(dependency) : null);
        }

        protected virtual string TransformDescription(JSchema schema, string description)
        {
            return description;
        }

        protected virtual IEnumerable<JToken> TransformEnum(JSchema schema, IList<JToken> @enum)
        {
            return @enum.Select(i => TransformEnumToken(schema, i));
        }

        protected virtual JToken TransformEnumToken(JSchema schema, JToken token)
        {
            return TransformToken(schema, token);
        }

        protected virtual bool TransformExclusiveMaximum(JSchema schema, bool exclusiveMaximum)
        {
            return exclusiveMaximum;
        }

        protected virtual bool TransformExclusiveMinimum(JSchema schema, bool exclusiveMinimum)
        {
            return exclusiveMinimum;
        }

        protected virtual IEnumerable<KeyValuePair<string, JToken>> TransformExtensionData(JSchema schema, IDictionary<string, JToken> extensionData)
        {
            return extensionData.Select(i => new KeyValuePair<string, JToken>(i.Key, TransformExtensionData(schema, i.Key, i.Value)));
        }

        protected virtual JToken TransformExtensionData(JSchema schema, string name, JToken data)
        {
            return data;
        }

        protected virtual string TransformFormat(JSchema schema, string format)
        {
            return format;
        }

        protected virtual Uri TransformId(JSchema schema, Uri id)
        {
            return id;
        }

        protected virtual IEnumerable<JSchema> TransformItems(JSchema schema, IList<JSchema> items)
        {
            return items.Select((i, j) => TransformItem(schema, i, j) ?? throw new JSchemaException("Cannot place null schema in items array."));
        }

        protected virtual JSchema TransformItem(JSchema schema, JSchema item, int index)
        {
            return Transform(item);
        }

        protected virtual bool TransformItemsPositionValidation(JSchema schema, bool itemsPositionValidation)
        {
            return itemsPositionValidation;
        }

        protected virtual double? TransformMaximum(JSchema schema, double? maximum)
        {
            return maximum;
        }

        protected virtual long? TransformMaximumItems(JSchema schema, long? maximumItems)
        {
            return maximumItems;
        }

        protected virtual long? TransformMaximumLength(JSchema schema, long? maximumLength)
        {
            return maximumLength;
        }

        protected virtual long? TransformMaximumProperties(JSchema schema, long? maximumProperties)
        {
            return maximumProperties;
        }

        protected virtual double? TransformMinimum(JSchema schema, double? minimum)
        {
            return minimum;
        }

        protected virtual long? TransformMinimumItems(JSchema schema, long? minimumItems)
        {
            return minimumItems;
        }

        protected virtual long? TransformMinimumLength(JSchema schema, long? minimumLength)
        {
            return minimumLength;
        }

        protected virtual long? TransformMinimumProperties(JSchema schema, long? minimumProperties)
        {
            return minimumProperties;
        }

        protected virtual double? TransformMultipleOf(JSchema schema, double? multipleOf)
        {
            return multipleOf;
        }

        protected virtual JSchema TransformNot(JSchema schema, JSchema not)
        {
            return not != null ? Transform(not) : null;
        }

        protected virtual IEnumerable<JSchema> TransformOneOf(JSchema schema, IList<JSchema> oneOf)
        {
            return oneOf.Select(i => TransformOneOfSchema(schema, i));
        }

        protected virtual JSchema TransformOneOfSchema(JSchema parent, JSchema oneOf)
        {
            return oneOf != null ? Transform(oneOf) : null;
        }

        protected virtual string TransformPattern(JSchema schema, string pattern)
        {
            return pattern;
        }

        protected virtual IEnumerable<KeyValuePair<string, JSchema>> TransformPatternProperties(JSchema schema, IDictionary<string, JSchema> patternProperties)
        {
            return patternProperties;
        }

        protected virtual IEnumerable<KeyValuePair<string, JSchema>> TransformProperties(JSchema schema, IDictionary<string, JSchema> properties)
        {
            foreach (var property in properties)
                yield return new KeyValuePair<string, JSchema>(property.Key, TransformProperty(schema, property.Key, property.Value));
        }

        protected virtual JSchema TransformProperty(JSchema schema, string name, JSchema property)
        {
            return property != null ? Transform(property) : null;
        }

        protected virtual JSchema TransformPropertyNames(JSchema schema, JSchema propertyNames)
        {
            return propertyNames != null ? Transform(propertyNames) : null;
        }

        protected virtual IEnumerable<string> TransformRequired(JSchema schema, IList<string> required)
        {
            return new List<string>(required);
        }

        protected virtual Uri TransformSchemaVersion(JSchema schema, Uri schemaVersion)
        {
            return schemaVersion;
        }

        protected virtual string TransformTitle(JSchema schema, string title)
        {
            return title;
        }

        protected virtual JSchemaType? TransformType(JSchema schema, JSchemaType? type)
        {
            return type;
        }

        protected virtual bool TransformUniqueItems(JSchema schema, bool uniqueItems)
        {
            return uniqueItems;
        }

        protected virtual bool? TransformValid(JSchema schema, bool? valid)
        {
            return valid;
        }

        protected virtual IEnumerable<JsonValidator> TransformValidators(JSchema schema, List<JsonValidator> validators)
        {
            return validators.Select(i => TransformValidator(schema, i));
        }

        protected virtual JsonValidator TransformValidator(JSchema schema, JsonValidator validator)
        {
            return validator;
        }

        protected virtual JToken TransformToken(JSchema schema, JToken token)
        {
            return token;
        }

        protected JSchema TransformIf(JSchema schema, JSchema @if)
        {
            return @if != null ? Transform(@if) : null;
        }

        protected JSchema TransformThen(JSchema schema, JSchema then)
        {
            return then != null ? Transform(then) : null;
        }

        protected JSchema TransformElse(JSchema schema, JSchema @else)
        {
            return @else != null ? Transform(@else) : null;
        }

    }

}
