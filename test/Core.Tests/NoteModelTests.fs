module MetadataModelTests

open System
open Expecto
open Notedown.Core
open UnquoteAliases
open Swensen.Unquote.Assertions
open System.Collections.Generic
open FsCheck


type DictLikeness<'key,'value> = ('key * 'value) list

module DictLikeness = 
    let fromDict (dictionary: Dictionary<'key,'value>) = [for kvp in dictionary -> (kvp.Key, kvp.Value)] 

[<Tests>]
let metadataModelTests = testList "Note Model" [
    testCase """GIVEN an empty file WHEN i build the section model 
        THEN there is no metadata 
        AND the section level is document/root
        """  <| fun () ->
            let document = ""
            let expected = {
                Level = SectionLevel.Root
                Meta = StructuralDictionary.empty
                ExclusiveContent = document
            }

            let actual = NoteModel.parse document

            expected =! actual

    testProperty """GIVEN an non-empty file with no meta WHEN i build the section model 
        THEN there is no metadata 
        AND the section level is document/root
        AND the section content is the whole document
        """  <| fun (document: NonEmptyString) ->
            let expected = {
                Level = SectionLevel.Root
                Meta = StructuralDictionary.empty
                ExclusiveContent = document.Get
            }

            let actual = NoteModel.parse document.Get

            expected =! actual
]


// Tests:
// - empty doc has no child sections
// - empty doc content span is nothing
// - a meta section with a simple value
// - a meta section with an array value
// - a meta section with a complex value
// - can take any meta section and round trip it?
// - doc with sections that have meta but no root meta
// - somehow test section nesting. might just be one property but some concrete tests would probably be good