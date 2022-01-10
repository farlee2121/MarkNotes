namespace System.CommandLine

open System.CommandLine
open System.CommandLine.PropertyMapBinder
open System.CommandLine.Invocation
open System

module Option =
    let ofObj obj = 
        match obj with
        | null -> None
        | _ -> Some obj

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
        open System.Linq.Expressions
        
        let nameAndSetter name (setter: 'a -> 'b -> 'a) = 
            PropertyMap.FromName(name, setter = setter)

