using Cogito.Json.Schema;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Tests.Schema
{

    [TestClass]
    public class JSchemaValidatorBuilderTests
    {

        [TestMethod]
        public void Should_validate_const_integer()
        {
            var s = new JSchema() { Const = 1 };
            var o = new JValue(1);
            var r = JSchemaValidatorBuilder.Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

        [TestMethod]
        public void Should_fail_to_validate_const_integer()
        {
            var s = new JSchema() { Const = 1 };
            var o = new JValue(2);
            var r = JSchemaValidatorBuilder.Build(s).Compile().Invoke(o);
            r.Should().BeFalse();
        }

        [TestMethod]
        public void Should_validate_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop"] = 1 };
            var r = JSchemaValidatorBuilder.Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

        [TestMethod]
        public void Should_fail_to_validate_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop"] = 2 };
            var r = JSchemaValidatorBuilder.Build(s).Compile().Invoke(o);
            r.Should().BeFalse();
        }

        [TestMethod]
        public void Should_skip_validating_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop1"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop2"] = 2 };
            var r = JSchemaValidatorBuilder.Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

    }

}
