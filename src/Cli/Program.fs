module Program
open System.CommandLine
open System.IO
open System
open Notedown.Core
open Microsoft.FSharp.Quotations
open System.CommandLine.Binding
open System.CommandLine.Invocation
open System.CommandLine.NamingConventionBinder

module Cli =
    let root description (symbols: Symbol list) =
        let root = new RootCommand(description)
        symbols |> List.map root.Add |> ignore
        root

    let command<'a> name description (symbols: Symbol list) (handler: ICommandHandler) =
        let command = new Command(name, description)
        symbols |> List.map command.Add |> ignore
        
        command.Handler <- handler
        command

    let option<'a> (aliases: string list) description =
        new Option<'a>(aliases = (aliases |> Array.ofList), description = description)

    let argument<'a> name description =
        new Argument<'a>(name = name, description = description)

type TagExtractionOptions(inputFile, tags) =
    member val InputFile: FileInfo = inputFile
    member val Tags: string = tags

let tagExtractionHandler (inputFile:FileInfo) (tags:string) =
    let documentText = File.ReadAllText(inputFile.FullName)
    let extractedContent = TagExtraction.extract [tags] documentText
    // probably write to stdout if they don't specify an output file
    Console.WriteLine (String.joinParagraphs extractedContent)




let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    let root =
       Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
           Cli.command "tag-extract" "Get content (list items, paragraphs, sections, etc) with the given tag" [
               Cli.argument<FileInfo> "input-file" "The file to extact content from"
               Cli.option<string> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
           ] (CommandHandler.Create((fun (inputFile:FileInfo) (tags:String)-> tagExtractionHandler inputFile tags))) // for some reason doesn't work against the F# function
       ] 

    root.SetHandler(showHelp root)
    root.Invoke args