---
date: 2023-05-08
---

## Motivation

I often don't care about understanding the whole metadata structure. 
All i really want is access to a particular value, if it exists.

This is not currently well supported by the library.


## Requirements

- Req: be able to get a metadata value at a given path, if it exists
  - REQ: I should be able to get values in an arbitrary depth of nexted complex values
  - GOAL: I should be able to specify indexes of array values at any level of the selector
- REQ: be able to tell if no value exists at that given path
- GOAL: be able to set a meta value at a given path

Other
- it'd be nice if I could conveniently convert meta into a well structured format of my own, and from the well-structured data back to meta
  - Ideally, I do this as one operation and don't have to set values individually


## Exploration

Q: What format do I use for the selector?
- OPT: string
  - Con: they could use all kinds of invalid syntax
  - PRO: it feels pretty familiar due to xpath selection
  - I think I can do it without getting too complicated. Just split everything on `.`. Might feel a bit off for index selection, but keeps it simple
	- I can always circle back and support indexer style (i.e `root.something.[1].property`)
	- hmm. even with square brackets I think I still want the `.`, it just reads better. That also makes it easier ot support that style. I just have to trim the braces and see if it's a number

Q: Should I let them set the root value?
- I think I will for clobber, but not trySet

Q: What should I do if they try to set a value with a non-complex value in the hierarchy?
- i.e. `config { routes: ["", ""] }` but they try to set `config.routes.home`
- OPT: clobber the existing value
- OPT: fail and don't set a value
- OPT: give them the option to clobber or not clobber

Q: Should set take a string or a metadata value?
- It's easy for them to convert a value. Setting a structure is more powerful


Q: I'm not loving the meta names. Should I change them to something else?
- Maybe: Map, Vector, Value
- Maybe: Map, Vector,
- Vector vs list?
- Single vs Value vs Simple


PROBLEM: if I support writing, then I also need to validate path segments. They can't contain any chars invalid in a json key
- I suppose I can just let it error if/when they try to serialize it 



More test cases to consider
- support array indexing
  - trySet succeeds if a valid array index is specified at expected portion of selector
- clobber 
  - fails if the path selector is invalid?
- trySet (non-clobbering)
  - fails if the selector is invalid?