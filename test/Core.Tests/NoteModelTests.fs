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
        "should return an empty root-level section when given an empty document"
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
        "should return a empty meta but all contents when given a document with no frontmatter"
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
        "should parse simple key-value meta"
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
        "should parse array meta"
        <| fun () ->
            let document =
                "\
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
        "should parse heterogeneous arrays of meta"
        <| fun () ->
            let document =
                "\
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

    testCase
        "should parse nested maps of meta"
        <| fun () ->
            // beware the funky spacing require here
            // \n  \  is adding two spaces in front of the nested properties. Not sure how to make this more intuitive since F# doesn't support relative formatting like C#'s triple quote.
            // F# triple quote is just a literal string that ignores escape sequences
            let document =
                "\
                ---\n\
                  author: Spencer\n\
                  config: \n  \
                    foo: 5\n  \
                    bar: {{baz: 8}}\n\
                ---\n\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = (Complex << sdict) [
                    "author", SingleValue "Spencer"
                    "config", (Complex << sdict) [
                        "foo", SingleValue "5"
                        "bar", (Complex << sdict) [
                            "baz", SingleValue "8"
                        ] 
                    ]
                ]
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
 