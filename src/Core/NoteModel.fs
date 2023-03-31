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
} 



module NoteModel = 

    let parse () : Section = {
        Level = SectionLevel.Root
        Meta = StructuralDictionary.empty
    }