using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides support for compiling expression trees implementing JSON schema validation.
    /// </summary>
    public static class JSchemaValidatorBuilder
    {

        static readonly PropertyInfo JToken_Type = typeof(JToken).GetProperty(nameof(JToken.Type));
        static readonly MethodInfo JToken_DeepEqual = typeof(JToken).GetMethod(nameof(JToken.DeepEquals));
        static readonly MethodInfo JObject_GetValue = typeof(JObject).GetMethod(nameof(JObject.GetValue), new[] { typeof(string) });

        static readonly Expression True = Expression.Constant(true);
        static readonly Expression False = Expression.Constant(true);
        static readonly Expression Null = Expression.Constant(null);

        /// <summary>
        /// Builds an expression that returns <c>true</c> if the expression is of a given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        static Expression IsType<T>(Expression e) =>
            Expression.TypeIs(e, typeof(T));

        /// <summary>
        /// Builds an expression that returns <c>true</c> if the expression returns null.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static Expression IsNull(Expression e) =>
            Expression.ReferenceEqual(e, Null);

        /// <summary>
        /// Returns an expression that returns <c>true</c> if JSON token type of the specified expression.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        static Expression TokenType(Expression o) =>
            Expression.Property(o, JToken_Type);

        /// <summary>
        /// Returns an expression that returns <c>true</c> if the specified expression of the the given token type.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        static Expression IsTokenType(Expression o, JTokenType type) =>
            Expression.Equal(TokenType(o), Expression.Constant(type));

        /// <summary>
        /// Returns an expression that returns <c>true</c> if the specified <see cref="JToken"/> expressions are completely equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static Expression DeepEqual(Expression a, Expression b) =>
            Expression.AndAlso(
                Expression.Equal(TokenType(a), TokenType(b)),
                Expression.Call(JToken_DeepEqual, a, b));

        /// <summary>
        /// Builds an expression tree that implements validation of JSON.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Expression<Func<JToken, bool>> Build(JSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var o = Expression.Parameter(typeof(JToken), "o");
            return Expression.Lambda<Func<JToken, bool>>(Build(schema, o), o);
        }

        /// <summary>
        /// Builds an expression tree that implements validation of JSON.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Expression Build(JSchema schema, Expression o)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (o == null)
                throw new ArgumentNullException(nameof(o));

            return BuildSchema(schema, o);
        }

        static Expression AndAlso(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate((Expression)Expression.Constant(true), (a, b) => Expression.AndAlso(a, b));
        }

        static Expression OrElse(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate((Expression)Expression.Constant(false), (a, b) => Expression.OrElse(a, b));
        }

        static Expression ExclusiveOr(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate((Expression)Expression.Constant(false), (a, b) => Expression.ExclusiveOr(a, b));
        }

        static Expression BuildSchema(JSchema schema, Expression o)
        {
            return AndAlso(BuildSchemaExpressions(schema, o).Where(i => i != null));
        }

        static IEnumerable<Expression> BuildSchemaExpressions(JSchema schema, Expression o)
        {
            yield return BuildAdditionalItems(schema, o);
            yield return BuildAdditionalProperties(schema, o);
            yield return BuildAllOf(schema, o);
            yield return BuildAnyOf(schema, o);
            yield return BuildConst(schema, o);
            yield return BuildContains(schema, o);
            yield return BuildDependencies(schema, o);
            yield return BuildEnum(schema, o);
            yield return BuildItems(schema, o);
            yield return BuildMaximum(schema, o);
            yield return BuildMaximumItems(schema, o);
            yield return BuildMaximumLength(schema, o);
            yield return BuildMaximumProperties(schema, o);
            yield return BuildMinimum(schema, o);
            yield return BuildMinimumItems(schema, o);
            yield return BuildMinimumLength(schema, o);
            yield return BuildMinimumProperties(schema, o);
            yield return BuildMultipleOf(schema, o);
            yield return BuildNot(schema, o);
            yield return BuildOneOf(schema, o);
            yield return BuildPattern(schema, o);
            yield return BuildPatternProperties(schema, o);
            yield return BuildProperties(schema, o);
            yield return BuildPropertyNames(schema, o);
            yield return BuildRequired(schema, o);
            yield return BuildType(schema, o);
            yield return BuildUniqueItems(schema, o);
            yield return BuildValid(schema, o);
            yield return BuildIfThenElse(schema, o);
        }

        static Expression BuildAdditionalItems(JSchema schema, Expression o)
        {
            if (schema.AdditionalItems == null && schema.AllowAdditionalItems == true)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildAdditionalProperties(JSchema schema, Expression o)
        {
            if (schema.AdditionalProperties == null && schema.AllowAdditionalProperties == true)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildAllOf(JSchema schema, Expression o)
        {
            if (schema.AllOf.Count == 0)
                return null;

            return AndAlso(schema.AllOf.Select(i => BuildSchema(i, o)));
        }

        static Expression BuildAnyOf(JSchema schema, Expression o)
        {
            if (schema.AnyOf.Count == 0)
                return null;

            return OrElse(schema.AnyOf.Select(i => BuildSchema(i, o)));
        }

        static Expression BuildConst(JSchema schema, Expression o)
        {
            if (ReferenceEquals(schema.Const, null))
                return null;

            return DeepEqual(o, Expression.Constant(schema.Const));
        }

        static Expression BuildContains(JSchema schema, Expression o)
        {
            if (schema.Contains == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildDependencies(JSchema schema, Expression o)
        {
            if (schema.Dependencies == null ||
                schema.Dependencies.Count == 0)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildEnum(JSchema schema, Expression o)
        {
            if (schema.Enum.Count == 0)
                return null;

            return OrElse(schema.Enum.Select(i => DeepEqual(o, Expression.Constant(i))));
        }

        static Expression BuildItems(JSchema schema, Expression o)
        {
            if (schema.Items == null ||
                schema.Items.Count == 0)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMaximum(JSchema schema, Expression o)
        {
            if (schema.Maximum == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMaximumItems(JSchema schema, Expression o)
        {
            if (schema.MaximumItems == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMaximumLength(JSchema schema, Expression o)
        {
            if (schema.MaximumLength == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMaximumProperties(JSchema schema, Expression o)
        {
            if (schema.MaximumProperties == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMinimum(JSchema schema, Expression o)
        {
            if (schema.Minimum == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMinimumItems(JSchema schema, Expression o)
        {
            if (schema.MinimumItems == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMinimumLength(JSchema schema, Expression o)
        {
            if (schema.MinimumLength == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMinimumProperties(JSchema schema, Expression o)
        {
            if (schema.MinimumProperties == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildMultipleOf(JSchema schema, Expression o)
        {
            if (schema.MultipleOf == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildNot(JSchema schema, Expression o)
        {
            if (schema.Not == null)
                return null;

            return Expression.Not(BuildSchema(schema.Not, o));
        }

        static Expression BuildOneOf(JSchema schema, Expression o)
        {
            if (schema.OneOf.Count == 0)
                return null;

            return schema.OneOf.Aggregate((Expression)Expression.Constant(false), (a, b) => Expression.ExclusiveOr(a, BuildSchema(b, o)));
        }

        static Expression BuildPattern(JSchema schema, Expression o)
        {
            if (schema.Pattern == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildPatternProperties(JSchema schema, Expression o)
        {
            if (schema.PatternProperties == null ||
                schema.PatternProperties.Count == 0)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildProperties(JSchema schema, Expression o)
        {
            if (schema.Properties.Count == 0)
                return null;

            return Expression.Condition(
                IsType<JObject>(o),
                AndAlso(schema.Properties.Select(i => BuildProperty(i.Key, i.Value, Expression.Convert(o, typeof(JObject))))),
                True);
        }


        static Expression BuildProperty(string propertyName, JSchema propertySchema, Expression o)
        {
            if (o.Type != typeof(JObject))
                throw new ArgumentException(nameof(o));

            var v = Expression.Call(o, JObject_GetValue, Expression.Constant(propertyName));
            return Expression.Condition(IsNull(v), True, BuildSchema(propertySchema, v));
        }

        static Expression BuildPropertyNames(JSchema schema, Expression o)
        {
            if (schema.PropertyNames == null)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildRequired(JSchema schema, Expression o)
        {
            if (schema.Required.Count == 0)
                return null;

            return Expression.Condition(
                IsType<JObject>(o),
                AndAlso(schema.Required.Select(i => BuildRequired(i, o))),
                True);
        }

        static Expression BuildRequired(string propertyName, Expression o)
        {
            return Expression.Not(IsNull(Expression.Call(o, JObject_GetValue)));
        }

        static Expression BuildType(JSchema schema, Expression o)
        {
            if (schema.Type == null)
                return null;

            switch (schema.Type)
            {
                case JSchemaType.Array:
                    return IsTokenType(o, JTokenType.Array);
                case JSchemaType.Boolean:
                    return IsTokenType(o, JTokenType.Boolean);
                case JSchemaType.Integer:
                    return IsTokenType(o, JTokenType.Integer);
                case JSchemaType.Null:
                    return IsTokenType(o, JTokenType.Null);
                case JSchemaType.Number:
                    return IsTokenType(o, JTokenType.Float);
                case JSchemaType.Object:
                    return IsTokenType(o, JTokenType.Object);
                case JSchemaType.String:
                    return IsTokenType(o, JTokenType.String);
                default:
                    throw new NotSupportedException();
            }
        }

        static Expression BuildUniqueItems(JSchema schema, Expression o)
        {
            if (schema.UniqueItems == false)
                return null;

            throw new NotImplementedException();
        }

        static Expression BuildValid(JSchema schema, Expression o)
        {
            if (schema.Valid == true)
                return True;

            if (schema.Valid == false)
                return False;

            return null;
        }

        static Expression BuildIfThenElse(JSchema schema, Expression o)
        {
            if (schema.If == null)
                return null;

            return Expression.Condition(
                BuildSchema(schema.If, o),
                schema.Then != null ? BuildSchema(schema.Then, o) : True,
                schema.Else != null ? BuildSchema(schema.Else, o) : True);
        }

    }

}
