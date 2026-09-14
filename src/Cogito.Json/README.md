# Cogito.Json

Compiles a `JToken` into an equality test, so comparing many documents against one template is fast.

## Why

`JToken.DeepEquals` walks both documents every time. When the same expected shape is compared against
a stream of incoming documents — matching events, routing messages, filtering a feed — that walk is
repeated work. Building the comparison once as an expression tree and compiling it turns it into
straight-line code.

## Install

```shell
dotnet add package Cogito.Json
```

## Use

```csharp
var builder = new JTokenEqualityExpressionBuilder();
var compare = builder.Build(expected).Compile();

if (compare(incoming))
    Handle(incoming);
```

Build once, call many times. For `System.Text.Json` see `Cogito.Text.Json`.

## License

MIT.
