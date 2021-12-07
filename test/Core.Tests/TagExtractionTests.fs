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

let extractTagsAsOriginalText tags markdown = 
    TagExtraction.extractTaggedSections tags markdown
    |> TagExtraction.blocksToDocument markdown

[<Tests>]
let extractTagsTests =

  testList "Extract Tag" [
    test "GIVEN a blank document WHEN I extract tagged content Nothing should be extracted" {
        let markdown = ""
        let extracted = TagExtraction.extractTaggedSections ["tag:"; "otherTag:"] markdown
        Expect.isEmpty extracted ""
    }
    // TODO: empty string shouldn't be a valid tag

    test "GIVEN a header containing a tag WHEN I extract tagged content THEN the header is extracted" {
        let markdown = "##Book: This is title"
        let extracted = TagExtraction.extractTaggedSections ["Book:"] markdown
        let extractedMarkdown = extracted |> (TagExtraction.blocksToDocument markdown)

        unquote <@markdown = extractedMarkdown@>
    }

    test "GIVEN multiple headers, WHEN I extract tagged content, THEN only those with tags I expect are extracted"{
        let expectedTag = "BOOK:"
        let taggedHeader = $"## {expectedTag} tagged"
        let untaggedHeaders = ["## different"; "### header lv 3"; "# A title"; $"close but not {expectedTag.Substring(1)}"];
        let headerOrderings = permute (List.append untaggedHeaders [taggedHeader]) |> List.map (String.join "\n")


        let extractedHeadersPerOrdering = 
            List.map (extractTagsAsOriginalText [expectedTag]) headerOrderings
        Expect.equal extractedHeadersPerOrdering (List.replicate (List.length headerOrderings) taggedHeader) "Every should extract just the one tagged header"
    }

    // extract multiple of same tag

    test "Extract multiple tags" {
        skiptest "Hi"
        // make sure I don't extract tags that aren't listed to extract
    }

    test "GIVEN a header that contains the expected tag WHEN I extract tagged content THEN the whole section belonging to the header is extracted" {
        raise (NotImplementedException())
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