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


## Markdig exploration

NOTE: Paragraph blocks are not children of their heading
- I think this means that I don't actually need to do a tree walk, I need to figure out cases I'm interested in that might actually be nested
  - links and sub-lists are nested (verified, they are examples of nested items sections I would care about)

h.Inline.First().ToString() seems to be the way to get the header text
h.Level is the heading level
Q: how do I get the sections associated with a heading?
- probably need an extra function, should seek to the heading then take everything until the next heading of equal or higher level
- NOTE: I can take advantage of the fact that all headers live at the same level and are necessarily top-level blocks. This means I can just iterate over the top-level block list instead of walking the tree


### Non-heading / inline tags
TODO: how do I do this. book notes could be in many kinds of lines. I want to exlcude headers, but include paragraphs, lists, block quotes
- I also need to handle when a page number comes first. 
- I don't think the line needs to start with the text... It could appear mid-paragraph
- I suppose I need to identify the smallest blocks that would include book info. Hmm, I don't think I can handle the case where there is a paragraph followed by a list...
-  TODO: i should document my assumptions and cases I want handled
  - Case: line starts with tag
  - case: note is in the middle of a pharagraph. In this case, include the whole paragraph?
  - case: list item starts with tag
  - case list item or paragraph has a page number before tag
    - this is a sub case of how we decided to tackle the tag appearing anywhere in a paragraph
  - case: paragraph followed by a list that is supposed to be included
  - case: list item with sub list items 

Q: how do I get a sub-list that go with a tagged list item?
- A: a list item and it's sub-items are all part of one ListItemBlock. The span includes all bullets
- Q: what type do I need to match on? Which has the text readily available and wouldn't cause duplicates?
  - A ListItemBlock with a sub-list contains a paragraph block with the text from the higher level bullet followed by ListItemBlocks for the sub-list items 
  - WARNING: This means I can't match on ParagraphBlocks without checking what the parent is if I want to have special list behavior
  - A: Split paragraph cases since I'll need to check the parent anyway

Q: How do I tell if there is an empty line between a paragraph and a list?
- It looks like white space should be tracked as triva
- !!! I missed that you have to set `trackTrivia = true` https://geoffreymcgill.github.io/markdig/src/markdig/roundtrip/#roundtrip-parser
  - trivia belongs to the earlier block
- !!! This also reveals that I can use RoundTripRenderer to get the text without referring to the original document
- A: The blocks are the same, but the new line will be accounted for by `paragraph.LinesAfter`

For some reason, the `RoundTripRenderer` is leaving out links. I'm just going back to the span approach for now

PICKUP: figure out list behaviors
- probably only want the branch of list items that had the tag


IDEA: I should create sections in the joined doc based on the source of each note

REQ: I should require that tags start with an alphabetic character


## Api

It would be nice if I can package this as a dotnet cli tool

REQ: take in a file or directory
REQ: take in an output file name
REQ: take a tag I want to extract
IDEA: I could also take in a config file that specifies the operation(s), input files, output file(s), and any flags. This would be nice for cases like my book where I may want to regularly run a transform without moving the original files

REQ: should also be consume-able as a library. It's likely that I'll want to use this for building interactive experiences in the future (browse by meta, view ranked readings, etc)

TODO: create a good name to publish the tool with
- MarkNotes?
- Notedown?
- Tagdown?

IDEA: dynamically generate a todo list based off one or more markdown files (TODO tag, maybe also checklists under a todo label)

I notice quotations Expr.Applications lets us apply a list of arguments to a function. They have to be expressions though

## Declarative cli structure

My goal is to define elements of cli (arguments, options, commands, handlers, etc) as complete declarative chunks that can be composed into a full cli.

almost have it, the sticking point is handlers and multi-arity overloads

attempts
- expressions to defer evaluation
  - taking in an untyped expression and templating it into an untyped expression almost works
- inline
  - it still tries to determine a type.
  - I think it doesn't work like addition. Addition can at least determine a member constraint like supporting addition.
- Base class or interface?
  - Func and Action overloads have no base class

Part of the problem is that the overloads take the function's types as separate generic parameters rather than the whole function signature as a single generic

Other ideas?
- require a custom binder and a single input data structure
- Investigate an overload that takes the whole function/delegate type as a single generic parameter

Q: How is the underlying binder implemented?
- Possible lead, it looks like `SetHandler` is actually part of an `ICommandHandler`
  - https://github.com/dotnet/command-line-api/commit/c31ce2797305a4e084736c1d43a078d7e90f9b60
