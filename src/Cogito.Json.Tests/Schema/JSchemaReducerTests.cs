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
        public void Should_remove_duplicate_allof()
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
        public void Should_remove_duplicate_anyof()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                AnyOf =
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
                AnyOf =
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
        public void Should_remove_duplicate_enum()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                Enum = { "A", "B", "C", "C" }
            });

            var t = new JSchema()
            {
                Title = "Test",
                Enum = { "A", "B", "C" }
            };

            JToken.DeepEquals(s, t).Should().BeTrue();
        }

        [TestMethod]
        public void Should_remove_duplicate_oneof()
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

        [TestMethod]
        public void Should_promote_allof_with_oneof_to_oneof_if_oneof_is_empty()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                AllOf =
                {
                    new JSchema()
                    {
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
                        }
                    }
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

        [TestMethod]
        public void Should_promote_allof_inside_allof_to_parent()
        {
            var s = new JSchemaReducer().Reduce(new JSchema()
            {
                Title = "Test",
                AllOf =
                {
                    new JSchema()
                    {
                        Title = "A",
                        AllOf =
                        {
                            new JSchema()
                            {
                                Title = "B",
                                Const = "DO NOT MOVE"
                            }
                        }
                    },
                    new JSchema()
                    {
                        AllOf =
                        {
                            new JSchema()
                            {
                                Title = "C",
                                Const = "Foo",
                            },
                            new JSchema()
                            {
                                Title = "D",
                                Const = "Bar"
                            },
                        }
                    }
                }
            });

            var t = new JSchema()
            {
                Title = "Test",
                AllOf =
                {
                    new JSchema()
                    {
                        Title = "A",
                        AllOf =
                        {
                            new JSchema()
                            {
                                Title = "B",
                                Const = "DO NOT MOVE"
                            }
                        }
                    },
                    new JSchema()
                    {
                        Title = "C",
                        Const = "Foo",
                    },
                    new JSchema()
                    {
                        Title = "D",
                        Const = "Bar"
                    },
                }
            };

            JToken.DeepEquals(s, t).Should().BeTrue();
        }

    }

}
