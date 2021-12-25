module Program
open System.CommandLine
open System.CommandLine.Invocation
open System.IO
open System

//module CommandLine =
//    //let rootCommand description = new RootCommand(description)
//    type RootConfig = {
//        Description: string
//        Options: Option list option
//    }

//    type OptionConfig ={
//        Aliases: string list
//        GetDefaultValue: (unit -> obj) option
//        Description: string option
//    }

//    let option<'a> (names: string list) description =
//        new Option<'a>(aliases = (names |> Array.ofList), description = description)
//    //IDEA: probably better to build up a config data structure then make the tree rather than create overloads
//    // that directly create instances with different arguments

//open CommandLine

type TagExtractionOptions(file, tags) =
    member val File: FileInfo = file
    member val Tags: string seq = tags

let tagExtractionHandler (filePath:FileInfo) tag =
    let documentText = File.ReadAllText(filePath.FullName)
    let extractedContent = Notedown.Core.TagExtraction.extract [tag] documentText
    // probably write to stdout if they don't specify an output file
    Console.WriteLine extractedContent


let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    let root = new RootCommand()
    let extractCommand = new Command("tag-extract", "such description")
    //TODO: I really want this to be a directory or pattern
    let fileArg = new Argument<FileInfo>(name = "input file", description = "The input file for extraction")
    let tagArg = new Option<string>(aliases = [|"--tags"; "-t"|])
    extractCommand.AddArgument(fileArg)
    extractCommand.AddOption(tagArg)
    extractCommand.SetHandler(tagExtractionHandler, fileArg, tagArg)
    root.AddCommand(extractCommand);
    root.SetHandler(showHelp root)
    root.Invoke args

    // If I make a builder model, then I'll have to figure out how to match up args/opts with a handler since I won't have the references, maybe look them up by name?
    
