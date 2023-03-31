module MetadataModelTests

open System
open Expecto
open Notedown.Core
open UnquoteAliases
open Swensen.Unquote.Assertions
open System.Collections.Generic


type DictLikeness<'key,'value> = ('key * 'value) list

module DictLikeness = 
    let fromDict (dictionary: Dictionary<'key,'value>) = [for kvp in dictionary -> (kvp.Key, kvp.Value)] 

[<Tests>]
let metadataModelTests = testList "Metadata Model" [
    testCase "GIVEN an empty file WHEN i build the section model THEN there is no metadata"  <| fun () ->
        let expected = {
            Meta = StructuralDictionary.empty
        }
        let actual = NoteModel.parse ()
        expected =! actual
]