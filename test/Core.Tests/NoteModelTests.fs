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
    testCase
        "should return an empty root-level section when given an empty document"
        <| fun () ->
            let document = ""
            let expected = {
                Level = SectionLevel.Root
                Meta = MetadataValue.default'
                ExclusiveText = document
                Children = []
            }

            let actual = NoteModel.parse document

            expected =! actual

    testProperty
        "should return a empty meta but all contents when given a document with no frontmatter"
        <| fun (document: NonEmptyString) ->
            let expected = {
                Level = SectionLevel.Root
                Meta = MetadataValue.default'
                ExclusiveText = document.Get
                Children = []
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
                Children = []
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
                Children = []
            }

            let actual = NoteModel.parse document

            expected =! actual

    testCase
        "should parse heterogeneous arrays of meta"
        <| fun () ->
            let document =
                "\
                ---\n\
                  author: [Spencer, [David], {hi: 5}]\n\
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
                Children = []
            }

            let actual = NoteModel.parse document

            expected =! actual

    testCase
        "should parse nested maps of meta"
        <| fun () ->
            // beware the funky spacing required here
            // \n  \  is adding two spaces in front of the nested properties then ignoring space just for code alignment.
            // Not sure how to make this more intuitive since F# doesn't support relative formatting like C#'s triple quote.
            // F# triple quote is just a literal string that ignores escape sequences
            let document =
                "\
                ---\n\
                  author: Spencer\n\
                  config: \n  \
                    foo: 5\n  \
                    bar: {baz: 8}\n\
                ---\n\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = MetadataValue.fromPairs [
                    "author", SingleValue "Spencer"
                    "config", MetadataValue.fromPairs [
                        "foo", SingleValue "5"
                        "bar", MetadataValue.fromPairs [
                            "baz", SingleValue "8"
                        ] 
                    ]
                ]
                ExclusiveText = document
                Children = []
            }

            let actual = NoteModel.parse document

            expected =! actual


    testCase
        "should parse headings without a codeblock as sections with no meta (other than what's inherited)"
        <| fun () ->
            // TODO: maybe remake this as a property. Probably need to register a custom Arb
            let document =
                "\
                # Title\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = MetadataValue.default'
                ExclusiveText = ""
                Children = [
                    {
                        Level = SectionLevel.Heading 1
                        ExclusiveText = document
                        Meta = MetadataValue.default'
                        Children = []
                    }
                ]
            }

            let actual = NoteModel.parse document

            expected =! actual

    testCase
        "should parse code blocks under headers as meta"
        <| fun () ->
            // TODO: maybe remake this as a property. Probably need to register a custom Arb
            let document =
                "\
                # Title \n\
                ```yml\n\
                  rating: 5\n\
                ```\
                "

            let expected = {
                Level = SectionLevel.Root
                Meta = MetadataValue.default'
                ExclusiveText = ""
                Children = [
                    {
                        Level = SectionLevel.Heading 1
                        ExclusiveText = document
                        Meta = MetadataValue.fromPairs [
                            "rating", SingleValue "5"
                        ]
                        Children = []
                    }
                ]
            }

            let actual = NoteModel.parse document

            expected =! actual
]


// Tests:
// - can take any meta section and round trip it?
// - doc with sections that have meta but no root meta
// - empty doc has no child sections
// - somehow test section nesting. might just be one property but some concrete tests would probably be good
//   - I can't blindly generate nested sections. The sections will have to obey section level hierarchy 
// - full text of root document equals original document
// - full content of a sub-section includes child content but is not the full document (maybe start with section text and add text around it so I can easily know what the original full content should be)
// - don't forget inheritance of meta
 