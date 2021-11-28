module TagExtractionTests

open System
open Expecto
open Notedown.Core

[<Tests>]
let extractTagsTests =

  testList "Extract Tag" [
    test "Given a blank document When I extract tagged content Nothing should be extracted" {
        raise (NotImplementedException())
    }

    test "Given a header that contains the expected tag When I extract tagged content Then the whole section belonging to the header is extracted" {
        raise (NotImplementedException())
    }
    test "Given a tag at the start of a line When I extract tagged content Then the line is extracted" {
        raise (NotImplementedException())
    }
    test "Given a tag in the middle of a paragraph Whe I extract tagged content Then the whole paragraph is extracted"{
        raise (NotImplementedException())
    }
    test "Given a tag with any capitalization When I extract tagged content Then the line is extracted" {
        raise (NotImplementedException())
    }

    test "Given a tagged line followed by a list with no space between When I extract tagged content Then the list is extracted with the line"{
        raise (NotImplementedException())
    }

    test "Given a tagged line followed by a list with space between When I extract tagged content Then only the line is extracted"{
        raise (NotImplementedException())
    }
    
    
  ]