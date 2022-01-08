---
date: 2022-01-07
---

## Motivation

The next core tool I want to tackle is file and section metadata via yaml


## Requirements
- REQ: return a model of the file sections where each section contains
  - direct meta
  - meta w/ inherited values
  - a list of sub-sections
  - section-exclusive content (e.g. no duplication of child section content)
- REQ: sub-section meta should inherit from higher sections, but override any shared fields
  - REQ: meta and section hierarchy is determined by heading levels
- REQ: The following patterns should be recognized as a section with meta
  - Document starting with yaml (either `---` delimited or a code fence) 
  - A heading directly followed by a yaml block
  - a horizontal rule followed by a yaml block


## Exploration
- Q: do higher sections include content of their sub-sections?
  - A: I'd say no. Only content leading up to the sub-section. It's simple for users to decide to compose mutually exclusive sub-section data when desired. Conversely, it'd be hard to get single-layer-only if parents contain undifferentiated child data. It basically asks the user to handle parsing work

Q: Do I want an object hierarchy or data-only hierarchy?
- properties like `Parent` would be convenient, but also introduce recursive relationships
- I could service most cases with methods like `getSectionLineage`
- I think I don't worry about it for now. I can always change it as new use cases arise

NOTE: the sections will all have to share a metadata model

Q: Should I lean on the deserializer for property mapping?
- probably works for now, but I should give it a look

Q: is there any case for metadata on the commandline?
- not really, it's more of a library tool for building other experiences
- maybe extracting sections based on certain meta, but I haven't wanted that yet
