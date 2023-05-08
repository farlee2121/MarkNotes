# Notedown

[![Build](https://github.com/farlee2121/Notedown/actions/workflows/ci.yml/badge.svg)](https://github.com/farlee2121/Notedown/actions/workflows/ci.yml)

Notedown is a [set of conventions](https://spencerfarley.com/2021/03/05/reference-ready-notes/) for notes in markdown and tools that operate on the conventions.
These conventions aim to
- be intuitive as plain text
- not interrupt flow while taking notes
- make notes easy to skim for reference
- easily back into existing notes
- [treat notes as a data source](https://spencerfarley.com/2021/03/05/reference-ready-notes/)

This repository contains tools for programmatically leveraging Notedown conventions: a CLI tool and an .NET library.

## Library / Code Model
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Notedown)](https://www.nuget.org/packages/Notedown)

The code model is written in F# and follows F# design practices, but it can be used from any .NET language

The code model creates a hierarchy of sections in a markdown document each with metadata and contents. 

For example, it would parse a Notedown-based document like this.
```
---
some-config: "I'm root-level meta" 
date: 20xx-MM-dd
tags: [tag1, tag2]
---

Root level content

# I'm a section

Content 

## I'm a child Section
```yml
some-config: "I'm child section meta"
rating: 5
\```
Child Content

```

The parsed data comes in this form
```fsharp
type Section = {
    Level: SectionLevel
    Meta: MetadataValue
    ExclusiveText: string
    Children: Section list
    // There is also a FullText() method
}
```

To create the note model

```fsharp
open Notedown

let noteModel = NoteModel.parse markdownText
```

There are also several optional rules for metadata inheritance. For example
```fsharp
let notesWithMetaInheritance = noteModel |> NoteModel.Inheritance.parentChild
```

You can crawl the note hierarchy to create your own inheritance rules or otherwise
transform the model using `Section.mapFold`.

### Metadata

Metadata is shaped like
```fsharp
type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of EquatableDictionary<string, MetadataValue>
```
List-like values become `Vector`. Key-value maps become `Complex`. And values become `SingleValue`. Note that Vectors can be hetrogeneous. In other words, they could contain a mix of any meta kinds (SingleValue, Vector, Complex).

All meta is read as a string and interpretation is left to the consumer.
Yaml is currently the only supported meta format.

More work needs to be done for conveniently accessing metadata values at a given path.
E.g. getting the value at `config.routes.home`

### Tag Extraction
The other main feature is tag extraction
```fsharp
let extract (tags: string list) (markdownDocument: string) : string list
```

It takes any list of tags to extract and any markdown text, then returns the markdown text of any sections, paragraphs, or list items marked by those tags

```fsharp
let extracted = TagExtraction.extract ["tag:"; "otherTag:"] markdown
```
Here's a sample of how tagging could work
```
## TAGGED: I'm a tagged heading

Content of tagged sections is extracted with the heading

- PRO: This list item is tagged pro
- CON: this list item is tagged con
  - child list items are included with tagged parent

TAGGED: This paragraph is tagged
- List items following the paragraph without space between are included in extraction

```

## Notedown CLI
[![](https://badgen.net/github/release/farlee2121/Notedown?label=zip)](https://github.com/farlee2121/Notedown/releases) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Notedown.Cli)](https://www.nuget.org/packages/Notedown.Cli)

###  Tag Extraction

```powershell
notedown extract-tags file-here.md -t "TAG:" -o output-file.md
```

Extract all content with the given tag by the following rules
- Paragraph: Extracts the full paragraph if the tag appears anywhere in the paragraph
- Paragraph+list: Extracts the paragraph and following list if there is no space between the paragraph and list
- List items: Extracts any list items containing the tag along with any sub-items (but not parent items)
- Headings/Sections: Extracts any heading containing the tag along with all content in the heading's section

### Installation

If you have the dotnet sdk installed, you can just run

```posh
dotnet tool install -g notedown.cli
```

Otherwise, download the zip for your platform from [releases](https://github.com/farlee2121/Notedown/releases).
Unzip and either 
- put the exe in your path
- or create an alias to the exe in your shell profile


