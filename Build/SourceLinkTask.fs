namespace SourceLink

open Argu
open SourceLink
open SourceLink.Commands
open System
open System.IO
open System.Text
open Microsoft.Build.Framework
open System.Collections.Generic

type SourceLinkTask() =
    inherit Task()

    member val Verbose = String.Empty with set, get

    member val Url = String.Empty with set, get

    [<Required>]
    member val ProjectDirectory = String.Empty with set, get

    [<Required>]
    member val Sources = Array.empty<string> with set, get

    [<Required>]
    member val TargetPath = String.Empty with set, get

    override x.Execute() =
        
        let verbose = not <| String.IsNullOrEmpty x.Verbose

        let globalArgs =
            let parser = ArgumentParser.Create<GlobalArgs>()
            [
                if verbose then
                    yield GlobalArgs.Verbose
            ]
            |> parser.PrintCommandLine

        let indexArgs =
            let parser = ArgumentParser.Create<IndexArgs>()
            [
                yield IndexArgs.Not_Verify_Pdb
                yield IndexArgs.Not_Pdbstr
                yield IndexArgs.Pdb (Path.ChangeExtension(x.TargetPath, ".pdb"))
                yield IndexArgs.Url x.Url
                for source in x.Sources do
                    yield IndexArgs.File source
            ]
            |> parser.PrintCommandLine

        let args = Array.append globalArgs indexArgs
            
        let exe = Path.combine (System.Reflection.Assembly.GetExecutingAssembly().Location |> Path.GetDirectoryName) "SourceLink.exe"

        let psSpecialChars = [| '%' |] // PowerShell special characters
        let psNeedsQuotes (s: string) = s.IndexOfAny psSpecialChars <> -1
        let arguments =
            let sb = StringBuilder()
            sb.Appendf "index"
            for arg in args do
                if psNeedsQuotes arg then
                    sb.Appendf " \"%s\"" arg
                else sb.Appendf " %s" arg
            sb.ToString()

        let mutable hasWarnings = false
        let p = Process()
        p.FileName <- exe
        p.Arguments <- arguments
        p.WorkingDirectory <- x.ProjectDirectory // x.Sources are relative
        let out = StringBuilder()
        let err = StringBuilder()
        p.Stdout |> Observable.add (fun s ->
            out.Appendf "%s\n" s
        )
        p.Stderr |> Observable.add (fun s ->
//            out.Appendf "%s\r\n" s
//            err.Appendf "%s\r\n" s
            hasWarnings <- true
        )
        let cmd = sprintf "%s> . '%s' %s" p.WorkingDirectory p.FileName p.Arguments
        if verbose then
            x.MessageHigh "%s" cmd
        else
            x.MessageNormal "%s" cmd
        try
            let exit = p.Run()
            let outStr = out.ToString()
            let errStr = err.ToString()
            if exit <> 0 then 
//                x.MessageHigh "sourcelink: error SL101: %s" errStr
                x.MessageHigh "sourcelink: error C1003: SourceLink error."
            else
                if verbose then
                    x.MessageHigh "%s" outStr
                else
                    x.MessageNormal "%s" outStr
                if hasWarnings then
//                    x.MessageHigh "sourcelink: warning SL100: %s" errStr
                    x.MessageHigh "sourcelink: warning C1004: SourceLink warning."
        with
            | ex -> 
                x.MessageHigh "sourcelink: error C1002: %s" ex.Message
                x.MessageHigh "%s" cmd
                x.MessageHigh "%s" (out.ToString())
                x.Error "SourceLink failed. See build output for details."

        not x.HasErrors