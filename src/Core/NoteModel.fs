namespace Notedown


open Markdig
open YamlDotNet.RepresentationModel
open System.IO
open System
open YamlDotNet.Serialization
open System.Collections.Generic
open Notedown.Internal.BCLExtensions
open Notedown.Internal.MarkdigSectionModel


type MetadataValue =
    | SingleValue of string
    | Vector of MetadataValue list
    | Complex of EquatableDictionary<string, MetadataValue>
with
    static member default' = Complex EquatableDictionary.empty

module MetadataValue =
    let fromPairs t = sdict t |> Complex

    let merge (target:MetadataValue) (overrides:MetadataValue) =
        let rec recurse (target:MetadataValue) overrides =
            match (target, overrides) with
            | (Complex t, Complex o) ->
                let merged = EquatableDictionary(t)
                for key in o.Keys do
                    if(merged.ContainsKey(key))
                    then merged[key] <- recurse t[key] o[key]
                    else merged[key] <- o[key]

                Complex merged
            | (Vector t, Vector o) -> Vector o
            | (_, over) -> over

        recurse target overrides


    module Selector =
        let private separator = "."
        let segments selector = selector |> String.split separator
        let join segments = segments |> String.join separator

    /// Get the metadata value at the given path if it exists
    /// Paths are . delimited. I.e `root.config.some-nested-property`
    let trySelect (selector: string) (meta :MetadataValue): MetadataValue option =
        let rec recurse relativePath relativeMeta =
            match relativePath, relativeMeta with
            | [], meta -> Some meta
            | localSelector::remainingPath, Complex dict ->
                if dict.ContainsKey localSelector then
                    recurse remainingPath dict[localSelector]
                else
                    None     
            | _ -> None

        if selector = ""
        then Some meta
        else 
            let pathSegments = selector |> Selector.segments |> List.ofArray
            recurse pathSegments meta

    /// Get the metadata value at the given path if it exists and is a simple value
    /// Paths are . delimited. I.e `root.config.some-nested-property`
    let trySelectSingle (selector: string) (meta :MetadataValue) : string option =
        match trySelect selector meta with
        | Some (SingleValue metaVal) -> Some metaVal
        | _ -> None

    /// Set the value at a given path, creating the path if it doesn't exist and overwriting any mid-level values if necessary.
    /// For example, if you have config `{routes: ["r1", "r2"]}` and you call clobber with path "routes.home",
    /// it will overwrite the config to `{ routes: { home: given-value }}`
    let clobber (selector: string) (meta: MetadataValue) (value: MetadataValue) =
        let rec recurse relativePath relativeMeta =
            match relativePath, relativeMeta with
            | [], meta -> value
            | localSelector::remainingPath, Complex dict ->
                if dict.ContainsKey localSelector then
                    dict[localSelector] <- recurse remainingPath dict[localSelector]
                    Complex dict
                else
                    dict[localSelector] <- recurse remainingPath MetadataValue.default'
                    Complex dict
            | localSelector::remainingPath, _ ->
                let dict = EquatableDictionary.empty
                dict[localSelector] <- recurse remainingPath MetadataValue.default'
                Complex dict

        if selector = ""
        then value
        else
            let pathSegments = selector |> Selector.segments |> List.ofArray
            recurse pathSegments meta

    type OverwriteLocation = { Path: string; Value: MetadataValue}
    type SetFailureReason = | WouldOverwrite of OverwriteLocation
    let trySet (selector: string) (meta: MetadataValue) (value: MetadataValue) : Result<MetadataValue,SetFailureReason>  =
        let rec recurse heritage relativePath relativeMeta =
            match relativePath, relativeMeta with
            | [], meta -> Ok value
            | localSelector::remainingPath, Complex dict ->
                let recurseMeta = if dict.ContainsKey localSelector then dict[localSelector] else MetadataValue.default'
                recurse (localSelector::heritage) remainingPath recurseMeta
                |> Result.map (fun updatedChild ->
                    dict[localSelector] <- updatedChild
                    Complex dict
                )
            | localSelector::remainingPath, localValue ->
                let path = heritage |> List.rev |> Selector.join
                Error (WouldOverwrite {Path =path;  Value = localValue})

        if selector = ""
        then Error (WouldOverwrite {Path = ""; Value = meta})
        else
            let pathSegments = selector |> Selector.segments |> List.ofArray
            recurse [] pathSegments meta



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


    module Inheritance =
        let forwardInheritance (root:Section) =
            let mergeMeta prevMeta currentSection =
                let merged = MetadataValue.merge prevMeta currentSection.Meta
                { currentSection with Meta = merged}, merged

            let (mapped, _) = Section.mapFold mergeMeta MetadataValue.default' root
            mapped

        let parentChild (root:Section) =
            let rec recurse (section:Section) : Section =
                let mergeMeta child =
                    let merged = MetadataValue.merge section.Meta child.Meta
                    { child with Meta = merged }
                let mappedChildren = section.Children |> List.map (mergeMeta >> recurse)
                { section with Children = mappedChildren }

            recurse root


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