namespace Notedown.BCLExtensions

module List =
    let headOr list default' =
        match List.tryHead list with
        | Some head -> head
        | None -> default'