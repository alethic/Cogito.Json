# Cogito.Json

[![Build](https://github.com/alethic/Cogito.Json/actions/workflows/Cogito.Json.yml/badge.svg)](https://github.com/alethic/Cogito.Json/actions/workflows/Cogito.Json.yml)

Compiles a JToken into an equality test, for comparing many documents against one template quickly.

## Packages

**[Cogito.Json](https://www.nuget.org/packages/Cogito.Json)** — Compiles a `JToken` into an equality test, so comparing many documents against one template is fast.

Each package carries its own README with the detail; the links above go to nuget.org.

## Building

```shell
dotnet restore Cogito.Json.slnx
dotnet msbuild -p:Configuration=Release Cogito.Json.dist.msbuildproj
```

Packages are staged into `dist/nuget` and test suites into `dist/tests`; run a suite with
`dotnet test -f <tfm> <path to its assembly>`.

## License

MIT — see [LICENSE](LICENSE).
