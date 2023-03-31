---
date: 2023-03-28
supersede: 2022-01-07 Metadata tool
---

## Motivation

A conversation got me excited about this tooling again.
Meta is really the other key part of the notedown convention and it'd be nice
to demonstrate some kind of basic tooling.

For a basic usecase, my thought is to enable fetching sections based on some simple meta, like rating.

A more advanced usecase might be "Get all quotes from articles by this author". That would require the meta and tag features working together

NOTE: I ended up figuring out a lot of the markdown yml parsing and writing when I migrated my blog to hugo. Should help a lot here
- https://github.com/farlee2121/Hugo-migration/blob/main/Hugo-migration/Program.cs

## Requirements

REQ: create a data model of the sections that includes
- complete metadata including inherited properties
- ability to get the contents of that section

GOAL: Add a CLI command for getting sections based on some meta key-value pair





## TODO
- [ ] Be able to construct a data model with parsed meta
- [ ] Use the object model to filter sections
- [ ] FAKE build to standardize release process
- [ ] move StructuralDictionary into a standalone package? (and post it https://stackoverflow.com/questions/3804367/testing-for-equality-between-dictionaries-in-c-sharp)
- [ ] update ReadMe


## Exploration


Q: These section data models could contain large amounts of text. Should I find a way to just point to the desired text?
- Doing this poorly could make the experience difficult
- OPT: I might be able to return some variant of a FileReader or Stream that holds a reference to a span in the document
  - PRO: The data object size will stay fairly constant and small
    - I may end up opening files multiple times, but the memory use at any given time should be more constant and controllable
    - This should allow me to operate on arbitrarily large amounts of notes
  - PRO: streams and readers are pretty standard in .NET
  - I can still offer a string-based endpoint that evaluates the readers to get section contents, so it shouldn't be too difficult to work with

Q: What should the CLI experience be?
- I may want to extract tags only from sections with certain meta. do I need a joint command?
- OPT: `notedown extract -t "tag" -m "rating:5"`
  - hmm. I'd need to require a tag OR a meta constraint
  - I think this experience probably makes more sense long term. Extracting is really one concept
  - It would be kinda cool if I could pipe (so I do a something like `extract -m "rating:5" | extract -t "!!!"`), but that seems a bit much for now. 
    - FUTURE: come back to piping as a later feature
- OPT: `notedown extract-section -m "rating:5"` 
  - PRO: this has some advantage in that it might be a bit more intuitive to extract sections that *contain* a tag instead of extracting just the tagged text.
    - I'm not convinced that i'm likely to use the tool this way though
  - PRO: makes it a bit easier to not yet handle simultaneous tag and meta constraints

Q: How should meta be represented the data model?
- OPT: keep it as yaml
  - CON: I may want to support other configuration formats other than yaml
- OPT: a dynamic data structure
  - CON: not very idiomatic
- OPT: A dictionary 
  - idiomatic, but I won't be able to differentiate single values from list-likes or complex structures 

Q: what do I need to be able to do with the metadata?
- REQ: match a value under a given key
- REQ: match a single value in an array under a given key (i.e. has a certain tag)
- GOAL: match a value in a hierarchy of nested meta

Q: Continued: How should meta be represented
- OPT: lean into an existing library that defines data kinds
  - CON: I think all of these will be associated with some specific data format like JSON or yaml
  - PRO: These will have robust capabilities already
- OPT: define my own data model that differentiates complex, vector, and simple values
  - PRO: I can hide details I don't care about but other config libraries do (like trivia, formatting, etc)
  - Q: do I need to worry about parsing values like numerics or dates?
    - I don't have insight into the semantics of a field. Even if a field says "date" they may allow values like "christmas 2020". Rating could be numeric or labels ("good, bad, ok").
    - It's likely users may use their own special value conventions. Like how referred-by for me can be a person or a link
    - A: No. I should let the consumers worry about field typing and parsing. 
  - Q: what would this format look like?
    - Vectors may not be homogeneous. They may contain a mix of simple, complex, or more vector values
    - Q: could keys be anything other than strings?
      - A: I don't know of any data format that allow that. Paths are always representable as strings.
    - I think the following F# should handle it

```fsharp
type Meta =
    | SingleValue of string
    | Vector of Meta list
    | Complex of Dictionary<string, Meta>
```


Ok, to make a model of the document I need a tree of the headings. I should be able to just iterate over blocks and keep a stack. Whenever the heading level increases. Then I walk backward collecting children


I don't like calling it metadata model. Maybe I should call it document model or Note model?

PROBLEM: dictionaries don't have value-based equality even in F# and I can't find a pre-built structural dictionary
- OPT: I could just create a metadata likeness, but I expect most of my types to be have value equality. It feel excessive to spread likenesses all over my tests
- OPT: create a structural dictionary type
- Having both constructor overloads and calling base constructors isn't compiling [as the docs describe it](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/inheritance#constructors-and-inheritance)
  - figured it out. You must have the `inherits type-here`, but not a default constructor 
- object expression can override methods! This is a decent alternative to normal likenesses
- OPT: I could use a deep equality library
  - Q: Do I think I'll use dictionary equality normally?
    - I might check equality of config sections, but it seems a bit unlikely


I might not actually use it, but it was fun to learn more about F#'s object syntax and figure out how to make a structural dictionary.
Some key lessons included
- overloading syntax
- Can't have a primary constructor if you overload https://stackoverflow.com/questions/75900935
- overloads won't auto complete (or compile) unless you specify a self-reference like `this`

```fsharp
type StructuralDictionary<'key, 'value when 'key:equality and 'value: equality> =
    inherit Dictionary<'key, 'value>

    new (dictionary:IDictionary<'key,'value>) = { inherit Dictionary<'key,'value>(dictionary) }
    new (dictionary:IDictionary<'key,'value>, comparer) = { inherit Dictionary<'key,'value>(dictionary, comparer) }
    new (collection:IEnumerable<KeyValuePair<'key,'value>>) = { inherit Dictionary<'key,'value>(collection) }
    new (collection:IEnumerable<KeyValuePair<'key,'value>>, comparer) = { inherit Dictionary<'key,'value>(collection, comparer) }
    new (keyComparer:IEqualityComparer<'key>) = { inherit Dictionary<'key,'value>(keyComparer) }
    new (capacity:int) = { inherit Dictionary<'key,'value>(capacity) }
    new (capacity:int, comparer) = { inherit Dictionary<'key,'value>(capacity, comparer) }
    new () = { inherit Dictionary<'key, 'value> () }

    

    override this.Equals(other) =
        match other with
        | :? StructuralDictionary<'key, 'value> as other -> 
            StructuralDictionary.equals this other
        | _ -> false

    override this.GetHashCode() =
        (this |> Seq.map (fun kvp -> (kvp.Key, kvp.Value))).GetHashCode()        

    interface IEquatable<StructuralDictionary<'key,'value>> with
        member this.Equals (other) : bool =
            StructuralDictionary.equals this other
```