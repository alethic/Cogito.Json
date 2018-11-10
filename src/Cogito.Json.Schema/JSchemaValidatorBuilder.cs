using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

using Cogito.Json.Schema.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Provides support for compiling expression trees implementing JSON schema validation.
    /// </summary>
    public class JSchemaValidatorBuilder
    {

        static readonly Expression True = Expression.Constant(true);
        static readonly Expression False = Expression.Constant(false);
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
            Expression.Property(o, typeof(JToken).GetProperty(nameof(JToken.Type)));

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
                Expression.Call(typeof(JToken).GetMethod(nameof(JToken.DeepEquals)), a, b));

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
        static Expression IsSchemaType(JSchema schema, Expression o, JSchemaType t) =>
            CallThis(nameof(IsSchemaTypeFunc), Expression.Constant(schema), o, Expression.Constant(t));

        /// <summary>
        /// Returns <c>true</c> if the token is of the specified schema type.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        static bool IsSchemaTypeFunc(JSchema schema, JToken o, JSchemaType t)
        {
            if (schema.SchemaVersion == Constants.SchemaVersions.Draft3 ||
                schema.SchemaVersion == Constants.SchemaVersions.Draft4)
                if (o.Type == JTokenType.Float && (t & JSchemaType.Integer) != 0 && (double)o % 1 == 0)
                    return false;

            //  handle cases of floating point values, tested against integer, that are actually even integers
            if (o.Type == JTokenType.Float && (t & JSchemaType.Integer) != 0 && (double)o % 1 == 0)
                return true;

            return (t & SchemaTypeForTokenType(o.Type)) != 0;
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c>
        /// </summary>
        /// <param name="test"></param>
        /// <param name="ifTrue"></param>
        /// <returns></returns>
        static Expression IfThenElseTrue(Expression test, Expression ifTrue)
        {
            return Expression.Condition(test, ifTrue, True);
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c> if all of the given expressions returns <c>true</c>.
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        static Expression AllOf(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate(True, (a, b) => Expression.AndAlso(a, b));
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c> if any of the given expressions returns <c>true</c>.
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        static Expression AnyOf(IEnumerable<Expression> expressions)
        {
            return expressions.Aggregate(False, (a, b) => Expression.OrElse(a, b));
        }

        /// <summary>
        /// Returns an expression that returns <c>true</c> if one of the given expressions returns <c>true</c>.
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
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

        readonly Dictionary<JSchema, ParameterExpression> delayed = new Dictionary<JSchema, ParameterExpression>();
        readonly Dictionary<JSchema, LambdaExpression> compile = new Dictionary<JSchema, LambdaExpression>();
        readonly Func<JSchema, Expression, Expression> buildSchemaFunc = null;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public JSchemaValidatorBuilder(Func<JSchema, Expression, Expression> buildSchemaFunc = null)
        {
            this.buildSchemaFunc = buildSchemaFunc;
        }

        /// <summary>
        /// Builds an expression tree that implements validation of JSON.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public Expression<Func<JToken, bool>> Build(JSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var o = Expression.Parameter(typeof(JToken), "o");
            var e = Build(schema, o);
            return Expression.Lambda<Func<JToken, bool>>(e, o);
        }

        /// <summary>
        /// Builds an expression tree that implements validation of JSON against another expression which provides the
        /// target of the validation.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public Expression Build(JSchema schema, Expression o)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (o == null)
                throw new ArgumentNullException(nameof(o));

            // evaluate expression
            var e = EvalSchema(schema, o);

            // if any recursed, generate assignment of delegates in block
            var v = delayed.Where(i => i.Value != null).ToArray();
            if (v.Length > 0)
                e = Expression.Block(
                    v.Select(i => i.Value).ToArray(),
                    Enumerable.Empty<Expression>()
                        .Concat(v.Select(i => Expression.Assign(i.Value, compile[i.Key])))
                        .Append(e));

            return e;
        }

        /// <summary>
        /// Returns an expression that invokes the validation of the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        Expression EvalSchema(JSchema schema, Expression o)
        {
            // evaluating of this schema is already in progress, return future variable to delegate
            if (delayed.TryGetValue(schema, out var e))
            {
                // we are recursed, but have not yet allocated a variable, do so
                if (e is null)
                    e = delayed[schema] = Expression.Variable(typeof(Func<JToken, bool>));

                // return call to eventually populated delegate variable
                return Expression.Invoke(e, o);
            }

            // insert null entry to detect future recursion
            delayed[schema] = null;

            // build the actual invocation of the validation
            var p = Expression.Parameter(typeof(JToken));
            var b = BuildSchemaBody(schema, p);
            var f = Expression.Lambda<Func<JToken, bool>>(b, p);

            // we did recurse, store away our finished lambda
            if (delayed.TryGetValue(schema, out var e2) && e2 != null)
            {
                compile[schema] = f;
                return Expression.Invoke(e2, o);
            }

            // was never actually recursed, remove
            if (delayed[schema] == null)
                delayed.Remove(schema);

            // return invocation of validator
            return Expression.Invoke(f, o);
        }


        /// <summary>
        /// Returns an expression that returns a delegate to evaluate the schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        Expression EvalSchemaFunc(JSchema schema)
        {
            var p = Expression.Parameter(typeof(JToken));
            return Expression.Lambda<Func<JToken, bool>>(EvalSchema(schema, p), p);
        }

        /// <summary>
        /// Builds a expression tree and lambda for invoking it that implements the validation of the given <see cref="JSchema"/>.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        Expression BuildSchemaBody(JSchema schema, Expression o)
        {
            return buildSchemaFunc?.Invoke(schema, o) ?? AllOf(BuildSchemaExpressions(schema, o).Where(i => i != null));
        }

        IEnumerable<Expression> BuildSchemaExpressions(JSchema schema, Expression o)
        {
            yield return BuildAllOf(schema, o);
            yield return BuildAnyOf(schema, o);
            yield return BuildConst(schema, o);
            yield return BuildContains(schema, o);
            yield return BuildContent(schema, o);
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
            yield return BuildProperties(schema, o);
            yield return BuildPropertyNames(schema, o);
            yield return BuildRequired(schema, o);
            yield return BuildType(schema, o);
            yield return BuildUniqueItems(schema, o);
            yield return BuildValid(schema, o);
            yield return BuildIfThenElse(schema, o);
        }

        Expression BuildAllOf(JSchema schema, Expression o)
        {
            if (schema.AllOf.Count == 0)
                return null;

            return AllOf(schema.AllOf.Select(i => EvalSchema(i, o)));
        }

        Expression BuildAnyOf(JSchema schema, Expression o)
        {
            if (schema.AnyOf.Count == 0)
                return null;

            return AnyOf(schema.AnyOf.Select(i => EvalSchema(i, o)));
        }

        Expression BuildConst(JSchema schema, Expression o)
        {
            if (ReferenceEquals(schema.Const, null))
                return null;

            return DeepEqual(o, Expression.Constant(schema.Const));
        }

        Expression BuildContains(JSchema schema, Expression o)
        {
            if (schema.Contains == null)
                return null;

            var val = Expression.Convert(o, typeof(JArray));
            var idx = Expression.Variable(typeof(int));
            var brk = Expression.Label(typeof(bool));
            var len = Expression.Property(val, nameof(JArray.Count));

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Array),
                Expression.Block(
                    new[] { idx },
                    Expression.Assign(idx, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.Not(Expression.LessThan(idx, len)),
                            Expression.Break(brk, False),
                            Expression.IfThenElse(
                                EvalSchema(schema.Contains, FromItemIndex(val, idx)),
                                Expression.Break(brk, True),
                                Expression.PostIncrementAssign(idx))),
                        brk)));
        }

        Expression BuildContent(JSchema schema, Expression o)
        {
            // no content related validation
            if (schema.ContentEncoding == null &&
                schema.ContentMediaType == null)
                return null;

            switch (schema.ContentEncoding)
            {
                case Constants.ContentEncodings.Base64:
                    return
                        IfThenElseTrue(
                            IsTokenType(o, JTokenType.String),
                            CallThis(
                                nameof(ContentBase64),
                                Expression.Convert(o, typeof(string)),
                                Expression.Constant(schema.ContentMediaType, typeof(string))));
                case null:
                    return IfThenElseTrue(
                        IsTokenType(o, JTokenType.String),
                        CallThis(
                            nameof(ContentMediaTypeString),
                            Expression.Convert(o, typeof(string)),
                            Expression.Constant(schema.ContentMediaType, typeof(string))));
                default:
                    return null;
            }
        }

        /// <summary>
        /// Attempts to validate Base64 content.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        static bool ContentBase64(string value, string mediaType)
        {
            return StringHelpers.IsBase64String(value) && ContentMediaTypeBinary(Convert.FromBase64String(value), mediaType);
        }

        /// <summary>
        /// Attempts to validate the given content according to the specified media type.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        static bool ContentMediaTypeBinary(byte[] value, string mediaType)
        {
            switch (mediaType)
            {
                case null:
                    return true;
                case "application/json":
                    return ContentMediaTypeIsJson(value);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Attempts to validate the given string content according to the specified media type.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        static bool ContentMediaTypeString(string value, string mediaType)
        {
            switch (mediaType)
            {
                case null:
                    return true;
                case "application/json":
                    return ContentMediaTypeIsJson(new StringReader(value));
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Validates that the given byte stream is JSON.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static bool ContentMediaTypeIsJson(byte[] value)
        {
            try
            {
                return ContentMediaTypeIsJson(new StreamReader(new MemoryStream(value)));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that the given text reader is JSON.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static bool ContentMediaTypeIsJson(TextReader reader)
        {
            try
            {
                var j = new JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.None
                };

                // try to read across document
                while (j.Read())
                    continue;

                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        Expression BuildDependencies(JSchema schema, Expression o)
        {
            if (schema.Dependencies == null ||
                schema.Dependencies.Count == 0)
                return null;

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Object),
                AllOf(schema.Dependencies.Select(i => BuildDependencyItem(i.Key, i.Value, o))));
        }

        Expression BuildDependencyItem(string propertyName, object dependencyValue, Expression o)
        {
            return IfThenElseTrue(
                CallThis(nameof(ContainsKey),
                    Expression.Convert(o, typeof(JObject)),
                    Expression.Constant(propertyName)),
                BuildDependencyItem(dependencyValue, o));
        }

        Expression BuildDependencyItem(object dependencyValue, Expression o)
        {
            switch (dependencyValue)
            {
                case JArray a:
                    return BuildDependency(a.Select(i => (string)i).ToArray(), o);
                case string[] a2:
                    return BuildDependency(a2, o);
                case IList<string> a3:
                    return BuildDependency(a3.ToArray(), o);
                case JSchema s:
                    return BuildDependency(s, o);
                default:
                    throw new NotSupportedException();
            }
        }

        Expression BuildDependency(string[] required, Expression o)
        {
            return AllOf(
                required.Select(i =>
                    CallThis(
                        nameof(ContainsKey),
                        Expression.Convert(o, typeof(JObject)),
                        Expression.Constant(i))));
        }

        Expression BuildDependency(JSchema required, Expression o)
        {
            return EvalSchema(required, o);
        }

        Expression BuildEnum(JSchema schema, Expression o)
        {
            if (schema.Enum.Count == 0)
                return null;

            return AnyOf(schema.Enum.Select(i => DeepEqual(o, Expression.Constant(i))));
        }

        static Expression BuildFormat(JSchema schema, Expression o)
        {
            if (schema.Format == null)
                return null;

            return IfThenElseTrue(
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
                case Constants.Formats.IdnHostname:
                    return CallThis(nameof(ValidateIdnHostname), o);
                case Constants.Formats.IPv4:
                case Constants.Formats.Draft3IPv4:
                    return CallThis(nameof(ValidateIPv4), o);
                case Constants.Formats.IPv6:
                    return CallThis(nameof(ValidateIPv6), o);
                case Constants.Formats.Email:
                    return CallThis(nameof(ValidateEmail), o);
                case Constants.Formats.IdnEmail:
                    return CallThis(nameof(ValidateIdnEmail), o);
                case Constants.Formats.Uri:
                    return CallThis(nameof(ValidateUri), o);
                case Constants.Formats.UriReference:
                    return CallThis(nameof(ValidateUriReference), o);
                case Constants.Formats.UriTemplate:
                    return CallThis(nameof(ValidateUriTemplate), o);
                case Constants.Formats.Iri:
                    return CallThis(nameof(ValidateIri), o);
                case Constants.Formats.IriReference:
                    return CallThis(nameof(ValidateIriReference), o);
                case Constants.Formats.JsonPointer:
                    return CallThis(nameof(ValidateJsonPointer), o);
                case Constants.Formats.RelativeJsonPointer:
                    return CallThis(nameof(ValidateRelativeJsonPointer), o);
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
            EmailHelpers.Validate(value, false);

        static bool ValidateIdnEmail(string value) =>
            EmailHelpers.Validate(value, true);

        static bool ValidateUri(string value) =>
            Uri.IsWellFormedUriString(value, UriKind.Absolute);

        static bool ValidateUriReference(string value) =>
            FormatHelpers.ValidateUriReference(value);

        static bool ValidateIri(string value) =>
            Uri.IsWellFormedUriString(value, UriKind.Absolute);

        static bool ValidateIriReference(string value) =>
            FormatHelpers.ValidateIriReference(value);

        static bool ValidateUriTemplate(string value) =>
            FormatHelpers.ValidateUriTemplate(value);

        static bool ValidateJsonPointer(string value) =>
            FormatHelpers.ValidateJsonPointer(value);

        static bool ValidateRelativeJsonPointer(string value) =>
            FormatHelpers.ValidateRelativeJsonPointer(value);

        static bool ValidateDate(string value) =>
            DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateTime(string value) =>
            DateTime.TryParseExact(value, "HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateDateTime(string value) =>
            DateTime.TryParseExact(value, @"yyyy-MM-dd\THH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _) ||
            DateTime.TryParseExact(value.ToUpper(), @"yyyy-MM-dd\THH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _);

        static bool ValidateUtcMilliseconds(string value) =>
            double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var _);

        static bool ValidateRegex(string value)
        {
            try
            {
                new Regex(value, RegexOptions.ECMAScript);
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

        static bool ValidateIdnHostname(string value)
        {
            return IdnHostnameRegex.IsMatch(value);
        }

        static readonly Regex IdnHostnameRegex =
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

        /// <summary>
        /// Evaluates whether each item in the array from the offset.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="off"></param>
        /// <param name="sch"></param>
        /// <returns></returns>
        static bool CompareLocal(JArray val, int off, Func<JToken, bool> sch)
        {
            for (var idx = off; idx < val.Count; idx++)
                if (!sch(val[idx]))
                    return false;

            return true;
        }

        Expression BuildItems(JSchema schema, Expression o)
        {
            Expression CompareExpr(Expression val, Expression off, JSchema sch) =>
                CallThis(nameof(CompareLocal), val, off, EvalSchemaFunc(sch));

            // compare single schema item to all array items
            if (schema.ItemsPositionValidation == false && schema.Items.Count > 0)
                return IfThenElseTrue(
                    IsTokenType(o, JTokenType.Array),
                    CompareExpr(Expression.Convert(o, typeof(JArray)), Expression.Constant(0), schema.Items[0]));

            if (schema.ItemsPositionValidation == true)
            {
                var val = Expression.Convert(o, typeof(JArray));
                var len = Expression.Property(val, nameof(JArray.Count));

                // compares the schema to the array from the beginning
                var cmp = AllOf(schema.Items
                    .Select((i, j) =>
                        Expression.OrElse(
                            Expression.LessThanOrEqual(len, Expression.Constant(j)),
                            EvalSchema(i, FromItemIndex(val, j)))));

                // additional items are not allowed, esure size is equal, and match
                if (schema.AllowAdditionalItems == false)
                    return Expression.AndAlso(
                        Expression.LessThanOrEqual(len, Expression.Constant(schema.Items.Count)),
                        cmp);

                // compare 1:1, but then also compare remaining items from end of schema as offset
                if (schema.AdditionalItems != null)
                    return Expression.AndAlso(
                        cmp,
                        CompareExpr(val, Expression.Constant(schema.Items.Count), schema.AdditionalItems));

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
                    Expression.Condition(
                        Expression.TypeIs(
                            Expression.Property(Expression.Convert(o, typeof(JValue)), nameof(JValue.Value)),
                            typeof(BigInteger)),
                        Comparer(
                            Expression.Convert(Expression.Property(Expression.Convert(o, typeof(JValue)), nameof(JValue.Value)), typeof(BigInteger)),
                            Expression.Constant((BigInteger)schema.Maximum)),
                        Comparer(
                            Expression.Convert(o, typeof(int)),
                            Expression.Constant((int)schema.Maximum))),
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

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Array),
                Expression.LessThanOrEqual(
                    Expression.Convert(Expression.Property(Expression.Convert(o, typeof(JArray)), nameof(JArray.Count)), typeof(long)),
                    Expression.Constant((long)schema.MaximumItems)));
        }

        static Expression BuildMaximumLength(JSchema schema, Expression o)
        {
            if (schema.MaximumLength == null)
                return null;

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.String),
                Expression.LessThanOrEqual(
                    Expression.Convert(StringLength(o), typeof(long)),
                    Expression.Constant(schema.MaximumLength)));
        }

        static Expression BuildMaximumProperties(JSchema schema, Expression o)
        {
            if (schema.MaximumProperties == null)
                return null;

            return IfThenElseTrue(
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

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Array),
                Expression.GreaterThanOrEqual(
                    Expression.Convert(Expression.Property(Expression.Convert(o, typeof(JArray)), nameof(JArray.Count)), typeof(long)),
                    Expression.Constant((long)schema.MinimumItems)));
        }

        static Expression BuildMinimumLength(JSchema schema, Expression o)
        {
            if (schema.MinimumLength == null)
                return null;


            return IfThenElseTrue(
                IsTokenType(o, JTokenType.String),
                Expression.GreaterThanOrEqual(
                    Expression.Convert(StringLength(o), typeof(long)),
                    Expression.Constant(schema.MinimumLength)));
        }

        static Expression BuildMinimumProperties(JSchema schema, Expression o)
        {
            if (schema.MinimumProperties == null)
                return null;

            return IfThenElseTrue(
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

            return IfThenElseTrue(
                IsSchemaType(schema, o, JSchemaType.Integer | JSchemaType.Number),
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

        Expression BuildNot(JSchema schema, Expression o)
        {
            if (schema.Not == null)
                return null;

            return Expression.Not(EvalSchema(schema.Not, o));
        }

        Expression BuildOneOf(JSchema schema, Expression o)
        {
            if (schema.OneOf.Count == 0)
                return null;

            return OneOf(schema.OneOf.Select(i => EvalSchema(i, o)));
        }

        Expression BuildPattern(JSchema schema, Expression o)
        {
            if (schema.Pattern == null)
                return null;

            return IfThenElseTrue(
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

        Expression BuildProperties(JSchema schema, Expression o)
        {
            return AllOf(BuildPropertiesAll(schema, o).Where(i => i != null));
        }

        IEnumerable<Expression> BuildPropertiesAll(JSchema schema, Expression o)
        {
            if (schema.Properties.Count > 0)
                yield return IfThenElseTrue(
                    IsTokenType(o, JTokenType.Object),
                    AllOf(schema.Properties.Select(i =>
                        BuildProperty(i.Key, i.Value, Expression.Convert(o, typeof(JObject))))));

            if (schema.PatternProperties.Count > 0)
                yield return IfThenElseTrue(
                    IsTokenType(o, JTokenType.Object),
                        AllOf(schema.PatternProperties.Select(i =>
                            BuildPatternProperty(i.Key, i.Value, Expression.Convert(o, typeof(JObject))))));

            if (schema.AllowAdditionalProperties == false)
            {
                yield return IfThenElseTrue(
                    IsTokenType(o, JTokenType.Object),
                    CallThis(
                        nameof(AllowAdditionalProperties),
                        Expression.Constant(schema),
                        Expression.Convert(o, typeof(JObject))));
            }
            else if (schema.AdditionalProperties != null)
            {
                var p = Expression.Parameter(typeof(JToken));

                yield return IfThenElseTrue(
                    IsTokenType(o, JTokenType.Object),
                    CallThis(
                        nameof(AdditionalProperties),
                        Expression.Constant(schema),
                        Expression.Convert(o, typeof(JObject)),
                        EvalSchemaFunc(schema.AdditionalProperties)));
            }
        }

        Expression BuildProperty(string propertyName, JSchema propertySchema, Expression o)
        {
            if (o.Type != typeof(JObject))
                throw new ArgumentException(nameof(o));

            return CallThis(nameof(Property), Expression.Constant(propertyName), EvalSchemaFunc(propertySchema), o);
        }

        static bool Property(string propertyName, Func<JToken, bool> propertySchema, JObject o)
        {
            if (o.TryGetValue(propertyName, out var p))
                return propertySchema(p);

            return true;
        }

        Expression BuildPatternProperty(string propertyPattern, JSchema propertySchema, Expression o)
        {
            if (o.Type != typeof(JObject))
                throw new ArgumentException(nameof(o));

            return CallThis(nameof(PatternProperty), Expression.Constant(propertyPattern), EvalSchemaFunc(propertySchema), o);
        }

        static bool PatternProperty(string propertyPattern, Func<JToken, bool> propertySchema, JObject o)
        {
            foreach (var p in o.Properties())
                if (Regex.IsMatch(p.Name, propertyPattern))
                    if (!propertySchema(p.Value))
                        return false;

            return true;
        }

        static bool AllowAdditionalProperties(JSchema schema, JObject o)
        {
            foreach (var p in o.Properties())
                if (schema.Properties.ContainsKey(p.Name) == false &&
                    schema.PatternProperties.Any(i => Regex.IsMatch(p.Name, i.Key)) == false)
                    return false;

            return true;
        }

        static bool AdditionalProperties(JSchema schema, JObject o, Func<JToken, bool> additionalPropertiesSchema)
        {
            foreach (var p in o.Properties())
                if (schema.Properties.ContainsKey(p.Name) == false &&
                    schema.PatternProperties.Any(i => Regex.IsMatch(p.Name, i.Key)) == false)
                    if (additionalPropertiesSchema(p.Value) == false)
                        return false;

            return true;
        }

        Expression BuildPropertyNames(JSchema schema, Expression o)
        {
            if (schema.PropertyNames == null)
                return null;

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Object),
                CallThis(nameof(PropertyNames),
                    Expression.Convert(o, typeof(JObject)),
                    EvalSchemaFunc(schema.PropertyNames)));
        }

        static bool PropertyNames(JObject o, Func<JToken, bool> schema)
        {
            foreach (var p in o.Properties())
                if (!schema(p.Name))
                    return false;

            return true;
        }

        Expression BuildRequired(JSchema schema, Expression o)
        {
            if (schema.Required.Count == 0)
                return null;

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Object),
                AllOf(schema.Required.Select(i => BuildRequired(i, o))));
        }

        Expression BuildRequired(string propertyName, Expression o)
        {
            return CallThis(
                nameof(ContainsKey),
                Expression.Convert(o, typeof(JObject)),
                Expression.Constant(propertyName));
        }

        static bool ContainsKey(JObject o, string propertyName) => o.ContainsKey(propertyName);

        Expression BuildType(JSchema schema, Expression o)
        {
            if (schema.Type == null)
                return null;

            return IsSchemaType(schema, o, (JSchemaType)schema.Type);
        }

        Expression BuildUniqueItems(JSchema schema, Expression o)
        {
            if (schema.UniqueItems == false)
                return null;

            return IfThenElseTrue(
                IsTokenType(o, JTokenType.Array),
                CallThis(nameof(UniqueItems), Expression.Constant(schema), Expression.Convert(o, typeof(JArray))));
        }

        static bool UniqueItems(JSchema schema, JArray a)
        {
            for (var i = 0; i < a.Count; i++)
                for (var j = i + 1; j < a.Count; j++)
                    if (JToken.DeepEquals(a[i], a[j]))
                        return false;

            return true;
        }

        Expression BuildValid(JSchema schema, Expression o)
        {
            if (schema.Valid == true)
                return True;

            if (schema.Valid == false)
                return False;

            return null;
        }

        Expression BuildIfThenElse(JSchema schema, Expression o)
        {
            if (schema.If == null)
                return null;

            return Expression.Condition(
                EvalSchema(schema.If, o),
                schema.Then != null ? EvalSchema(schema.Then, o) : True,
                schema.Else != null ? EvalSchema(schema.Else, o) : True);
        }

    }

}
