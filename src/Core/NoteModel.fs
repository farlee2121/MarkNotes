namespace Notedown.Core


open Markdig
open YamlDotNet.RepresentationModel
open System.IO
open System
open YamlDotNet.Serialization
open System.Collections.Generic
open Notedown.BCLExtensions
open Notedown.Internals.MarkdigSectionModel


type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of EquatableDictionary<string, MetadataValue>

module MetadataValue =
    let default' = Complex EquatableDictionary.empty
    let fromPairs t = sdict t |> Complex

    let merge (target:MetadataValue) (overrides:MetadataValue) =

        match (target, overrides) with
        | (Complex t, Complex o) ->
            (Seq.append t o) |> EquatableDictionary |> Complex
        | (Vector t, Vector o) -> Vector o
        | (_, over) -> over

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

module Section =
    /// Forward traverse section with a tracked state. Can modify sections while preserving structure/hierarchy
    let mapFold (mapf: 'state -> Section -> (Section*'state)) (state: 'state) (section: Section) : (Section *'state) =
        let rec recurse state section =
            let mapped, state = mapf state section 
            let childrenMapped, state = List.mapFold recurse state mapped.Children
            {mapped with Children = childrenMapped}, state
        recurse state section

    let fullText (section:Section) = 
        let mapf state (section:Section) = (section, section.ExclusiveText :: state)
        let _,textInReverseOrder = mapFold mapf [] section

        let textInOrder = textInReverseOrder |> List.rev

        let trimRootIfEmpty sectionTexts =
            // if the root document is empty it causes an extra newline when joined
            // so remove it
            match sectionTexts with
            | "" :: tail -> tail
            | l -> l

        textInOrder |> trimRootIfEmpty |> String.joinLines
 
type Section with
    member this.FullText() =
        Section.fullText this


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
                    map.Children |> Seq.map (mapKvp extractMapKey recurse) |> EquatableDictionary |> MetadataValue.Complex 
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
            | None -> MetadataValue.Complex EquatableDictionary.empty


    let parse (document: string) : Section =

        let trimEndSingle (s:string) = 
                if s.EndsWith("\n")
                then s.Substring(0, s.Length-1)
                else s

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
            

            let mapSection (node:HeadingHierarchy) (children:Section list) =
                let maybeMeta = node.MetaBlock |> Option.map (MetaBlock.toYamlText >> Yaml.parseYaml) 
                {
                    Level =
                        match node.Level with
                        | 0 -> SectionLevel.Root
                        | n -> SectionLevel.Heading n
                    Meta = maybeMeta |> Option.defaultValue MetadataValue.default'
                    ExclusiveText = node.SourceSpan |> MarkdigExtensions.spanToText document |> trimEndSingle
                    Children = children
                }


            let headingHierarchy =
                markdownModel
                |> MarkdigSectionModel.extractSectionHierarchy

            let sections =
                headingHierarchy
                |> MarkdigSectionModel.cata mapSection

            sections