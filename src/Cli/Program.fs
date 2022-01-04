module Program
open System.CommandLine
open System.IO
open System
open Notedown.Core
open System.CommandLine.Invocation
open System.CommandLine.NamingConventionBinder
open Microsoft.Extensions.FileSystemGlobbing

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

let tagExtractionHandler (inputFilePattern:string) (tags:string) =
    let fileMatcher = new Matcher()
    let inputFilePaths =
        if (Path.IsPathFullyQualified(inputFilePattern))
        then seq {inputFilePattern}
        else fileMatcher.AddInclude(inputFilePattern)
                            .GetResultsInFullPath(Directory.GetCurrentDirectory())

    let extractSingleDocument inputFilePath =
        let documentText = File.ReadAllText(inputFilePath)
        let extractedContent = TagExtraction.extract [tags] documentText
        extractedContent

    //IDEA: it'd probably be a good idea to include the source file name at the start of each
    let extractedContents = inputFilePaths |> Seq.map (extractSingleDocument >> String.joinParagraphs)

    let documentOutputSeparator = "\n---\n"
    let joinedOutput = String.join documentOutputSeparator extractedContents

    // Write to stdout if they don't specify an output file
    Console.WriteLine (joinedOutput)




let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    let root =
       Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
           Cli.command "tag-extract" "Get content (list items, paragraphs, sections, etc) with the given tag" [
               Cli.argument<string> "input-file-pattern" "File(s) to extract tagged data from. A file path or glob pattern."
               Cli.option<string> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
           ] (CommandHandler.Create((fun (inputFilePattern:string) (tags:String)-> tagExtractionHandler inputFilePattern tags))) // for some reason doesn't work against the F# function
       ] 

    root.SetHandler(showHelp root)
    root.Invoke args