module Tests

open System
open Expecto
open Notedown.Core


type Tree<'LeafData,'INodeData> =
    | LeafNode of 'LeafData
    | InternalNode of 'INodeData * Tree<'LeafData,'INodeData> seq
let node = InternalNode
let branch child = InternalNode (None, child)
let leaf = LeafNode
let getChildren node =
            match node with
            | InternalNode (_, children) -> children 
            | LeafNode _ -> []

module csharp =
  let toFunc<'a, 'b> f =
      System.Func<'a, 'b> f
  let toFunc2<'a, 'b, 'c> f =
      System.Func<'a, 'b, 'c> f
  let toAction<'a> f =
      System.Action<'a> f
  let toAction2<'a, 'b> f =
      System.Action<'a, 'b> f


module Tree = 
  let walk<'node> root fGetChildren fNode = Notedown.Core.Tree.Walk<'node>(root, fGetChildren |> csharp.toFunc, fNode |> csharp.toAction)
  let collect<'node> root fGetChildren ftest = Notedown.Core.Tree.Collect<'node>(root, fGetChildren |> csharp.toFunc, ftest |> csharp.toFunc)
  let fold<'node, 'agg> root fGetChildren fFold initial = Notedown.Core.Tree.Fold<'node, 'agg>(root, fGetChildren |> csharp.toFunc, fFold |> csharp.toFunc2, initial)




[<Tests>]
let treeUtilityTests =
  testList "Tree Utility" [
    test "Basic tree flatten" {
        let tree = branch [leaf 1; branch [leaf 2; leaf 3]; branch [leaf 4; branch [leaf 5; leaf 6]]]

        

        let flatten agg node =
          match node with
          | LeafNode value -> Seq.append agg [value]
          | InternalNode _ -> agg

        let actualLeaves = Tree.fold tree getChildren flatten []

        Expect.equal actualLeaves [1;2;3;4;5;6] ""
    }
  ]