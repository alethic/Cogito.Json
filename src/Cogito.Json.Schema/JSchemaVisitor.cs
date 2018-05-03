using System;
using System.Collections.Generic;
using System.Linq;

using Cogito.Collections;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides methods that visit each node in a <see cref="JSchema"/>, rewriting it as they go.
    /// </summary>
    public abstract class JSchemaVisitor
    {

        /// <summary>
        /// Visits a <see cref="JSchema"/> node. Default implementation dispatches to Visit methods for each sub-item and returns a copy of the original object.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public virtual JSchema Visit(JSchema schema)
        {
            var s = new JSchema();
            s.AdditionalItems = VisitAdditionalItems(schema, schema.AdditionalItems);
            s.AdditionalProperties = VisitAdditionalProperties(schema, schema.AdditionalProperties);
            s.AllOf.AddRange(VisitAllOf(schema, schema.AllOf));
            s.AllowAdditionalItems = VisitAllowAdditionalItems(schema, schema.AllowAdditionalItems);
            s.AllowAdditionalProperties = VisitAllowAdditionalProperties(schema, schema.AllowAdditionalProperties);
            s.AnyOf.AddRange(VisitAnyOf(schema, schema.AnyOf));
            s.Const = VisitConst(schema, schema.Const);
            s.Contains = VisitContains(schema, schema.Contains);
            s.Default = VisitDefault(schema, schema.Default);
            s.Dependencies.AddRange(VisitDependencies(schema, schema.Dependencies));
            s.Description = VisitDescription(schema, schema.Description);
            s.Enum.AddRange(VisitEnum(schema, schema.Enum));
            s.ExclusiveMaximum = VisitExclusiveMaximum(schema, schema.ExclusiveMaximum);
            s.ExclusiveMinimum = VisitExclusiveMinimum(schema, schema.ExclusiveMinimum);
            s.ExtensionData.AddRange(VisitExtensionData(schema, schema.ExtensionData));
            s.Format = VisitFormat(schema, schema.Format);
            s.Id = VisitId(schema, schema.Id);
            s.Items.AddRange(VisitItems(schema, schema.Items));
            s.ItemsPositionValidation = VisitItemsPositionValidation(schema, schema.ItemsPositionValidation);
            s.Maximum = VisitMaximum(schema, schema.Maximum);
            s.MaximumItems = VisitMaximumItems(schema, schema.MaximumItems);
            s.MaximumLength = VisitMaximumLength(schema, schema.MaximumLength);
            s.MaximumProperties = VisitMaximumProperties(schema, schema.MaximumProperties);
            s.Minimum = VisitMinimum(schema, schema.Minimum);
            s.MinimumItems = VisitMinimumItems(schema, schema.MinimumItems);
            s.MinimumLength = VisitMinimumLength(schema, schema.MinimumLength);
            s.MinimumProperties = VisitMinimumProperties(schema, schema.MinimumProperties);
            s.MultipleOf = VisitMultipleOf(schema, schema.MultipleOf);
            s.Not = VisitNot(schema, schema.Not);
            s.OneOf.AddRange(VisitOneOf(schema, schema.OneOf));
            s.Pattern = VisitPattern(schema, schema.Pattern);
            s.PatternProperties.AddRange(VisitPatternProperties(schema, schema.PatternProperties));
            s.Properties.AddRange(VisitProperties(schema, schema.Properties));
            s.PropertyNames = VisitPropertyNames(schema, schema.PropertyNames);
            s.Required.AddRange(VisitRequired(schema, schema.Required));
            s.SchemaVersion = VisitSchemaVersion(schema, schema.SchemaVersion);
            s.Title = VisitTitle(schema, schema.Title);
            s.Type = VisitType(schema, schema.Type);
            s.UniqueItems = VisitUniqueItems(schema, schema.UniqueItems);
            s.Valid = VisitValid(schema, schema.Valid);
            s.Validators.AddRange(VisitValidators(schema, schema.Validators));
            s.If = VisitIf(schema, schema.If);
            s.Then = VisitThen(schema, schema.Then);
            s.Else = VisitElse(schema, schema.Else);
            return s;
        }

        protected virtual JSchema VisitAdditionalItems(JSchema schema, JSchema additionalItems)
        {
            return additionalItems != null ? Visit(additionalItems) : null;
        }

        protected virtual JSchema VisitAdditionalProperties(JSchema schema, JSchema additionalProperties)
        {
            return additionalProperties != null ? Visit(additionalProperties) : null;
        }

        protected virtual IEnumerable<JSchema> VisitAllOf(JSchema schema, IList<JSchema> allOf)
        {
            return allOf.Select(i => VisitAllOfSchema(schema, i));
        }

        protected virtual JSchema VisitAllOfSchema(JSchema parent, JSchema allOf)
        {
            return allOf != null ? Visit(allOf) : null;
        }

        protected virtual bool VisitAllowAdditionalItems(JSchema schema, bool allowAdditionalItems)
        {
            return allowAdditionalItems;
        }

        protected virtual bool VisitAllowAdditionalProperties(JSchema schema, bool allowAdditionalProperties)
        {
            return allowAdditionalProperties;
        }

        protected virtual IEnumerable<JSchema> VisitAnyOf(JSchema schema, IList<JSchema> anyOf)
        {
            return anyOf.Select(i => VisitAnyOfSchema(schema, i));
        }

        protected virtual JSchema VisitAnyOfSchema(JSchema parent, JSchema anyOf)
        {
            return anyOf != null ? Visit(anyOf) : null;
        }

        protected virtual JToken VisitConst(JSchema schema, JToken @const)
        {
            return @const != null ? VisitToken(schema, @const) : null;
        }

        protected virtual JSchema VisitContains(JSchema schema, JSchema contains)
        {
            return contains != null ? Visit(contains) : null;
        }

        protected virtual JToken VisitDefault(JSchema schema, JToken @default)
        {
            return @default != null ? VisitToken(schema, @default) : null;
        }

        protected virtual IEnumerable<KeyValuePair<string, object>> VisitDependencies(JSchema schema, IDictionary<string, object> dependencies)
        {
            foreach (var kvp in dependencies)
            {
                (var propertyName, var value) = VisitDependency(schema, kvp.Key, kvp.Value);
                yield return new KeyValuePair<string, object>(propertyName, value);
            }
        }

        protected virtual (string PropertyName, object Dependency) VisitDependency(JSchema schema, string propertyName, object value)
        {
            switch (value)
            {
                case JSchema d:
                    return VisitSchemaDependency(schema, propertyName, d);
                case string[] d:
                    return VisitPropertyDependency(schema, propertyName, d);
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual (string PropertyName, IEnumerable<string> Dependencies) VisitPropertyDependency(JSchema schema, string propertyName, string[] dependencies)
        {
            return (propertyName, dependencies);
        }

        protected virtual (string PropertyName, JSchema Schema) VisitSchemaDependency(JSchema schema, string propertyName, JSchema dependency)
        {
            return (propertyName, dependency != null ? Visit(dependency) : null);
        }

        protected virtual string VisitDescription(JSchema schema, string description)
        {
            return description;
        }

        protected virtual IEnumerable<JToken> VisitEnum(JSchema schema, IList<JToken> @enum)
        {
            return @enum.Select(i => VisitEnumToken(schema, i));
        }

        protected virtual JToken VisitEnumToken(JSchema schema, JToken token)
        {
            return VisitToken(schema, token);
        }

        protected virtual bool VisitExclusiveMaximum(JSchema schema, bool exclusiveMaximum)
        {
            return exclusiveMaximum;
        }

        protected virtual bool VisitExclusiveMinimum(JSchema schema, bool exclusiveMinimum)
        {
            return exclusiveMinimum;
        }

        protected virtual IEnumerable<KeyValuePair<string, JToken>> VisitExtensionData(JSchema schema, IDictionary<string, JToken> extensionData)
        {
            return extensionData.Select(i => new KeyValuePair<string, JToken>(i.Key, VisitExtensionData(schema, i.Key, i.Value)));
        }

        protected virtual JToken VisitExtensionData(JSchema schema, string name, JToken data)
        {
            return data;
        }

        protected virtual string VisitFormat(JSchema schema, string format)
        {
            return format;
        }

        protected virtual Uri VisitId(JSchema schema, Uri id)
        {
            return id;
        }

        protected virtual IEnumerable<JSchema> VisitItems(JSchema schema, IList<JSchema> items)
        {
            return items.Select((i, j) => VisitItem(schema, i, j) ?? throw new JSchemaException("Cannot place null schema in items array."));
        }

        protected virtual JSchema VisitItem(JSchema schema, JSchema item, int index)
        {
            return Visit(item);
        }

        protected virtual bool VisitItemsPositionValidation(JSchema schema, bool itemsPositionValidation)
        {
            return itemsPositionValidation;
        }

        protected virtual double? VisitMaximum(JSchema schema, double? maximum)
        {
            return maximum;
        }

        protected virtual long? VisitMaximumItems(JSchema schema, long? maximumItems)
        {
            return maximumItems;
        }

        protected virtual long? VisitMaximumLength(JSchema schema, long? maximumLength)
        {
            return maximumLength;
        }

        protected virtual long? VisitMaximumProperties(JSchema schema, long? maximumProperties)
        {
            return maximumProperties;
        }

        protected virtual double? VisitMinimum(JSchema schema, double? minimum)
        {
            return minimum;
        }

        protected virtual long? VisitMinimumItems(JSchema schema, long? minimumItems)
        {
            return minimumItems;
        }

        protected virtual long? VisitMinimumLength(JSchema schema, long? minimumLength)
        {
            return minimumLength;
        }

        protected virtual long? VisitMinimumProperties(JSchema schema, long? minimumProperties)
        {
            return minimumProperties;
        }

        protected virtual double? VisitMultipleOf(JSchema schema, double? multipleOf)
        {
            return multipleOf;
        }

        protected virtual JSchema VisitNot(JSchema schema, JSchema not)
        {
            return not != null ? Visit(not) : null;
        }

        protected virtual IEnumerable<JSchema> VisitOneOf(JSchema schema, IList<JSchema> oneOf)
        {
            return oneOf.Select(i => VisitOneOfSchema(schema, i));
        }

        protected virtual JSchema VisitOneOfSchema(JSchema parent, JSchema oneOf)
        {
            return oneOf != null ? Visit(oneOf) : null;
        }

        protected virtual string VisitPattern(JSchema schema, string pattern)
        {
            return pattern;
        }

        protected virtual IEnumerable<KeyValuePair<string, JSchema>> VisitPatternProperties(JSchema schema, IDictionary<string, JSchema> patternProperties)
        {
            return patternProperties;
        }

        protected virtual IEnumerable<KeyValuePair<string, JSchema>> VisitProperties(JSchema schema, IDictionary<string, JSchema> properties)
        {
            foreach (var property in properties)
                yield return new KeyValuePair<string, JSchema>(property.Key, VisitProperty(schema, property.Key, property.Value));
        }

        protected virtual JSchema VisitProperty(JSchema schema, string name, JSchema property)
        {
            return property != null ? Visit(property) : null;
        }

        protected virtual JSchema VisitPropertyNames(JSchema schema, JSchema propertyNames)
        {
            return propertyNames != null ? Visit(propertyNames) : null;
        }

        protected virtual IEnumerable<string> VisitRequired(JSchema schema, IList<string> required)
        {
            return new List<string>(required);
        }

        protected virtual Uri VisitSchemaVersion(JSchema schema, Uri schemaVersion)
        {
            return schemaVersion;
        }

        protected virtual string VisitTitle(JSchema schema, string title)
        {
            return title;
        }

        protected virtual JSchemaType? VisitType(JSchema schema, JSchemaType? type)
        {
            return type;
        }

        protected virtual bool VisitUniqueItems(JSchema schema, bool uniqueItems)
        {
            return uniqueItems;
        }

        protected virtual bool? VisitValid(JSchema schema, bool? valid)
        {
            return valid;
        }

        protected virtual IEnumerable<JsonValidator> VisitValidators(JSchema schema, List<JsonValidator> validators)
        {
            return validators.Select(i => VisitValidator(schema, i));
        }

        protected virtual JsonValidator VisitValidator(JSchema schema, JsonValidator validator)
        {
            return validator;
        }

        protected virtual JToken VisitToken(JSchema schema, JToken token)
        {
            return token;
        }

        protected JSchema VisitIf(JSchema schema, JSchema @if)
        {
            return @if != null ? Visit(@if) : null;
        }

        protected JSchema VisitThen(JSchema schema, JSchema then)
        {
            return then != null ? Visit(then) : null;
        }

        protected JSchema VisitElse(JSchema schema, JSchema @else)
        {
            return @else != null ? Visit(@else) : null;
        }

    }

}
