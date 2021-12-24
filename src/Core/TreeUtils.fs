namespace Notedown.Core
module TreeUtils =
    open System.Collections.Generic 

    type Queue<'T> with
        member self.EnqueueRange(range) = 
            for element in range do
                (self.Enqueue(element))

    let fold getChildren aggFn rootNode initialValue =
        // NOTE: tail recursion optimization seems a bit unreliable in F#, and this has potential for deep nesting. Thus I'm solving this iteratively
        let mutable aggregate = initialValue;
        let mutable unwalked = Queue()
        unwalked.Enqueue(rootNode)
        let mutable walked = Queue()
        
        while not (Seq.isEmpty unwalked) do
            let node = unwalked.Dequeue()
            walked.Enqueue(node)
            unwalked.EnqueueRange (getChildren node)

            aggregate <- aggFn aggregate node
        
        aggregate

    let collect getChildren predicate rootNode = 
        let aggFn collected next = 
            if (predicate next) 
            then List.append collected [next]
            else collected

        fold getChildren aggFn rootNode []