- The underlying handler implementation is nasty redundant.
  - Each overload depends on the exact number of arguments and explicitly uses each generic type argument to extract a parameter then invoke the delegate
  - Problems with using a single structure
    - unlikely all arguments will be different types. If we don't have different types, then there has to be some implicit convention for matching arguments to properties 

IDEA: Could we require a "config type" of the command, then each nested argument and option has to specify a value path in that config type?
- This collapses all arity issue.
- Easier to configure your own convention-based bindings (name-based, type-based, annotation-based) 
- ALT: the config type could have its own configuration where each property is mapped to an argument name or an actual symbol reference
  - still easy to understand, but does have some duplication of argument declaration
  - even better for swapping in convention-based setup
  - remove the distribution of handler configuration
  - enables multiple handler configurations more cleanly
  - con: can only detect incomplete configuration at runtime
    - this is also true of the current paradigm though
- idea: I could probably use a type provider to provide compile-time checking of completeness (early detection of mismatch between handler and argument list). This would require
- TODO: I should be able to make this map-based paradigm within the current customization framework
  - 2 overloads
    - one taking a symbol name
    - one taking a symbol reference
- Looks like AutoMapper uses reflection to manage the LINQ-based path maps
  - https://github.com/AutoMapper/AutoMapper/blob/bdc0120497d192a2741183415543f6119f50a982/src/AutoMapper/Configuration/MappingExpression.cs#L173
  - This means I should either require full set functions or expressions
  - assignment functions would be easier up-front, though expressions would probably be easier for consumers (because I can take care of easily forgotten cases like enumerables)
- IDEA: I could use pipes to allow each input mapper direct access to the value type information. 
  - con: this is not really a self-contained declaration 
  - pro: avoid putting all the handlers in a shared list that requires loss of non-shared type information
- ALT: IDEA: I could construct mappers like `Input.fromName "-t" (fun input value -> expr)` or `Input.fromReference symbol ...`
  - This allows me to wrap the setter with code that shares a signature like `bindingContext -> config -> config`
  - all the work of casting and parsing is baked in at this time, which allows for the setters to all share a signature
    - probably need to pattern match between `bindingContext.ParseResult.GetValueForOption`, `bindingContext.ParseResult.GetValueForArgument`, and error for other
  - i think i should split off some handler sub-type so there is only one complete value passed to represent all the handler needs
    - It looks like I could still create an ICommandHandler that gets passed, this allows compatibility with techniques like the CommandHandler.Create
    - I could also mostly copy-paste the ordered parameter set methods into a class and return ICommandHandler instead of mutating a command internally
      - i.e. `command.Handler = ParameterBased.Create((p1, p2) => ..., arg1, arg2)`
    - I notice the NamingConventionBinder has it's own handler `ModelBindingCommandHandler` that is based on `delegate.DynamicInvoke`
  - Hander binding models so far
    - Naming Convention -> an existing package
    - Primitive delegate params -> copy of SetHandler overloads
    - PropertyMap -> as described above
  - TODO: comment on the awkward favoritism of SetHandler. It'd feel more consistent if the SetHandler overloads were a ICommandHandler factory like the NamingConventionBinder uses. It's just a different creation strategy.
```fs
//NOTE: the bindings could be a dictionary (either <string, Expr> or <IValueDescriptor,Expr>)
// I know it should be possible to specify setters/paths in a type-agnostic way, but not sure how
let handler = Cli.handler<ConfigType>
[
  binding<'a> "-t"  (fun (config) (value:'a) -> config.Tags = value)
  //ALT: then I add assignment to the expression, I can also pattern match on the type of the field to decide assignment strategy (i.e. append to list)
  binding "-t" <@ (fun config -> config.Tags) @>
]

command "name" handler [
  Cli.argument ["-t"] "description"
]


```



## Next Steps
- [x] Finish transition to project format from csx
- [x] Create test cases for my expected tag scenarios
  - [x] still need list scenarios
- [x] Support directory or glob pattern
- [x] Support output file
- [ ] Run actual extraction to get my book meta notes
- [x] Add console interface
- [ ] Ensure only lowest tagged list block is extracted
- [ ] publish
  - [ ] be sure to use a preview semantic version
  - [ ] Package as dotnet tool
  - [ ] downloadable self-contained exe
- [ ] Add Logging with different verbosity levels