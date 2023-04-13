namespace Notedown.BCLExtensions

open System.Collections.Generic
open System

module internal EquatableDictionaryInternal =
    let kvpEqual (left:KeyValuePair<'key,'value>) (right:KeyValuePair<'key,'value>) =
        left.Key = right.Key
        && left.Value = right.Value

    let equals left right =
        Seq.length left = Seq.length right
        && Seq.forall2 kvpEqual left right

type EquatableDictionary<'key, 'value when 'key:equality and 'value: equality> =
    inherit Dictionary<'key, 'value>

    new (dictionary:IDictionary<'key,'value>) = { inherit Dictionary<'key,'value>(dictionary) }
    new (dictionary:IDictionary<'key,'value>, comparer) = { inherit Dictionary<'key,'value>(dictionary, comparer) }
    new (collection:IEnumerable<KeyValuePair<'key,'value>>) = { inherit Dictionary<'key,'value>(collection) }
    new (collection:IEnumerable<KeyValuePair<'key,'value>>, comparer) = { inherit Dictionary<'key,'value>(collection, comparer) }
    new (keyComparer:IEqualityComparer<'key>) = { inherit Dictionary<'key,'value>(keyComparer) }
    new (capacity:int) = { inherit Dictionary<'key,'value>(capacity) }
    new (capacity:int, comparer) = { inherit Dictionary<'key,'value>(capacity, comparer) }
    new () = { inherit Dictionary<'key, 'value> () }

    

    override this.Equals(other) =
        match other with
        | :? EquatableDictionary<'key, 'value> as other -> 
            EquatableDictionaryInternal.equals this other
        | _ -> false

    override this.GetHashCode() =
        (this |> Seq.map (fun kvp -> (kvp.Key, kvp.Value))).GetHashCode()        

    interface IEquatable<EquatableDictionary<'key,'value>> with
        member this.Equals (other) : bool =
            EquatableDictionaryInternal.equals this other


module EquatableDictionary =
    let empty<'key, 'value when 'key:equality and 'value:equality> = new EquatableDictionary<'key, 'value>()
    let fromDict (dict:IDictionary<'key,'value>) = EquatableDictionary(dict)
    let toPairs (d:EquatableDictionary<'key,'value>) = [for kvp in d -> (kvp.Key, kvp.Value)]
    let fromPairs tupleList = EquatableDictionary(dict tupleList)


[<AutoOpen>]
module _EquatableDictionaryConstructor =
    let sdict tupleList = EquatableDictionary(dict tupleList)