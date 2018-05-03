using Cogito.Json.Schema;
using FluentAssertions;
using FluentAssertions.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Schema;

namespace Cogito.Json.Tests
{

    [TestClass]
    public class JSchemaReducerTests
    {

        [TestMethod]
        public void Should_not_alter_single_const()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Const = "123"
            });
        }

        [TestMethod]
        public void Should_reduce_duplicate_allof()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                AllOf =
                {
                    new JSchema()
                    {
                        Const = "Foo",
                    },
                    new JSchema()
                    {
                        Const = "Bar",
                    },
                    new JSchema()
                    {
                        Const = "Foo",
                    },
                }
            });

            s.Title.Should().Be("Test");
            s.AllOf.Should().HaveCount(2);
            s.AllOf.Should().Contain(i => (string)i.Const == "Foo");
            s.AllOf.Should().Contain(i => (string)i.Const == "Bar");
        }

    }

}
