module Program
open System.CommandLine
open System.IO
open System
open Notedown.Core
open System.CommandLine.Invocation
open System.CommandLine.PropertyMapBinder
open Microsoft.Extensions.FileSystemGlobbing


[<CLIMutable>]
type TagExtractionOptions = {
    InputFilePattern: string;
    Tags: string list
    OutputFile: FileInfo option
    DocumentOutputSeparator: string option
    SourceMapFormat: string option
}

module String =
    let replaceMany (fromToPairs: (string*string) list) (str: string) =
        let replace (s:string) (from:string, to':string) = s.Replace(from, to')
        fromToPairs |> List.fold (replace) str

let specialCharMap = [("\\n","\n"); ("\\t", "\\t");]
let enableSpecialChars = 
    String.replaceMany specialCharMap

let escapeSpecialChars = 
    let tupleReverse (a,b) = (b,a)
    String.replaceMany (specialCharMap |> List.map tupleReverse)

let interpolateFileInfo (path:string) targetString = 
    String.replaceMany [("{source_name}", Path.GetFileName(path)); ("{source_path}", path)] targetString
    
let defaultSourceMapFormat = "```yml\n source: \"{source_path}\"\n``` \n\n"
let defaultOutputSeparator = "\n\n---\n\n";

let tagExtractionHandler (extractionOptions:TagExtractionOptions) =

    let patternToPaths (pathPattern:string) =
        let fileMatcher = new Matcher()
        if (Path.IsPathFullyQualified(pathPattern))
        then seq {pathPattern}
        else fileMatcher.AddInclude(extractionOptions.InputFilePattern)
                            .GetResultsInFullPath(Directory.GetCurrentDirectory())

    let extractSingleDocument inputFilePath =
        let documentText = File.ReadAllText(inputFilePath)
        let extractedContent = TagExtraction.extract extractionOptions.Tags documentText
        extractedContent

    let formatSingleFileExtactions (sourceMapFormat:string option) (path:string, extracted) =  
        let sourceMapFormat = (Option.defaultValue defaultSourceMapFormat sourceMapFormat) |> enableSpecialChars
        $"{sourceMapFormat |> interpolateFileInfo path}{String.joinParagraphs extracted}"

    let inputFilePaths = patternToPaths extractionOptions.InputFilePattern
    let extractedContents = 
        inputFilePaths 
        |> Seq.map (fun path -> (path, extractSingleDocument path))
        |> Seq.filter (fun (_, extracted)-> not (List.isEmpty extracted))

    let documentOutputSeparator = (Option.defaultValue defaultOutputSeparator extractionOptions.DocumentOutputSeparator) |> enableSpecialChars
    let joinedOutput = String.join documentOutputSeparator (extractedContents |> Seq.map (formatSingleFileExtactions extractionOptions.SourceMapFormat))

    match extractionOptions.OutputFile with
    | None -> Console.WriteLine (joinedOutput)
    | Some f -> File.WriteAllText(f.FullName, joinedOutput) 


let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    let root =
       Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
           Cli.commandMap {
                Name = "extract-tags"
                Description = Some "Get content (list items, paragraphs, sections, etc) with the given tag"
                Inputs = [
                    Cli.argument<string> "input-file-pattern" "File(s) to extract tagged data from. A file path or glob pattern."
                    Cli.option<string seq> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
                     |> Cli.withArity ArgumentArity.OneOrMore
                    Cli.option<FileInfo> ["--output"; "-o"] "File to write extracted content to. Will overwrite if it already exists."
                    Cli.option<string> ["--output-separator"] $"Used to delineate extracted output from each input file. Default is {escapeSpecialChars defaultOutputSeparator}"
                    Cli.option<string> ["--source-map-format"] $"Format for showing source file in output. Supports {{source_name}} and {{source_path}} variables.\nDefault prints a yaml block with the source path \n {escapeSpecialChars defaultSourceMapFormat}"
                ]
                Children = None
                Handler = (Cli.CommandHandler.fromPropertyMap [
                    (Cli.PropertyMap.nameAndSetter "--tags" (fun model input -> { model with Tags = List.ofSeq input}))
                    (Cli.PropertyMap.nameAndSetter "input-file-pattern" (fun model input -> { model with InputFilePattern = input }))
                    (Cli.PropertyMap.nameAndSetter "-o" (fun model input ->  {model with OutputFile = Option.ofObj input }))
                    (Cli.PropertyMap.nameAndSetter "--output-separator" (fun model input ->  {model with DocumentOutputSeparator = Option.ofObj input }))
                    (Cli.PropertyMap.nameAndSetter "--source-map-format" (fun model input ->  {model with SourceMapFormat = Option.ofObj input }))
                 ] tagExtractionHandler)
           }    
       ] 

    root.SetHandler(showHelp root)
    root.Invoke args