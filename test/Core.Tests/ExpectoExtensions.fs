module ExpectoExtensions

open Expecto


let removeToolUnfriendlyChars (str: string) =
    let forbiddenChars = ["("; ")"]
    let inline replace' (str:string) from = str.Replace(from, "")
    forbiddenChars |> List.fold replace' str 

let theory (name:string) (cases: #seq<'T>) (fTest: 'T -> 'U) =
  let dataToTest caseData =
    testCase (caseData |> string |> removeToolUnfriendlyChars) <| fun () ->
      fTest caseData |> ignore
      
  testList name (cases |> Seq.map dataToTest |> List.ofSeq)
  
let theoryWithResult (name:string) (cases: #seq<('T*'U)>) (fTest: 'T -> 'U) =
  let dataToTest (caseData,expected) =
    testCase ((caseData, expected) |> string |> removeToolUnfriendlyChars) <| fun () ->
      let actual = fTest caseData
      Expect.equal actual expected $"Input: {caseData} \nExpected {expected} \nActual: {actual}"
  
  testList name (cases |> Seq.map dataToTest |> List.ofSeq)
