using System.IO;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

namespace Cogito.Json.Tests
{

    [TestClass]
    public class JTokenEqualityExpressionBuilderTests
    {

        [TestMethod]
        public void Should_evaluate_fairly_large_object_as_true()
        {
            var o = JObject.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(typeof(JTokenEqualityExpressionBuilderTests).Assembly.Location).FullName, "efm.json")));
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(o);
            var m = e.Compile();
            m.Invoke(o).Should().BeTrue();
        }

        [TestMethod]
        public void Should_evaluate_fairly_large_object_as_false()
        {
            var o = JObject.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(typeof(JTokenEqualityExpressionBuilderTests).Assembly.Location).FullName, "efm.json")));
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(o);
            var m = e.Compile();

            o["Next"] = 123;
            m.Invoke(o).Should().BeFalse();
        }

        [TestMethod]
        public void Should_evaluate_simple_object_as_false()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JObject() { ["Foo"] = "Bar" });
            var m = e.Compile();
            m.Invoke(new JObject() { ["Foo"] = "Foo" }).Should().BeFalse();
        }

        [TestMethod]
        public void Should_evaluate_simple_object_as_true()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JObject() { ["Foo"] = "Bar" });
            var m = e.Compile();
            m.Invoke(new JObject() { ["Foo"] = "Bar" }).Should().BeTrue();
        }

        [TestMethod]
        public void Should_evaluate_object_with_two_properties_as_true()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JObject() { ["Foo"] = "Bar", ["Joe"] = 123 });
            var m = e.Compile();
            m.Invoke(new JObject() { ["Foo"] = "Bar", ["Joe"] = 123 }).Should().BeTrue();
        }

        [TestMethod]
        public void Should_evaluate_missing_property_as_false()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JObject() { ["Foo"] = "Bar" });
            var m = e.Compile();
            m.Invoke(new JObject() { ["Asd"] = "Bar" }).Should().BeFalse();
        }

        [TestMethod]
        public void Should_evaluate_extra_property_as_false()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JObject() { ["Foo"] = "Bar" });
            var m = e.Compile();
            m.Invoke(new JObject() { ["Foo"] = "Bar", ["Joe"] = 1 }).Should().BeFalse();
        }

        [TestMethod]
        public void Should_compare_booleans()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JValue(true));
            var m = e.Compile();
            m.Invoke(new JValue(true)).Should().BeTrue();
        }

        [TestMethod]
        public void Should_compare_booleans_as_false()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JValue(true));
            var m = e.Compile();
            m.Invoke(new JValue(false)).Should().BeFalse();
        }

        [TestMethod]
        public void Should_compare_strings()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JValue("TEST"));
            var m = e.Compile();
            m.Invoke(new JValue("TEST")).Should().BeTrue();
        }

        [TestMethod]
        public void Should_compare_strings_as_false()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JValue("TEST"));
            var m = e.Compile();
            m.Invoke(new JValue("TEST2")).Should().BeFalse();
        }

        [TestMethod]
        public void Should_compare_empty_arrays()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JArray());
            var m = e.Compile();
            m.Invoke(new JArray()).Should().BeTrue();
        }

        [TestMethod]
        public void Should_compare_arrays_with_one_value()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JArray() { true });
            var m = e.Compile();
            m.Invoke(new JArray() { true }).Should().BeTrue();
        }

        [TestMethod]
        public void Should_compare_arrays_with_one_unequal_value()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JArray() { true });
            var m = e.Compile();
            m.Invoke(new JArray() { false }).Should().BeFalse();
        }

        [TestMethod]
        public void Should_compare_arrays_with_unequal_size()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(new JArray() { true });
            var m = e.Compile();
            m.Invoke(new JArray() { true, true }).Should().BeFalse();
        }

        [TestMethod]
        public void Can_compare_against_null()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(JObject.Parse("{'foo': 12}"));
            var m = e.Compile();
            m.Invoke(JValue.CreateNull()).Should().BeFalse();
        }

        [TestMethod]
        public void Can_compare_against_badly_typed_properties()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(JObject.Parse("{'foo': 12}"));
            var m = e.Compile();
            m.Invoke(JObject.Parse("{'foo': false}")).Should().BeFalse();
        }

        [TestMethod]
        public void Should_compare_integers_as_equal_if_difference_is_decimal_only_ltr ()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(JToken.Parse("0"));
            var m = e.Compile();
            m.Invoke(JToken.Parse("0.0")).Should().BeTrue();
        }

        [TestMethod]
        public void Should_compare_integers_as_equal_if_difference_is_decimal_only()
        {
            var b = new JTokenEqualityExpressionBuilder();
            var e = b.Build(JToken.Parse("0.0"));
            var m = e.Compile();
            m.Invoke(JToken.Parse("0")).Should().BeTrue();
        }

    }

}
