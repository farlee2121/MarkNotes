module internal MarkdigExtensions

open Markdig.Syntax
open System.Collections.Generic


type Stack<'T> with
    member this.Any() = Seq.length this <> 0


type HeadingHierarchy =
    {
        Heading: HeadingBlock option
        Children: HeadingHierarchy list
        SourceSpan: SourceSpan
    }
with
    member this.Level =
        this.Heading
        |> Option.map (fun h -> h.Level)
        |> Option.defaultValue 0

    member this.StartPosition =
        match this.Heading with
        | Some h -> h.Span.Start
        | None -> 0
        



module HeadingHierarchy =
    let ofHeading block = {
        Heading = Some block
        Children = []
        SourceSpan = block.Span
    }

    let cata f root =
        let rec recurse (node:HeadingHierarchy) =
            f node (node.Children |> List.map recurse)
        recurse root

    let preOrder root: HeadingHierarchy list =
        let rec recurse (node: HeadingHierarchy) =
            let children = List.concat (node.Children |> List.map recurse)
            node :: children
        recurse root

    let scanBack f state root =
        let rec recurse (node:HeadingHierarchy) state =
            let states =
                List.scanBack recurse node.Children state
                |> List.concat
            
            (f node states.Head) :: states

        recurse root state
    //let mapWithPrev fMap fPrev prev root =
    //    let rec recurse (node:HeadingHierarchy) prev =
    //        let (prev, mapped) = List.foldBack recurse node.Children prev
            
    //        (fPrev node, fMap node mapped prev)

    //    recurse root prev



let spanToText (source: string) (span:SourceSpan) =
    source.Substring (span.Start, span.Length)

let blockToMarkdownText (markdownNode:MarkdownObject) =
    let sw = new System.IO.StringWriter()
    let renderer = new Markdig.Renderers.Roundtrip.RoundtripRenderer(sw)
    renderer.Write(markdownNode)
    sw.ToString()

let setSectionSpan (section: HeadingHierarchy) nextSectionStart =
    ({ section with SourceSpan = SourceSpan(section.SourceSpan.Start, nextSectionStart-1)}, section.SourceSpan.Start)

let getRootSpan (documentSpan:SourceSpan) (headings: HeadingHierarchy seq) =
    let rootEnd =
        headings
        |> Seq.tryHead
        |> Option.map (fun h -> h.SourceSpan.Start - 1)
        |> Option.defaultValue documentSpan.End
    SourceSpan(0, rootEnd)

let extractSectionHierarchy (markdownModel: MarkdownDocument) =
    let (headings, _) =
        markdownModel
        |> Seq.choose tryUnbox<HeadingBlock>
        |> Seq.map HeadingHierarchy.ofHeading
        |> (fun l -> Seq.mapFoldBack setSectionSpan l (markdownModel.Span.End + 1))


    let root = {
        Heading = None;
        SourceSpan = getRootSpan markdownModel.Span headings
        Children = [];
    }
    let stack = Stack([root])

    let collectChildren (prev:HeadingHierarchy) =
        let mutable siblingAccumulator = []
        while (stack.Peek().Level >= prev.Level) do
            let top = stack.Pop()
            siblingAccumulator <- top :: siblingAccumulator

        let parent = stack.Pop()
        let merged = {parent with Children = List.concat [parent.Children; siblingAccumulator]}
        stack.Push(merged)

    for h in headings do
        
        while(h.Level < stack.Peek().Level) do
            collectChildren (stack.Peek())
            
        stack.Push(h)

    while stack.Count > 1 do
        collectChildren (stack.Peek())

    stack.Pop()

// Test cases
// all descending items
// starts with headers that will be siblings
// - all ascending levels
// - all same levels
// should really probably have a bunch of tests here