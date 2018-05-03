using Cogito.Json.Schema;
using FluentAssertions;
using FluentAssertions.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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

            JToken.DeepEquals(s, s).Should().BeTrue();
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

            var t = new JSchema()
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
                    }
                }
            };

            JToken.DeepEquals(s, t).Should().BeTrue();
        }

        [TestMethod]
        public void Should_reduce_duplicate_oneof()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                OneOf =
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

            var t = new JSchema()
            {
                Title = "Test",
                OneOf =
                {
                    new JSchema()
                    {
                        Const = "Foo",
                    },
                    new JSchema()
                    {
                        Const = "Bar",
                    }
                }
            };

            JToken.DeepEquals(s, t).Should().BeTrue();
        }

    }

}
