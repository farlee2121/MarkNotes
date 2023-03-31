namespace Notedown.Core

type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of StructuralDictionary<string, MetadataValue>

type SectionMeta = StructuralDictionary<string,MetadataValue>

type Section = {
    Meta: SectionMeta
} 


module NoteModel = 

    let parse () : Section = {
        Meta = StructuralDictionary.empty
    }