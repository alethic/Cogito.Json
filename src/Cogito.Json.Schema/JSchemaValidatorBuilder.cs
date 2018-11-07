﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Cogito.Json.Schema.Internal;

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
        static readonly Expression False = Expression.Constant(false);
        static readonly Expression Null = Expression.Constant(null);

        static Expression Reduce(Expression e)
        {
            e = e.Reduce();

            {
                if (e.NodeType == ExpressionType.AndAlso && e is BinaryExpression u)
                {
                    if (u.Left.NodeType == ExpressionType.Constant && u.Left is ConstantExpression l)
                    {
                        if (l.Value is bool b1 && b1)
                            return Reduce(u.Right);

                        if (l.Value is bool b2 && !b2)
                            return False;
                    }

                    if (u.Right.NodeType == ExpressionType.Constant && u.Right is ConstantExpression r)
                    {
                        if (r.Value is bool b1 && b1)
                            return Reduce(u.Left);

                        if (r.Value is bool b2 && !b2)
                            return False;
                    }
                }
            }

            {
                if (e.NodeType == ExpressionType.OrElse && e is BinaryExpression u)
                {
                    if (u.Left.NodeType == ExpressionType.Constant && u.Left is ConstantExpression l)
                    {
                        if (l.Value is bool b1 && b1)
                            return True;

                        if (l.Value is bool b2 && !b2)
                            return Reduce(u.Right);
                    }

                    if (u.Right.NodeType == ExpressionType.Constant && u.Right is ConstantExpression r)
                    {
                        if (r.Value is bool b1 && b1)
                            return True;

                        if (r.Value is bool b2 && !b2)
                            return Reduce(u.Left);
                    }
                }
            }

            if (e.NodeType == ExpressionType.Negate && e is UnaryExpression n)
            {
                // nested negation, flip
                if (n.Operand.NodeType == ExpressionType.Negate && n.Operand is UnaryExpression n2)
                    return Reduce(n2);

                // nested constant boolean, flip
                if (n.Operand.NodeType == ExpressionType.Constant && n.Operand is ConstantExpression c)
                    return c.Value is bool b && b ? False : True;
            }

            return e;
        }

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
        /// Returns an expression that returns the item at the specified index.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        static Expression FromItemIndex(Expression o, int index) =>
            FromItemIndex(o, Expression.Constant(index));

        /// <summary>
        /// Returns an expression that returns the item at the specified index.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        static Expression FromItemIndex(Expression o, Expression index) =>
            Expression.Property(o, "Item", index);


        /// <summary>
        /// Returns an expression that calls the given method on this class.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static Expression CallThis(string methodName, params Expression[] args) =>
            Expression.Call(
                typeof(JSchemaValidatorBuilder).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
                args);

        /// <summary>
        /// Returns an expression that gets the length of a string in text elements.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        static Expression StringLength(Expression o) =>
            CallThis(nameof(StringLengthMethod), Expression.Convert(o, typeof(string)));

        /// <summary>
        /// Gets the string length.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static int StringLengthMethod(string value) =>
            new StringInfo(value).LengthInTextElements;

        /// <summary>
        /// Returns a <see cref="JSchemaType"/> enum that indicates the covered types of the <see cref="JTokenType"/>.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static JSchemaType SchemaTypeForTokenType(JTokenType t)
        {
            var s = JSchemaType.None;

            if (t == JTokenType.Array)
                s |= JSchemaType.Array;

            if (t == JTokenType.Boolean)
                s |= JSchemaType.Boolean;

            if (t == JTokenType.Integer)
                s |= JSchemaType.Integer | JSchemaType.Number;

            if (t == JTokenType.Null)
                s |= JSchemaType.Null;

            if (t == JTokenType.Float)
                s |= JSchemaType.Number;

            if (t == JTokenType.Object)
                s |= JSchemaType.Object;

            if (t == JTokenType.String)
                s |= JSchemaType.String;

            return s;
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c> if the specified expression is compatible with the given schema type.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        static Expression IsSchemaType(Expression o, JSchemaType t) =>
            Expression.NotEqual(
                Expression.And(
                    Expression.Constant((int)t),
                    Expression.Convert(
                        CallThis(nameof(SchemaTypeForTokenType), TokenType(o)),
                        typeof(int))),
                Expression.Constant(0));

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
            var e = Reduce(Build(schema, o));
            return Expression.Lambda<Func<JToken, bool>>(e, o);
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

        static Expression OnlyWhen(Expression @if, Expression then)
        {
            return Expression.Condition(@if, then, True);
        }

        static Expression AllOf(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate(True, (a, b) => Reduce(Expression.AndAlso(a, b)));
        }

        static Expression AnyOf(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate(False, (a, b) => Reduce(Expression.OrElse(a, b)));
        }

        static Expression OneOf(IEnumerable<Expression> expressions)
        {
            var rsl = Expression.Variable(typeof(bool));
            var brk = Expression.Label(typeof(bool));

            return Expression.Block(
                new[] { rsl },
                Expression.Block(
                    expressions.Select(i =>
                        Expression.IfThen(i,
                            Expression.IfThenElse(rsl,
                                Expression.Return(brk, False),
                                Expression.Assign(rsl, True))))),
                Expression.Label(brk, rsl));
        }

        static Expression BuildSchema(JSchema schema, Expression o)
        {
            return AllOf(BuildSchemaExpressions(schema, o).Where(i => i != null).Select(i => Reduce(i)));
        }

        static IEnumerable<Expression> BuildSchemaExpressions(JSchema schema, Expression o)
        {
            yield return BuildAdditionalProperties(schema, o);
            yield return BuildAllOf(schema, o);
            yield return BuildAnyOf(schema, o);
            yield return BuildConst(schema, o);
            yield return BuildContains(schema, o);
            yield return BuildContentEncoding(schema, o);
            yield return BuildDependencies(schema, o);
            yield return BuildEnum(schema, o);
            yield return BuildFormat(schema, o);
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

            return AllOf(schema.AllOf.Select(i => BuildSchema(i, o)));
        }

        static Expression BuildAnyOf(JSchema schema, Expression o)
        {
            if (schema.AnyOf.Count == 0)
                return null;

            return AnyOf(schema.AnyOf.Select(i => BuildSchema(i, o)));
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

            var val = Expression.Convert(o, typeof(JArray));
            var idx = Expression.Variable(typeof(int));
            var brk = Expression.Label(typeof(bool));
            var len = Expression.Property(val, nameof(JArray.Count));

            return OnlyWhen(
                IsTokenType(o, JTokenType.Array),
                Expression.Block(
                    new[] { idx },
                    Expression.Assign(idx, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Not(Expression.LessThan(idx, len)),
                            Expression.Break(brk, False),
                            Expression.IfThenElse(
                                BuildSchema(schema.Contains, FromItemIndex(val, idx)),
                                Expression.Break(brk, True),
                                Expression.PostIncrementAssign(idx))),
                        brk)));
        }

        static Expression BuildContentEncoding(JSchema schema, Expression o)
        {
            if (schema.ContentEncoding == null)
                return null;

            return CallThis(nameof(IsBase64String), Expression.Convert(o, typeof(string)));
        }

        static bool IsBase64String(string value) =>
            StringHelpers.IsBase64String(value);

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

            return AnyOf(schema.Enum.Select(i => DeepEqual(o, Expression.Constant(i))));
        }

        static Expression BuildFormat(JSchema schema, Expression o)
        {
            if (schema.Format == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.String),
                ValidateFormat(schema.Format, Expression.Convert(o, typeof(string))));
        }

        static Expression ValidateFormat(string format, Expression o)
        {
            switch (format)
            {
                case Constants.Formats.Color:
                    return CallThis(nameof(ValidateColor), o);
                case Constants.Formats.Hostname:
                case Constants.Formats.Draft3Hostname:
                    return CallThis(nameof(ValidateHostname), o);
                case Constants.Formats.IPv4:
                case Constants.Formats.Draft3IPv4:
                    return CallThis(nameof(ValidateIPv4), o);
                case Constants.Formats.IPv6:
                    return CallThis(nameof(ValidateIPv6), o);
                case Constants.Formats.Email:
                    return CallThis(nameof(ValidateEmail), o);
                case Constants.Formats.Uri:
                    return CallThis(nameof(ValidateUri), o);
                case Constants.Formats.UriReference:
                    return CallThis(nameof(ValidateUriReference), o);
                case Constants.Formats.UriTemplate:
                    return CallThis(nameof(ValidateUriTemplate), o);
                case Constants.Formats.JsonPointer:
                    return CallThis(nameof(ValidateJsonPointer), o);
                case Constants.Formats.Date:
                    return CallThis(nameof(ValidateDate), o);
                case Constants.Formats.Time:
                    return CallThis(nameof(ValidateTime), o);
                case Constants.Formats.DateTime:
                    return CallThis(nameof(ValidateDateTime), o);
                case Constants.Formats.UtcMilliseconds:
                    return CallThis(nameof(ValidateUtcMilliseconds), o);
                case Constants.Formats.Regex:
                    return CallThis(nameof(ValidateRegex), o);
                default:
                    return True;
            }
        }

        static bool ValidateEmail(string value) =>
            EmailHelpers.Validate(value, true);

        static bool ValidateUri(string value) =>
            Uri.IsWellFormedUriString(value, UriKind.Absolute);

        static bool ValidateUriReference(string value) =>
            FormatHelpers.ValidateUriReference(value);

        static bool ValidateUriTemplate(string value) =>
            FormatHelpers.ValidateUriTemplate(value);

        static bool ValidateJsonPointer(string value) =>
            FormatHelpers.ValidateJsonPointer(value);

        static bool ValidateDate(string value) =>
            DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateTime(string value) =>
            DateTime.TryParseExact(value, "HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateDateTime(string value) =>
            DateTime.TryParseExact(value, @"yyyy-MM-dd\THH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateUtcMilliseconds(string value) =>
            double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var _);

        static bool ValidateRegex(string value)
        {
            try
            {
                new Regex(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool ValidateHostname(string value)
        {
            return HostnameRegex.IsMatch(value);
        }

        static readonly Regex HostnameRegex =
            new Regex(@"^(?=.{1,255}$)[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?(?:\.[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?)*\.?$",
                RegexOptions.CultureInvariant | RegexOptions.Compiled);

        static bool ValidateColor(string value) => ColorHelpers.IsValid(value);

        static bool ValidateIPv6(string value) => Uri.CheckHostName(value) == UriHostNameType.IPv6;

        static bool ValidateIPv4(string value)
        {
            var parts = value.Split('.');
            if (parts.Length != 4)
                return false;

            for (var i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var num) || num < 0 || num > 255)
                    return false;

            return true;
        }

        static Expression BuildItems(JSchema schema, Expression o)
        {
            // compares the array items in val to the schema in sch from offset
            Expression Compare(Expression val, Expression off, JSchema sch)
            {
                var idx = Expression.Variable(typeof(int));
                var brk = Expression.Label(typeof(bool));
                var len = Expression.Property(val, nameof(JArray.Count));

                return Expression.Block(
                    new[] { idx },
                    Expression.Assign(idx, off),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Not(Expression.LessThan(idx, len)),
                            Expression.Break(brk, True),
                            Expression.IfThenElse(
                                Expression.Not(BuildSchema(sch, FromItemIndex(val, idx))),
                                Expression.Break(brk, False),
                                Expression.PostIncrementAssign(idx))),
                        brk));
            }

            // compare single schema item to all array items
            if (schema.ItemsPositionValidation == false && schema.Items.Count > 0)
                return Expression.OrElse(
                    Expression.Not(IsTokenType(o, JTokenType.Array)),
                    Compare(Expression.Convert(o, typeof(JArray)), Expression.Constant(0), schema.Items[0]));

            if (schema.ItemsPositionValidation == true)
            {
                var val = Expression.Convert(o, typeof(JArray));
                var len = Expression.Property(val, nameof(JArray.Count));

                // compares the schema to the array from the beginning
                var cmp = AllOf(schema.Items
                    .Select((i, j) =>
                        Expression.OrElse(
                            Expression.LessThanOrEqual(len, Expression.Constant(j)),
                            BuildSchema(i, FromItemIndex(val, j)))));

                // additional items are not allowed, esure size is equal, and match
                if (schema.AllowAdditionalItems == false)
                    return Expression.AndAlso(
                        Expression.LessThanOrEqual(len, Expression.Constant(schema.Items.Count)),
                        cmp);

                // compare 1:1, but then also compare remaining items from end of schema as offset
                if (schema.AdditionalItems != null)
                    return Expression.AndAlso(
                        cmp,
                        Compare(val, Expression.Constant(schema.Items.Count), schema.AdditionalItems));

                // basic comparison, additional items are allowed, but no validated
                return cmp;
            }

            return null;
        }

        static Expression BuildMaximum(JSchema schema, Expression o)
        {
            if (schema.Maximum == null)
                return null;

            Expression Comparer(Expression left, Expression right) =>
                schema.ExclusiveMaximum ? Expression.LessThan(left, right) : Expression.LessThanOrEqual(left, right);

            return Expression.Switch(
                TokenType(o),
                True,
                Expression.SwitchCase(
                    Comparer(
                        Expression.Convert(o, typeof(int)),
                        Expression.Constant((int)schema.Maximum)),
                    Expression.Constant(JTokenType.Integer)),
                Expression.SwitchCase(
                    Comparer(
                        Expression.Convert(o, typeof(double)),
                        Expression.Constant((double)schema.Maximum)),
                    Expression.Constant(JTokenType.Float)));
        }

        static Expression BuildMaximumItems(JSchema schema, Expression o)
        {
            if (schema.MaximumItems == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.Array),
                Expression.LessThanOrEqual(
                    Expression.Convert(Expression.Property(Expression.Convert(o, typeof(JArray)), nameof(JArray.Count)), typeof(long)),
                    Expression.Constant((long)schema.MaximumItems)));
        }

        static Expression BuildMaximumLength(JSchema schema, Expression o)
        {
            if (schema.MaximumLength == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.String),
                Expression.LessThanOrEqual(
                    Expression.Convert(StringLength(o), typeof(long)),
                    Expression.Constant(schema.MaximumLength)));
        }

        static Expression BuildMaximumProperties(JSchema schema, Expression o)
        {
            if (schema.MaximumProperties == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.Object),
                Expression.LessThanOrEqual(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(o, typeof(JObject)),
                            nameof(JObject.Count)),
                        typeof(long)),
                    Expression.Constant((long)schema.MaximumProperties)));
        }

        static Expression BuildMinimum(JSchema schema, Expression o)
        {
            if (schema.Minimum == null)
                return null;

            Expression Comparer(Expression left, Expression right) =>
                schema.ExclusiveMinimum ? Expression.GreaterThan(left, right) : Expression.GreaterThanOrEqual(left, right);

            return Expression.Switch(
                TokenType(o),
                True,
                Expression.SwitchCase(
                    Comparer(
                        Expression.Convert(o, typeof(int)),
                        Expression.Constant((int)schema.Minimum)),
                    Expression.Constant(JTokenType.Integer)),
                Expression.SwitchCase(
                    Comparer(
                        Expression.Convert(o, typeof(double)),
                        Expression.Constant((double)schema.Minimum)),
                    Expression.Constant(JTokenType.Float)));
        }

        static Expression BuildMinimumItems(JSchema schema, Expression o)
        {
            if (schema.MinimumItems == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.Array),
                Expression.GreaterThanOrEqual(
                    Expression.Convert(Expression.Property(Expression.Convert(o, typeof(JArray)), nameof(JArray.Count)), typeof(long)),
                    Expression.Constant((long)schema.MinimumItems)));
        }

        static Expression BuildMinimumLength(JSchema schema, Expression o)
        {
            if (schema.MinimumLength == null)
                return null;


            return OnlyWhen(
                IsTokenType(o, JTokenType.String),
                Expression.GreaterThanOrEqual(
                    Expression.Convert(StringLength(o), typeof(long)),
                    Expression.Constant(schema.MinimumLength)));
        }

        static Expression BuildMinimumProperties(JSchema schema, Expression o)
        {
            if (schema.MinimumProperties == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.Object),
                Expression.GreaterThanOrEqual(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(o, typeof(JObject)),
                            nameof(JObject.Count)),
                        typeof(long)),
                    Expression.Constant((long)schema.MinimumProperties)));
        }

        static Expression BuildMultipleOf(JSchema schema, Expression o)
        {
            if (schema.MultipleOf == null)
                return null;

            return OnlyWhen(
                IsSchemaType(o, JSchemaType.Integer | JSchemaType.Number),
                CallThis(nameof(MultipleOf),
                    Expression.Convert(o, typeof(JValue)),
                    Expression.Constant((double)schema.MultipleOf)));
        }

        /// <summary>
        /// Returns <c>true</c> if the given value is a multiple of the specified value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="multipleOf"></param>
        /// <returns></returns>
        static bool MultipleOf(JValue value, double multipleOf)
        {
            switch (value.Type)
            {
                case JTokenType.Integer:
                    return MathHelpers.IsIntegerMultiple(value.Value, multipleOf);
                case JTokenType.Float:
                    return MathHelpers.IsDoubleMultiple(value.Value, multipleOf);
                default:
                    throw new InvalidOperationException();
            }
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

            return OneOf(schema.OneOf.Select(i => BuildSchema(i, o)));
        }

        static Expression BuildPattern(JSchema schema, Expression o)
        {
            if (schema.Pattern == null)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.String),
                CallThis(nameof(Pattern), Expression.Constant(schema.Pattern), Expression.Convert(o, typeof(string))));
        }

        static bool Pattern(string pattern, string value)
        {
            try
            {
                return Regex.IsMatch(value, pattern);
            }
            catch (Exception)
            {
                return false;
            }
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

            return OnlyWhen(
                IsTokenType(o, JTokenType.Object),
                AllOf(schema.Properties.Select(i =>
                    BuildProperty(i.Key, i.Value, Expression.Convert(o, typeof(JObject))))));
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

            var val = Expression.Convert(Expression.Call(Expression.Convert(o, typeof(JObject)), nameof(JObject.Properties), null), typeof(IEnumerable<JProperty>));
            var itr = Expression.Variable(typeof(IEnumerator<JProperty>));
            var brk = Expression.Label(typeof(bool));

            return OnlyWhen(
                IsTokenType(o, JTokenType.Object),
                Expression.Block(
                    new[] { itr },
                    Expression.Assign(itr, Expression.Call(val, nameof(IEnumerable<JProperty>.GetEnumerator), null)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Not(Expression.Call(itr, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)))),
                            Expression.Break(brk, True),
                            Expression.IfThen(
                                Expression.Not(
                                    BuildSchema(
                                        schema.PropertyNames,
                                        Expression.Convert(
                                            Expression.Property(
                                                Expression.Property(itr, nameof(IEnumerator<JProperty>.Current)),
                                                nameof(JProperty.Name)),
                                            typeof(JToken)))),
                                Expression.Break(brk, False))),
                        brk)));
        }

        static Expression BuildRequired(JSchema schema, Expression o)
        {
            if (schema.Required.Count == 0)
                return null;

            return OnlyWhen(
                IsTokenType(o, JTokenType.Object),
                AllOf(schema.Required.Select(i => BuildRequired(i, o))));
        }

        static Expression BuildRequired(string propertyName, Expression o)
        {
            return CallThis(
                nameof(ContainsKey),
                Expression.Convert(o, typeof(JObject)),
                Expression.Constant(propertyName));
        }

        static bool ContainsKey(JObject o, string propertyName) =>
            o.ContainsKey(propertyName);

        static Expression BuildType(JSchema schema, Expression o)
        {
            if (schema.Type == null)
                return null;

            return IsSchemaType(o, (JSchemaType)schema.Type);
        }

        static Expression BuildUniqueItems(JSchema schema, Expression o)
        {
            if (schema.UniqueItems == false)
                return null;

            throw new NotImplementedException();

            //var val = Expression.Convert(o, typeof(JArray));
            //var idx = Expression.Variable(typeof(int));
            //var brk = Expression.Label(typeof(bool));
            //var len = Expression.Property(val, nameof(JArray.Count));

            //return Expression.Block(
            //    new[] { idx },
            //    Expression.Assign(idx, Expression.Constant(0)),
            //    Expression.Loop(
            //        Expression.IfThenElse(
            //            Expression.Not(Expression.LessThan(idx, len)),
            //            Expression.Break(brk, True),
            //            Expression.IfThenElse(
            //                Expression.Not(BuildSchema(schema.UniqueItems, FromItemIndex(val, idx))),
            //                Expression.Break(brk, False),
            //                Expression.PostIncrementAssign(idx))),
            //        brk));
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
