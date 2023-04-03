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


Q: should title and meta be included in section contents?
- I think so.
- Q: Where would document meta belong if not to the root contents?
  - I suppose it could belong only as the parsed structure and content could be only the child data
- I think the semantic of "Contents" suggest it's not inclusive of the container or meta, which is the section itself
- Maybe the real change here is the naming like `ExclusiveText` and `FullText` or `CompleteText`


### Content Ref

I want to model section content as a reference or getter, not a direct value. 
It seems likely that data models could grow excessively large very quickly if each section contains a copy of contents.
Especially since sections can be deeply nested and that content would be duplicated in every parent.

Instead I'd like to do something like a stream where the consumer can pull just that content into memory when they need it.

- OPT: A function
  - PRO: very simple in F# with no need to create derivatives for different approaches
  - CON: not equateable
    - Q: If I give it a NoEqualityAttribute, then how will equality for the containing record behave?
      - A: The containing type will no longer support equality. Throws compile-time error
- OPT: Interface & objects
  - PRO: more control over behaviors. i.e. equality can be based on ranges instead of fetching content if compared instances both support it
  - PRO: more intuitive for C# users
  - CON: implementing different approaches requires more effort
- OPT: Streams
  - I need to test this a bit. Overall I haven't found streams very intuitive to work with
    - I don't want to limit myself to file streams, but general streams are a bit unintuitive. You have to use a stream reader...
    - Maybe I provide my own derivative of stream and/or provide my own helper method like `System.IO.File` methods..
  - PRO: Possibly even more granular streaming for sections and documents that are very large
  - PRO: can use existing .NET tools for streaming large amounts of data
  - CON: Complex to implement
    - stream is abstract. I'd have to create a derivative (or several) that support all those methods
    - IDEA: I might be able to get away with basing my type mostly on `FileStream`. Basing my stream on FileStream plus the character range would prevent worries about object reference lifetimes. 
      - However, I need to think about cases where there isn't a file being parsed and the markdown sources from in-memory.
      - This is really tricky because I'd need to start the process knowing where the markdown sources from and track that knowledge.
      - I'm currently always bringing the whole file into memory to parse it anyway. That means I don't realize benefits unless I can manage the whole parsing process with streaming, which seems unlikely. No MarkDig overloads support streams.
        - to clarify. I don't realize benefits over just using a function to point to the original markdig object. There is still benefit in not replicating section contents throughout a hierarchy
          - OPT: this problem can also be solved by only copying top-level contents into the section and stitching together total content by appending child section contents
            - Could call this property "ExclusiveContent"
            - PRO: I minimize in-memory load without creating indirection / using streaming
            - CON: not sure this approach is the most intuitive. 
              - Though I can probably solve that with an extension method. Then `.Content()` is discoverable similar to a property
        - I suppose I just need to be careful to only handle one file at a time
- OPT: Bail on it for now and just use strings for now
  - CON: could be hard to change later
  - PRO: I can get into more immediately valuable usecases
- A: ExclusiveContent
  - Minimize memory load while avoiding indirection or streaming complexity
  - It'll probably be a lot of work if I ever switch to streaming, but streaming is a lot of complexity now that keeps me from moving on to more immediate and known value 




## simple Parsing

I think I need to reconsider having the root section meta be a structural dictionary instead of MetadataValue.
- OPT: Keep the dictionary
  - pro: can check top-level keys easily and intuitively
  - CON: the top level behaves differently than the rest of the recursive structure
- OPT: Use a MetadataValue
  - CON: It's hard for the user to even check top-level keys, which is likely a common task
    - Q: Can I add a helper to check for path existence and extraction? 
      - something path-based like `MetadataValue.tryGet "foo.bar" meta`
  - PRO: Makes access of the metadata more uniform. They'll have to pattern match at some point
  - PRO: I could allow the top-level meta to be something other than a map type


Two remaining tasks
- block to text
- YamlDotNet model to internal model

Q: Why do I use the RoundTripRenderer for checking contents for tags but not for clipping extracted sections?
- ???

Q: What kinds of YamlNodes are there
- `YamlScalarNode`, `YamlAliasNode`, `YamlSequenceNode`, `YamlMappingNode`
- Q: What is an AliasNode?
  - https://stackoverflow.com/questions/70975523
  - Can assign values to names
  - I don't think I should handle these for now

PICKUP: Some example-based meta to make sure I support arrays and nested complex values
- then maybe a property test to text edge cases. Just generate a meta tree, convert it to yaml, then use that as my document
- after that is individual headings, then nested headings
  - probably test different heading levels at top level of document to make sure I can use an h2 not under an h1 etc