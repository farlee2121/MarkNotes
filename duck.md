---
date: 2021-11-07
---

## Motivation

## Markdown Models
https://github.com/xoofx/markdig
- pro: popular
- pro: plugin friendly with many existing plugins
- pro: yaml parser plugin
- pro: diagram support (i.e. mermaid)
- con: AST doesn't appear to be well documented
  - some here https://github.com/xoofx/markdig/blob/master/src/Markdig/Roundtrip.md
  - https://github.com/xoofx/markdig/blob/203cfd6508d4f8fca510edd2f66f229ada2c25ad/src/Markdig.Benchmarks/spec.md
- It's basically just a tree. What I need is a tree walk algorithm

https://www.nuget.org/packages/Freude/
https://www.nuget.org/packages/Westwind.AspNetCore.Markdown/

## .NET Scripting exploration
 https://medium.com/cs-nerds/c-scripting-with-vscode-a-recipe-c672dd44d6
 csx: have to restart omnisharp to get nuget reference intellisense https://github.com/OmniSharp/omnisharp-roslyn/issues/68


-  fsx basics (mostly the same)https://devonburriss.me/fsharp-scripting/
- seems that fsx files can't currently be published
- ionide has handy REPL / fsi commands for sending selections or files to fsi (reminicent of clojure days)
- no obvious debugger integration