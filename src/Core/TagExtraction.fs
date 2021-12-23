module TagExtraction
open Markdig;
open Markdig.Syntax;
open Notedown.Core;

let private spanToText (source: string) (span:SourceSpan) =
    source.Substring (span.Start, span.Length)

let blockToMarkdownText (source:string) (markdownNode:MarkdownObject) =
    spanToText source markdownNode.Span

let private computeContainingSpan (blocks: MarkdownObject seq) =
    let spanStart =
        blocks |> Seq.map (fun block -> block.Span.Start)
        |> Seq.min

    let spanEnd =
        blocks |> Seq.map (fun block -> block.Span.End)
        |> Seq.max

    SourceSpan(spanStart, spanEnd)

let getSectionWithContents (document:MarkdownDocument) (heading:HeadingBlock) =

    let isSameBlock (left:Block) (right:Block) = (left.Span.Equals(right.Span))
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


let isTaggedHeading (blockToMarkdownText':MarkdownObject -> string) (keyPhrases:string seq) (node:MarkdownObject) = 
    let normalizeText (str:string) = str.ToLower()
    let normalizedPhrases = Seq.map normalizeText keyPhrases
    match node with
    | :? HeadingBlock as h ->  
        let headerText = blockToMarkdownText' h |> normalizeText
        let isMatch = 
            normalizedPhrases 
            |> Seq.tryFind (fun phrase -> headerText.Contains (phrase)) 
            |> Option.isSome
        isMatch
    | _ -> false

let extractTaggedSectionsAsBlocks (tags: string seq) (markdown: string) =
    // TODO: i probably want to find a pipeline arrangement where the document doesn't need parsed more than once for different kinds of tasks
    //      Probably clearest to just take a parsed document as an option 
    //      consideration: I probably want to preserve order of extractions across exraction types...
    let ast = Markdown.Parse(markdown)
    let getChildren (block:MarkdownObject) = block.Descendants()
    let isTaggedHeading' = isTaggedHeading (blockToMarkdownText markdown) tags

    TreeUtils.collect getChildren isTaggedHeading' ast


let extract (tags: string list) (markdownDocument: string) : string list=
    //TODO: ast versus string usage is inconsistent
    let parsedDocument = Markdown.Parse(markdownDocument)

    let headingToSectionContentString headingBlock =
        let blocks = getSectionWithContents parsedDocument headingBlock |> Seq.cast<MarkdownObject> 
        spanToText markdownDocument (computeContainingSpan blocks)

    let blockToMarkdownText' (block: MarkdownObject) =
        match block with
        | :? HeadingBlock as h -> headingToSectionContentString h         
        | _ -> blockToMarkdownText markdownDocument block

    extractTaggedSectionsAsBlocks tags markdownDocument
    |> List.map blockToMarkdownText'
    


