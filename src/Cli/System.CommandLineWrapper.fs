namespace System.CommandLine

open System.CommandLine
open System.CommandLine.PropertyMapBinder
open System.CommandLine.Invocation
open System
open System.CommandLine.PropertyMapBinder.PropertyBinders

module Option =
    let ofObj obj = 
        match obj with
        | null -> None
        | _ -> Some obj

module Cli =
    type Command' = {
        Name : string
        Description : string option
        Inputs : Symbol list
        Children : Symbol list option
        Handler : ICommandHandler
    }

    let addSymbol (command: Command) (symbol:Symbol) =
        match symbol with
        | :? Option as opt -> command.Add(opt)
        | :? Argument as opt -> command.Add(opt)
        | :? Command as opt -> command.Add(opt)
        | _ -> invalidArg (nameof symbol) $"Unsupported symbol type. Value { symbol }"

    let root description (symbols: Symbol list) =
        let root = new RootCommand(description)
        symbols |> List.map (addSymbol root) |> ignore
        root

    let command name description (symbols: Symbol list) (handler: ICommandHandler) =
        let command = new Command(name, description)
        symbols |> List.map (addSymbol command) |> ignore
        
        command.Handler <- handler
        command

    let commandMap (map: Command') =
        let command = new Command(map.Name, (Option.defaultValue null map.Description))
        command.Handler = map.Handler |> ignore
        let allSymbols = List.append map.Inputs (Option.defaultValue [] map.Children)
        allSymbols |> List.map (addSymbol command) |> ignore

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
            (new BinderPipeline<'a>(binders)).ToHandler(handler)

    module PropertyMap =
        
        let nameAndSetter name (setter: 'a -> 'b -> 'a) = 
            new SymbolNamePropertyBinder<'a, 'b>(name, propertySetter = setter) :> IPropertyBinder<'a>

