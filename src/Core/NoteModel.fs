namespace Notedown.Core

open YamlDotNet
open Markdig.Extensions.Yaml
open Markdig
open Markdig.Syntax
open YamlDotNet.RepresentationModel
open System.IO
open System
open YamlDotNet.Serialization
open System.Collections.Generic

type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of StructuralDictionary<string, MetadataValue>

module MetadataValue =
    let default' = Complex StructuralDictionary.empty
    let fromPairs t = sdict t |> Complex 

type HeadingLevel = int
type SectionLevel = 
    | Root
    | Heading of HeadingLevel

type Section = {
    Level: SectionLevel
    Meta: MetadataValue
    ExclusiveText: string
    Children: Section list
}

type Section with
    member this.Text() =
        this.ExclusiveText


module NoteModel =

    module Yaml =

        let nodeToText (node:YamlNode) =
            SerializerBuilder().Build().Serialize(node)

        let extractMapKey (node: YamlNode) =
            match node with
            | :? YamlScalarNode as key -> key.Value
            | _ -> invalidOp $"Cannot extract yaml map key: {node}"

        let mapKvp mapKey mapValue (kvp:KeyValuePair<'key,'value>) =
            KeyValuePair((mapKey kvp.Key), (mapValue kvp.Value))

        let yamlNodeToMetaModel (node: YamlNode) : MetadataValue =
            let rec recurse (node: YamlNode) =
                match node with
                | :? YamlScalarNode as scalar -> scalar.Value |> MetadataValue.SingleValue
                | :? YamlSequenceNode as vec -> vec.Children |> Seq.map recurse |> List.ofSeq |> MetadataValue.Vector
                | :? YamlMappingNode as map ->
                    map.Children |> Seq.map (mapKvp extractMapKey recurse) |> StructuralDictionary |> MetadataValue.Complex 
                | node -> MetadataValue.SingleValue $"Unsupported yaml: {node |> nodeToText}"

            recurse node
        
    
        let internal parseYaml text =
            let yamlStream = YamlStream();
            yamlStream.Load(new StringReader(text))

            let tryMappingNode (n:YamlNode) = match n with :? YamlMappingNode as n -> Some n | _ -> None
            let getRoot (document:YamlDocument)  = document.RootNode
            let yamlRoot =
                yamlStream.Documents
                |> Seq.tryHead
                |> Option.bind (getRoot >> Some)
                |> Option.bind tryMappingNode

            match yamlRoot with
            | Some root -> root |> yamlNodeToMetaModel 
            | None -> MetadataValue.Complex StructuralDictionary.empty


    type private HeadingHierarchy = {
        Heading: HeadingBlock
        Children: HeadingHierarchy list
    }


    let parse (document: string) : Section =
        if (String.IsNullOrEmpty document) then
            {
                Level = SectionLevel.Root
                Meta = sdict [] |> Complex
                ExclusiveText = document
                Children = []
            }
        else

            let pipeline = MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().EnableTrackTrivia().Build()
            let markdownModel = Markdown.Parse(document, pipeline)

            let yamlBlock = markdownModel |> Seq.tryPick tryUnbox<YamlFrontMatterBlock>

            let maybeHeading = markdownModel |> Seq.tryPick tryUnbox<HeadingBlock>
            let maybeYamlBlock = markdownModel |> Seq.tryPick tryUnbox<FencedCodeBlock>


            match maybeHeading with
            | Some heading ->
                let tryGetYamlFromCodeBlock (codeBlock:FencedCodeBlock) =
                    let allowedIdentifiers = set ["yml"; "yaml"]
                    if(allowedIdentifiers.Contains(codeBlock.Info))
                    then
                        codeBlock.Lines |> string |> Some
                    else None

                let maybeMetaText = maybeYamlBlock |> Option.bind tryGetYamlFromCodeBlock
                let parsedMeta =
                    match maybeMetaText with
                    | Some metaYaml -> Yaml.parseYaml metaYaml
                    | None -> MetadataValue.default'
                {
                    Level = SectionLevel.Root
                    Meta = sdict [] |> Complex
                    ExclusiveText = ""
                    Children = [
                        {
                            Level = SectionLevel.Heading 1
                            Meta = parsedMeta
                            ExclusiveText = document
                            Children = []
                        }
                    ]
                }
            | None ->
                let rootMeta =
                    match yamlBlock with
                    | Some ymlBlock -> ymlBlock |> MarkdigExtensions.blockToMarkdownText |> Yaml.parseYaml
                    | None -> MetadataValue.Complex StructuralDictionary.empty

                {
                    Level = SectionLevel.Root
                    Meta = rootMeta
                    ExclusiveText = document
                    Children = []
                }