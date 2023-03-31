namespace Notedown.Core

open System.Collections.Generic
open System

module internal StructuralDictionaryInternal =
    let kvpEqual (left:KeyValuePair<'key,'value>) (right:KeyValuePair<'key,'value>) =
        left.Key = right.Key
        && left.Value = right.Value

    let equals left right =
        Seq.forall2 kvpEqual left right

type StructuralDictionary<'key, 'value when 'key:equality and 'value: equality> =
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
        | :? StructuralDictionary<'key, 'value> as other -> 
            StructuralDictionaryInternal.equals this other
        | _ -> false

    override this.GetHashCode() =
        (this |> Seq.map (fun kvp -> (kvp.Key, kvp.Value))).GetHashCode()        

    interface IEquatable<StructuralDictionary<'key,'value>> with
        member this.Equals (other) : bool =
            StructuralDictionaryInternal.equals this other


module StructuralDictionary =
    let empty<'key, 'value when 'key:equality and 'value:equality> = new StructuralDictionary<'key, 'value>()
    let fromDict (dict:IDictionary<'key,'value>) = StructuralDictionary(dict)
