module TreeUtilityTests

open Expecto
open Notedown.Internal
open UnquoteAliases


type Tree<'LeafData,'INodeData> =
    | LeafNode of 'LeafData
    | InternalNode of 'INodeData * Tree<'LeafData,'INodeData> seq
let node = InternalNode
let branch child = InternalNode (None, child)
let leaf = LeafNode
let getChildren node =
            match node with
            | InternalNode (_, children) -> children 
            | LeafNode _ -> Seq.empty


[<Tests>]
let treeUtilityTests =
  testList "Tree Utility" [
    test "Basic tree flatten" {
        let tree = branch [leaf 1; branch [leaf 2; leaf 3]; branch [leaf 4; branch [leaf 5; leaf 6]]]

        let flatten agg node =
          match node with
          | LeafNode value -> List.append agg [value]
          | InternalNode _ -> agg

        let actualLeaves = TreeUtils.fold getChildren flatten tree []

        unquote <@ actualLeaves = [1;2;3;4;5;6] @>
    }
  ]