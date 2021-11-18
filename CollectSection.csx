#r "nuget: Markdig, 0.26.0"
#load "TreeWalk.csx"

using Markdig;
using Markdig.Syntax;
using System;
using System.Collections.Generic;


var source = @"## hi
My name is nyanta and this is a section.

So much in senction [link](http://google.com)

### Sub heading

## BOOK: yo dog

This is a paragraph to include

> such include

## New Section
> Block quote

- one 
- two 
  - sub 1
  - sub 2
- three
";

var ast = Markdown.Parse(source);

var blocks = ast.ToList();

string ToOriginalMarkdown(string source, SourceSpan span){
    return source.Substring(span.Start, span.Length);
}
// CASE: should consider what happens when tags like BOOK: show up in a section that also gets included as a whole

IEnumerable<Block> GetSectionWithContents(MarkdownDocument document, HeadingBlock heading){
    var headingWithContents = document.ToList()
    .SkipWhile(block => block != heading)
    .TakeWhile(block => {
        if(block is HeadingBlock laterHeading){
            // stop on the next heading of equal or higher level. Conversely, include lower level headings.
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
        // TODO: how do I do this. book notes could be in many kinds of lines. I want to exlcude headers, but include paragraphs, lists, block quotes
        // I also need to handle when a page number comes first. 
        // I don't think the line needs to start with the text... It could appear mid-paragraph
        // I suppose I need to identify the smallest blocks that would include book info. Hmm, I don't think I can handle the case where there is a paragraph followed by a list...
        // TODO: i should document my assumptions and cases I want handled
        // - Case: line starts with tag
        // - case: note is in the middle of a pharagraph. In this case include the whole paragraph
        // - case: list item starts with tag
        // - case list item or paragraph has a page number before tag
        // - case: paragraph followed by a list that is supposed to be included
        // - case: list item with sub list items 
        var headerText = ToOriginalMarkdown(source, h.Span);
        bool isMatch = keyPhrases.Any(phrase => headerText.ToLower().Contains(phrase));
        return isMatch;
    }
    else{
        return false;
    }
}


// Walk(ast, (MarkdownObject block) => block.Descendants(), (MarkdownObject node) =>{
//     // NOTE: Paragraph blocks are not children of their heading
//     // - I think this means that I don't actually need to do a tree walk, I need to figure out cases I'm interested in that might actually be nested
//     //   - links and sub-lists are nested
//     //h.Inline.First().ToString() seems to be the way to get the header text
//     // h.Level is the heading level
//     //Q: how do I get the sections associated with a heading (probably need an extra function, should seek to the heading then take everything until the next heading of equal or higher level)
//     if(node is HeadingBlock h){
//         Console.WriteLine(h);
//     }
//     if(node is ListBlock l){
//         Console.WriteLine(l);
//     }
// });

var headings = Collect(ast, (MarkdownObject block) => block.Descendants(), IsBookHeading);

var inlineNotes = 

var fullSections = headings.Cast<HeadingBlock>().SelectMany(heading =>GetSectionWithContents(ast, heading)).ToList();

Console.WriteLine($"Hello world! {String.Join(", ", Args)}");
