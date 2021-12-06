module TagExtraction
open Markdig;
open Markdig.Syntax;
open Notedown.Core;

let blockToMarkdownText (source:string) (markdownNode:MarkdownObject) =
    let span = markdownNode.Span
    source.Substring (span.Start, span.Length)

let getSectionWithContents (document:MarkdownDocument) (heading:HeadingBlock) =

    let isEqualOrLesserToHeading (heading:HeadingBlock) (block: Block) =
        match block with
        //STEP: stop on the next heading of equal or higher level. Conversely, include lower level headings.
        | :? HeadingBlock as laterHeading -> (LanguagePrimitives.PhysicalEquality laterHeading heading) || not (laterHeading.Level <= heading.Level)
        | _ -> true
    
    let headingWithContents =
        document
        |> Seq.skipWhile (fun block -> not (LanguagePrimitives.PhysicalEquality block heading))
        |> Seq.takeWhile (isEqualOrLesserToHeading heading)
     
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

let extractTaggedSections (markdown: string) (tags: string list) =
    // TODO: i probably want to find a pipeline arrangement where the document doesn't need parsed more than once for different kinds of tasks
    //      Probably clearest to just take a parsed document as an option 
    //      consideration: I probably want to preserve order of extractions across exraction types...
    let ast = Markdown.Parse(markdown)
    let getChildren (block:MarkdownObject) = block.Descendants()
    let isTaggedHeading' = isTaggedHeading (blockToMarkdownText markdown) tags

    TreeUtils.collect getChildren isTaggedHeading' ast

