#r "nuget: Markdig, 0.26.0"
#load "TreeWalk.csx"

using Markdig;
using Markdig.Syntax;
using System;
using System.Collections.Generic;


var source = @"## hi
My name is nyanta and this is a section.

So much in section [link](http://google.com)

### Sub heading

## BOOK: yo dog

This is a paragraph to include

> such include

## New Section
> Block quote

paragraph

- one 
- two 
  - sub 1
  - sub 2
- three

paragraph with list
- item 1
- item 2
";

var ast = Markdown.Parse(source, trackTrivia: true);

var blocks = ast.ToList();

string ToOriginalMarkdown(string source, SourceSpan span){
    return source.Substring(span.Start, span.Length);
}

string ToOriginalMarkdown(MarkdownObject node){
    var sw = new System.IO.StringWriter();
    var renderer = new Markdig.Renderers.Roundtrip.RoundtripRenderer(sw);
    renderer.Write(node);
    return sw.ToString();
}

// CASE: should consider what happens when tags like BOOK: show up in a section that also gets included as a whole

IEnumerable<Block> GetSectionWithContents(MarkdownDocument document, HeadingBlock heading){
    var headingWithContents = document.ToList()
    .SkipWhile(block => block != heading)
    .TakeWhile(block => {
        if(block is HeadingBlock laterHeading){
            //STEP: stop on the next heading of equal or higher level. Conversely, include lower level headings.
            return laterHeading == heading || !(laterHeading.Level <= heading.Level) ;
        }
        else{
            return true;
        }
    });
    return headingWithContents.ToArray();
}

bool IsBookHeading(MarkdownObject node){
    string[] keyPhrases = new []{"Book:", "Takeaways for my book"}.Select(s => s.ToLower()).ToArray();
    if(node is HeadingBlock h){
        var headerText = ToOriginalMarkdown(source, h.Span);
        bool isMatch = keyPhrases.Any(phrase => headerText.ToLower().Contains(phrase));
        return isMatch;
    }
    else{
        return false;
    }
}

bool IsBookNote(MarkdownObject node){
    string[] keyPhrases = new []{"Book:"}.Select(s => s.ToLower()).ToArray();
    if(node is HeadingBlock h){
        
        var headerText = ToOriginalMarkdown(source, h.Span);
        bool isMatch = keyPhrases.Any(phrase => headerText.ToLower().Contains(phrase));
        return isMatch;
    }
    else{
        return false;
    }
}


// Walk(ast, (MarkdownObject block) => block.Descendants(), (MarkdownObject node) =>{
//     if(node is HeadingBlock h){
//         Console.WriteLine(h);
//     }
//     if(node is ListBlock l){
//         Console.WriteLine(l);
//     }
// });

var headings = Collect(ast, (MarkdownObject block) => block.Descendants(), IsBookHeading);


var fullSections = headings.Cast<HeadingBlock>().SelectMany(heading =>GetSectionWithContents(ast, heading)).ToList();

Console.WriteLine($"Hello world! {String.Join(", ", Args)}");
