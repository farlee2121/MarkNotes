namespace Notedown.Core

open System

module String = 
    let join (separator: string) (strings:string seq) =
        String.Join(separator, strings)

    let joinLines (strings: string seq) = join "\n" strings
    let joinParagraphs (strings: string seq) = join "\n\n" strings
    let split (separator: string) (str: string) = 
        str.Split(separator)

    let trim (s:string) = s.Trim() 

