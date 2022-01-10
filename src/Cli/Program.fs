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

    let enableSpecialStringChars (str:string) = 
        let specialPairs = [("\\n","\n"); ("\\t", "\\t"); ("\\s", "\s")]
        let replace (s:string) (from:string, to':string) = s.Replace(from, to')
        specialPairs |> List.fold (replace) str

    let documentOutputSeparator = (Option.defaultValue "\n\n---\n\n" extractionOptions.DocumentOutputSeparator) |> enableSpecialStringChars
    let joinedOutput = String.join documentOutputSeparator (extractedContents |> Seq.map formatSingleFileExtactions)

    match extractionOptions.OutputFile with
    // Write to stdout if they don't specify an output file
    | None -> Console.WriteLine (joinedOutput)
    | Some f -> File.WriteAllText(f.FullName, joinedOutput) 




let showHelp (command:Command) () =
    command.Invoke([|"--help"|]) |> ignore 

[<EntryPoint>]
let main args =
    printfn "%A" args
    let root =
       Cli.root "Notedown is a set of conventions for notes in Markdown. This cli provides tools for treating such notes as data" [
           Cli.command "extract-tags" "Get content (list items, paragraphs, sections, etc) with the given tag" [
               Cli.argument<string> "input-file-pattern" "File(s) to extract tagged data from. A file path or glob pattern."
               Cli.option<string seq> ["--tags"; "-t"] "One or more tags marking content to extract (e.g. 'BOOK:', 'TODO:')"
                |> Cli.withArity ArgumentArity.OneOrMore
               Cli.option<FileInfo> ["--output"; "-o"] "File to write extracted content to. Will overwrite if it already exists."
               Cli.option<string> ["--output-separator"] "Used to delineate extracted output from each input file. Default is \\n\\n---\\n\\n"
           ] (Cli.CommandHandler.fromPropertyMap [
               (Cli.PropertyMap.nameAndSetter "--tags" (fun model input -> { model with Tags = List.ofSeq input}))
               (Cli.PropertyMap.nameAndSetter "input-file-pattern" (fun model input -> { model with InputFilePattern = input }))
               (Cli.PropertyMap.nameAndSetter "-o" (fun model input ->  {model with OutputFile = Option.ofObj input }))
               (Cli.PropertyMap.nameAndSetter "--output-separator" (fun model input ->  {model with DocumentOutputSeparator = Option.ofObj input }))
            ] tagExtractionHandler)
       ] 

    root.SetHandler(showHelp root)
    root.Invoke args