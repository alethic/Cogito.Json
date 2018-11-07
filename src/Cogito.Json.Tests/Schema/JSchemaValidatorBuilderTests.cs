using System.IO;
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
            var r = new JSchemaValidatorBuilder().Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

        [TestMethod]
        public void Should_fail_to_validate_const_integer()
        {
            var s = new JSchema() { Const = 1 };
            var o = new JValue(2);
            var r = new JSchemaValidatorBuilder().Build(s).Compile().Invoke(o);
            r.Should().BeFalse();
        }

        [TestMethod]
        public void Should_validate_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop"] = 1 };
            var r = new JSchemaValidatorBuilder().Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

        [TestMethod]
        public void Should_fail_to_validate_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop"] = 2 };
            var r = new JSchemaValidatorBuilder().Build(s).Compile().Invoke(o);
            r.Should().BeFalse();
        }

        [TestMethod]
        public void Should_skip_validating_single_property_with_const()
        {
            var s = new JSchema() { Properties = { ["Prop1"] = new JSchema() { Const = 1 } } };
            var o = new JObject() { ["Prop2"] = 2 };
            var r = new JSchemaValidatorBuilder().Build(s).Compile().Invoke(o);
            r.Should().BeTrue();
        }

        [TestMethod]
        public void Can_load_really_big_schema()
        {
            var s = JSchema.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(JSchemaValidatorBuilderTests).Assembly.Location), "Schema", "ecourt_com_151.json")));
            var o = new JObject { };
            var v = new JSchemaValidatorBuilder().Build(s).Compile();
            var r = v.Invoke(o);
        }

    }

}
