#r "nuget: Markdig, 0.26.0"
#load "TreeWalk.csx"

using Markdig;
using System;
using System.Collections.Generic;


var ast = Markdown.Parse("## hi");

// first step is that I need a tree walk
// the walk can probably take an an action. I can build off that to create a flatten or select 

TestTree();

Console.WriteLine($"Hello world! {String.Join(", ", Args)}");
