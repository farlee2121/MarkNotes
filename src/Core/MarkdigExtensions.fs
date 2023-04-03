module internal MarkdigExtensions

open Markdig.Syntax

let  spanToText (source: string) (span:SourceSpan) =
    source.Substring (span.Start, span.Length)

let blockToMarkdownText (markdownNode:MarkdownObject) =
    let sw = new System.IO.StringWriter()
    let renderer = new Markdig.Renderers.Roundtrip.RoundtripRenderer(sw)
    renderer.Write(markdownNode)
    sw.ToString()
