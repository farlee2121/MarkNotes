module Program
open System.CommandLine
open System.IO
open System
open Notedown.Core
open Microsoft.FSharp.Quotations

module Cli =
    let root description (symbols: Symbol list) =
        let root = new RootCommand(description)
        symbols |> List.map root.Add |> ignore
        root

    let inline command<'a> name description (symbols: Symbol list) =
        let command = new Command(name, description)
        symbols |> List.map command.Add |> ignore
        // command.SetHandler(handler, symbols = (symbols :> Binding.IValueDescriptor[]) )
        command

    let option<'a> (aliases: string list) description =
        new Option<'a>(aliases = (aliases |> Array.ofList), description = description)

    let argument<'a> name description =
        new Argument<'a>(name = name, description = description)

type TagExtractionOptions(inputFile, tags) =
    member val InputFile: FileInfo = inputFile
    member val Tags: string = tags

let tagExtractionHandler (fileInput:FileInfo) tags =
    let documentText = File.ReadAllText(fileInput.FullName)
    let extractedContent = Notedown.Core.TagExtraction.extract [tags] documentText
    // probably write to stdout if they don't specify an output file
    Console.WriteLine (String.joinParagraphs extractedContent)

//let tagExtractionHandler (opts:TagExtractionOptions) =
//    let documentText = File.ReadAllText(opts.InputFile.FullName)
//    let extractedContent = Notedown.Core.TagExtraction.extract [opts.Tags] documentText
//    // probably write to stdout if they don't specify an output file
//    Console.WriteLine (String.joinParagraphs extractedContent)



let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    // let root =
    //    Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
    //        (Cli.command "tag-extract" "Get content (list items, paragraphs, sections, etc) with the given tag" [
    //            Cli.argument<FileInfo> "input-file" "The file to extact content from"
    //            Cli.option<FileInfo> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
    //        ]).SetHandler (tagExtractionHandler, symbols = [||])
    //    ]


    let root = new RootCommand()
    let extractCommand = new Command("tag-extract", "such description")
    //TODO: I really want this to be a directory or pattern
    let fileArg = new Argument<FileInfo>(name = "input-file", description = "The input file for extraction")
    let tagArg = new Option<string>(aliases = [|"--tags"; "-t"|])
    extractCommand.AddArgument(fileArg)
    extractCommand.AddOption(tagArg)
    extractCommand.SetHandler(tagExtractionHandler, fileArg, tagArg)
    root.AddCommand(extractCommand);
    root.SetHandler(showHelp root)
    root.Invoke args