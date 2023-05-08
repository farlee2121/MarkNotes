namespace Notedown.Internal.MarkdigSectionModel

open System.Collections.Generic
open Markdig.Extensions.Yaml
open Markdig.Syntax


type MetaBlock =
    | FrontMatter of YamlFrontMatterBlock
    | FencedCode of FencedCodeBlock

module MetaBlock =
    let toYamlText metaBlock =
        match metaBlock with
        | FrontMatter frontmatter -> frontmatter |> MarkdigExtensions.blockToMarkdownText
        | FencedCode fenced -> fenced.Lines |> string

type HeadingHierarchy =
    {
        Heading: HeadingBlock option
        Children: HeadingHierarchy list
        SourceSpan: SourceSpan
        MetaBlock: MetaBlock option
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

module MarkdigSectionModel =

    type Stack<'T> with
        member this.Any() = Seq.length this <> 0


    let ofHeading block = {
        Heading = Some block
        Children = []
        SourceSpan = block.Span
        MetaBlock = None
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


    let tryFindMetaBlock (document:MarkdownDocument) (section: HeadingHierarchy) =
        let tryGetHeadingMeta (heading: HeadingBlock) =
            let tryYamlBlock (codeBlock:FencedCodeBlock) =
                let allowedIdentifiers = set ["yml"; "yaml"]
                if(allowedIdentifiers.Contains(codeBlock.Info))
                then
                    Some codeBlock
                else None

            let maybeNextBlock = document.FindClosestBlock(heading.Line + 1)
            let maybeCodeBlock = maybeNextBlock |> tryUnbox<FencedCodeBlock>
            maybeCodeBlock |> Option.bind tryYamlBlock
        
        section.Heading |> Option.bind tryGetHeadingMeta


    let getRootSpan (documentSpan:SourceSpan) (headings: HeadingHierarchy seq) =
        let rootEnd =
            headings
            |> Seq.tryHead
            |> Option.map (fun h -> h.SourceSpan.Start - 1)
            |> Option.defaultValue documentSpan.End
        SourceSpan(0, rootEnd)


    let extractSectionHierarchy (markdownModel: MarkdownDocument) =
        let setSectionSpan (section: HeadingHierarchy) nextSectionStart =
            ({ section with SourceSpan = SourceSpan(section.SourceSpan.Start, nextSectionStart-1)}, section.SourceSpan.Start)
        let tryIncludeMetaBlock section =
            {section with MetaBlock = tryFindMetaBlock markdownModel section |> Option.map MetaBlock.FencedCode}

        let (headings, _) =
            markdownModel
            |> Seq.choose tryUnbox<HeadingBlock>
            |> Seq.map ofHeading
            |> Seq.map tryIncludeMetaBlock
            |> (fun l -> Seq.mapFoldBack setSectionSpan l (markdownModel.Span.End + 1))


        let root = {
            Heading = None
            SourceSpan = getRootSpan markdownModel.Span headings
            Children = []
            MetaBlock = markdownModel.FindBlockAtPosition(1) |> tryUnbox<YamlFrontMatterBlock> |> Option.map MetaBlock.FrontMatter
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

