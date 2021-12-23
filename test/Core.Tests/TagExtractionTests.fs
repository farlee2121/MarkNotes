module TagExtractionTests

open System
open Expecto
open Notedown.Core
open UnquoteAliases

let permute list =
  let rec inserts e = function
    | [] -> [[e]]
    | x::xs as list -> (e::list)::(inserts e xs |> List.map (fun xs' -> x::xs'))

  List.fold (fun accum x -> List.collect (inserts x) accum) [[]] list

module String = 
    let join (separator: string) (strings:string seq) =
        String.Join(separator, strings)

    let joinLines (strings: string seq) = join "\n" strings
    let split (separator: string) (str: string) = 
        str.Split(separator)

let extractTagsAsOriginalText tags markdown = 
    TagExtraction.extract tags markdown
    |> String.joinLines


[<Tests>]
let extractTagsTests =

  testList "Extract Tag" [
    // TODO: empty string shouldn't be a valid tag
    test "GIVEN a blank document WHEN I extract tagged content Nothing should be extracted" {
        let markdown = ""
        let extracted = TagExtraction.extract ["tag:"; "otherTag:"] markdown
        Expect.isEmpty extracted ""
    }

    test "GIVEN a header containing a tag WHEN I extract tagged content THEN the header is extracted" {
        let markdown = "## Book: This is title"
        let extractedMarkdown = TagExtraction.extract ["Book:"] markdown

        unquote <@markdown = (extractedMarkdown |> String.joinLines)@>
    }

    test "GIVEN a document with multiple headers, WHEN I extract tagged content, THEN only those with tags I expect are extracted"{
        let expectedTag = "BOOK:"
        let taggedHeader = $"## {expectedTag} tagged"
        let untaggedHeaders = ["## different"; "# A title"; $"## close but not {expectedTag.Substring(1)}"];
        let headerOrderings = permute (List.append untaggedHeaders [taggedHeader]) |> List.map (String.joinLines)


        let extractedHeadersPerOrdering = 
            List.map (extractTagsAsOriginalText [expectedTag]) headerOrderings
        Expect.equal extractedHeadersPerOrdering (List.replicate (List.length headerOrderings) taggedHeader) "Every arrangement should extract just the one tagged header"
    }

    test "GIVEN a document with numerous headers and multiple tagged headers WHEN i extract tagged content THEN all tagged headers are extracted" {
        let expectedTag = "BOOK:"
        let taggedHeaders = List.init 3 (fun i-> $"## {expectedTag} {Guid.NewGuid()}");
        let untaggedHeaders = ["## different"; "## header"; "# A title"; $"## close but not {expectedTag.Substring(1)}"];
        let headerOrderings = permute (List.append untaggedHeaders taggedHeaders) |> List.map (String.joinLines)


        let expectedExtraction = List.sort taggedHeaders |> String.joinLines
        let extractedHeadersPerOrdering = 
            List.map (TagExtraction.extract [expectedTag]) headerOrderings
            
        let orderAndJoin lines = lines |> Seq.sort |> String.joinLines 
        let actualExctractionsWithOrderedLines = extractedHeadersPerOrdering |> List.map orderAndJoin
        
        Expect.equal actualExctractionsWithOrderedLines (List.replicate (List.length headerOrderings) expectedExtraction) "All extractions should return the same tagged headers, ignoring order"
    }

    test "GIVEN multiple tags to extract and a document containing headers with each tag WHEN I extract tagged content THEN headers with any listed tag are extracted" {
        let expectedTags = List.init 3 (fun i -> $"{Guid.NewGuid()}:")
        let expectedHeaders = expectedTags |> List.map (fun tag -> $"## {tag}")
        let unexpectedHeaders = List.init 3 (fun i -> $"## {Guid.NewGuid()}")

        let document =
            [expectedHeaders; unexpectedHeaders]
            |> List.concat
            |> String.joinLines

        let extractedContent = TagExtraction.extract expectedTags document |> List.ofSeq

        unquote <@(List.sort expectedHeaders) = (List.sort extractedContent)@>
    }

    test "GIVEN a header that contains the expected tag WHEN I extract tagged content THEN the whole section belonging to the header is extracted" {
        let expectedTag = "BOOK:"
        let expectedSection = $"\
            ## {expectedTag} Title \n\
            This is a paragraph belonging with a [link](https://spencerfarley.com). \n\
            \n\
            ### Sub-header to include \n\
            - this one includes a list \n\
            - with multiple items \
        "
        let excludedSections = ["## Equivalent header level"; "# Higher header level"]

        let document = String.joinLines (List.append [expectedSection] excludedSections)
        let extractedContent = extractTagsAsOriginalText [expectedTag] document
        unquote <@ expectedSection = extractedContent @>
    }



    test "GIVEN a tag at the start of a line WHEN I extract tagged content THEN the line is extracted" {
        raise (NotImplementedException())
    }
    test "GIVEN a tag in the middle of a paragraph Whe I extract tagged content THEN the whole paragraph is extracted"{
        raise (NotImplementedException())
    }
    test "GIVEN a tag with any capitalization WHEN I extract tagged content THEN the line is extracted" {
        raise (NotImplementedException())
    }

    test "GIVEN a tagged line followed by a list with no space between WHEN I extract tagged content THEN the list is extracted with the line"{
        raise (NotImplementedException())
    }

    test "GIVEN a tagged line followed by a list with space between WHEN I extract tagged content THEN only the line is extracted"{
        raise (NotImplementedException())
    }
       
  ]