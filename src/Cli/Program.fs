module Program
open System.CommandLine
open System.IO
open System
open Notedown.Core
open System.CommandLine.Invocation
open System.CommandLine.PropertyMapBinder
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
    
    let withArity<'a> (arity: ArgumentArity) (opt: Option<'a>) =
        opt.Arity <- arity
        opt

    let argument<'a> name description =
        new Argument<'a>(name = name, description = description)

    module CommandHandler = 
        let fromPropertyMap (binders: IPropertyBinder<'a> list) (handler: 'a -> 'b) : ICommandHandler =
            CommandHandler.FromPropertyMap (handler, (new BinderPipeline<'a>(binders)))

    module PropertyMap =
        open Microsoft.FSharp.Quotations
        open Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter
        let private toLinq = QuotationToLambdaExpression

[<CLIMutable>]
type TagExtractionOptions = {
    InputFilePattern: string;
    Tags: string list
    OutputFile: FileInfo option
}

let tagExtractionHandler (extractionOptions:TagExtractionOptions) =
    let fileMatcher = new Matcher()
    let inputFilePaths =
        if (Path.IsPathFullyQualified(extractionOptions.InputFilePattern))
        then seq {extractionOptions.InputFilePattern}
        else fileMatcher.AddInclude(extractionOptions.InputFilePattern)
                            .GetResultsInFullPath(Directory.GetCurrentDirectory())

    let extractSingleDocument inputFilePath =
        let documentText = File.ReadAllText(inputFilePath)
        let extractedContent = TagExtraction.extract extractionOptions.Tags documentText
        extractedContent

    //IDEA: it'd probably be a good idea to include the source file name at the start of each
    let extractedContents = 
        inputFilePaths 
        |> Seq.map (fun path -> (path, extractSingleDocument path))
        |> Seq.filter (fun (_, extracted)-> not (List.isEmpty extracted))

    let formatSingleFileExtactions (path:string, extracted) =  
        $"```yml\n source: \"{path}\"\n``` \n\n{String.joinParagraphs extracted}"

    let documentOutputSeparator = "\n\n---\n\n"
    let joinedOutput = String.join documentOutputSeparator (extractedContents |> Seq.map formatSingleFileExtactions)

    match extractionOptions.OutputFile with
    // Write to stdout if they don't specify an output file
    | None -> Console.WriteLine (joinedOutput)
    | Some f -> File.WriteAllText(f.FullName, joinedOutput) 




let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    let root =
       Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
           Cli.command "extract-tags" "Get content (list items, paragraphs, sections, etc) with the given tag" [
               Cli.argument<string> "input-file-pattern" "File(s) to extract tagged data from. A file path or glob pattern."
               Cli.option<string seq> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
                |> Cli.withArity ArgumentArity.OneOrMore
               Cli.option<FileInfo> ["--output"; "-o"] "File to write extracted content to. Will overwrite if it already exists."
           ] (Cli.CommandHandler.fromPropertyMap [
               (PropertyMap.FromName ("--tags", setter = (fun model input -> { model with Tags = (List.ofSeq input)})))
               (PropertyMap.FromName ("input-file-pattern", selectorLambda = (fun (model) -> model.InputFilePattern)))
               (PropertyMap.FromName ("-o", setter = (fun model (input:FileInfo) -> 
                                                                    match input with
                                                                    | null -> {model with OutputFile = None}
                                                                    | _ -> {model with OutputFile = Some input})))
            ] tagExtractionHandler)
       ] 

    root.SetHandler(showHelp root)
    root.Invoke args