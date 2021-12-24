module TagExtraction
open Markdig;
open Markdig.Syntax;
open Notedown.Core;

let private spanToText (source: string) (span:SourceSpan) =
    source.Substring (span.Start, span.Length)

let blockToMarkdownText (markdownNode:MarkdownObject) =
    let sw = new System.IO.StringWriter()
    let renderer = new Markdig.Renderers.Roundtrip.RoundtripRenderer(sw)
    renderer.Write(markdownNode)
    sw.ToString()



let private computeContainingSpan (blocks: MarkdownObject seq) =
    let spanStart =
        blocks |> Seq.map (fun block -> block.Span.Start)
        |> Seq.min

    let spanEnd =
        blocks |> Seq.map (fun block -> block.Span.End)
        |> Seq.max

    SourceSpan(spanStart, spanEnd)

let private isSameBlock (left:Block) (right:Block) = (left.Span.Equals(right.Span))
let getHeadingWithContents (document:MarkdownDocument) (heading:HeadingBlock) =

    let isEqualOrLesserToHeading (heading:HeadingBlock) (block: Block) =
        match block with
        //STEP: stop on the next heading of equal or higher level. Conversely, include lower level headings.
        | :? HeadingBlock as laterHeading -> not (laterHeading.Level <= heading.Level)
        | _ -> true

    let isSectionContent (sectionHeading:HeadingBlock) (block: Block) =
        ((isSameBlock sectionHeading block) || (isEqualOrLesserToHeading sectionHeading block))

    let headingWithContents =
        document
        |> Seq.skipWhile (fun block -> not (isSameBlock block heading))
        |> Seq.takeWhile (isSectionContent heading)

    headingWithContents


let private doesTextContainTag (keyPhrases:string seq) (text: string) =
    let normalizeText (str:string) = str.ToLower()
    let normalizedPhrases = Seq.map normalizeText keyPhrases
    let headerText = text |> normalizeText
    let isMatch = 
        normalizedPhrases 
        |> Seq.tryFind (fun phrase -> headerText.Contains (phrase)) 
        |> Option.isSome
    isMatch




let reduceToTaggedBlocks (tags: string seq) (parsedMarkdown: MarkdownDocument) =

    let isTaggedBlock (keyPhrases:string seq) (node:MarkdownObject) =
        //NOTE: This could really be a separated into a list of predicates if I wanted it to be easy to configure / partially customize
        match node with
        | :? HeadingBlock as h ->
            doesTextContainTag keyPhrases (blockToMarkdownText h)
        | :? ParagraphBlock as p ->
            doesTextContainTag keyPhrases (blockToMarkdownText p)
        | _ -> false

    
    let getChildren (block:MarkdownObject) = block.Descendants()

    TreeUtils.collect getChildren (isTaggedBlock tags) parsedMarkdown

let private extractUnguarded (tags: string list) (markdownDocument: string) : string list=
    //TODO: ast versus string usage is inconsistent, probably a good scenario for a union + constructors
    let parsedDocument = Markdown.Parse(markdownDocument, trackTrivia = true)

    let headingToSectionContentString headingBlock =
        let blocks = getHeadingWithContents parsedDocument headingBlock |> Seq.cast<MarkdownObject> 
        spanToText markdownDocument (computeContainingSpan blocks)

    let paragraphToContentString (paragraphBlock: ParagraphBlock) =
        let getNextBlock block = parsedDocument |> Seq.skipWhile (isSameBlock block) |> Seq.tryHead
        let defaultRender () = spanToText markdownDocument paragraphBlock.Span
        let renderWithList list =
            spanToText markdownDocument (computeContainingSpan [paragraphBlock; list])

        let contentString =
            match paragraphBlock.LinesAfter with
            | null ->
                match getNextBlock paragraphBlock with
                | None -> defaultRender ()
                | Some list ->
                    match list with
                    | :? ListBlock as list -> renderWithList list
                    | _ -> defaultRender ()
            | _ -> defaultRender ()
                
        contentString

    let blockToMarkdownText' (block: MarkdownObject) =
        match block with
        | :? HeadingBlock as h -> headingToSectionContentString h
        | :? ParagraphBlock as p -> paragraphToContentString p
        | _ -> spanToText markdownDocument block.Span

    reduceToTaggedBlocks tags parsedDocument
    |> List.map blockToMarkdownText'
    
let extract (tags: string list) (markdownDocument: string) : string list =
    if markdownDocument = "" then []
    else extractUnguarded tags markdownDocument

