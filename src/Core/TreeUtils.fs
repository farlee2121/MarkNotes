namespace Notedown.Core
module TreeUtils =
    open System.Collections.Generic 

    type Queue<'T> with
        member self.EnqueueRange(range) = 
            for element in range do
                (self.Enqueue(element))

    let fold rootNode getChildren aggFn initialValue =
        // NOTE: tail recursion optimization seems a bit unreliable in F#, and this has potential for deep nesting. Thus I'm solving this iteratively
        let mutable aggregate = initialValue;
        let mutable unwalked = Queue()
        unwalked.Enqueue(rootNode)
        
        while not (Seq.isEmpty unwalked) do
            let node = unwalked.Dequeue()
            unwalked.EnqueueRange (getChildren node)

            aggregate <- aggFn aggregate node
        
        aggregate
