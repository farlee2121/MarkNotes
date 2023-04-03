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


let emptyMeta = MetadataValue.Complex StructuralDictionary.empty

[<Tests>]
let metadataModelTests = testList "Note Model" [
    testCase
        "GIVEN an empty file WHEN i build the section model 
        THEN there is no metadata 
        AND the section level is document/root"
        <| fun () ->
            let document = ""
            let expected = {
                Level = SectionLevel.Root
                Meta = emptyMeta
                ExclusiveText = document
            }

            let actual = NoteModel.parse document

            expected =! actual

    testProperty
        "GIVEN an non-empty file with no meta WHEN i build the section model 
        THEN there is no metadata 
        AND the section level is document/root
        AND the section content is the whole document"
        <| fun (document: NonEmptyString) ->
            let expected = {
                Level = SectionLevel.Root
                Meta = emptyMeta
                ExclusiveText = document.Get
            }

            let actual = NoteModel.parse document.Get

            expected =! actual
            document.Get =! actual.Text() 

    testCase
        "GIVEN document meta with a simple key value pair 
        THEN that key-value are parsed"
        <| fun () ->
            let expectedKey = "rating";
            let expectedValue = 5;
            let document =
                $"\
                ---\n\
                  {expectedKey}: {expectedValue}\n\
                ---\n\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = sdict [expectedKey, (expectedValue |> string |> SingleValue)] |> Complex
                ExclusiveText = document
            }

            let actual = NoteModel.parse document

            expected =! actual

    testCase
        "GIVEN document meta with an array value
        THEN that key-value is parsed as an array of meta"
        <| fun () ->
            let document =
                $"\
                ---\n\
                  author: [Spencer, David, Joe]\n\
                ---\n\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = sdict ["author", (["Spencer"; "David"; "Joe"] |> List.map SingleValue |> MetadataValue.Vector)] |> Complex
                ExclusiveText = document
            }

            let actual = NoteModel.parse document

            expected =! actual

    testCase
        "GIVEN document meta with a heterogeneous array
        THEN array values are parsed as the relevant meta type"
        <| fun () ->
            let document =
                $"\
                ---\n\
                  author: [Spencer, [David], {{hi: 5}}]\n\
                ---\n\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = sdict [
                    "author", Vector [
                        SingleValue "Spencer"
                        Vector [ SingleValue "David"]
                        Complex (sdict ["hi", SingleValue "5"])
                    ]
                ] |> Complex
                ExclusiveText = document
            }

            let actual = NoteModel.parse document

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
// - full text of root document equals original document
// - full content of a sub-section includes child content but is not the full document (maybe start with section text and add text around it so I can easily know what the original full content should be)
 