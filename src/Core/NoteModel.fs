namespace Notedown.Core

type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of StructuralDictionary<string, MetadataValue>

type SectionMeta = StructuralDictionary<string,MetadataValue>

type HeadingLevel = int
type SectionLevel = 
    | Root
    | Heading of HeadingLevel

type Section = {
    Level: SectionLevel
    Meta: SectionMeta
    ExclusiveContent: string
}

type Section with
    member this.Content() =
        this.ExclusiveContent

module NoteModel = 

    let parse (document: string) : Section = {
        Level = SectionLevel.Root
        Meta = StructuralDictionary.empty
        ExclusiveContent = document
    }