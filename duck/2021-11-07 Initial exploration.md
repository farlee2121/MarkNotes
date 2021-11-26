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


## Next Steps
- [ ] Finish transition to project format from csx
- [ ] Create test cases for my expected tag scenarios
- [ ] Run actual extraction to get my book meta notes
- [ ] Package Expecto.TestApi so I can use it as a nuget package (Maybe call it Expecto.TestEnvironment)
- [ ] Add console interface
- [ ] Package as dotnet tool
  - [ ] be sure to use a preview semantic version