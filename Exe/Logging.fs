// derived from https://github.com/fsprojects/Paket/blob/master/src/Paket.Core/Logging.fs
[<AutoOpen>]
module SourceLink.Logging

open System
open System.Diagnostics

let mutable verbose = false

let monitor = new Object()

type Trace = {
    Level: TraceLevel
    Text: string
    NewLine: bool }

let event = Event<Trace>()

let tracen s = event.Trigger { Level = TraceLevel.Info; Text = s; NewLine = true }
let tracefn fmt = Printf.ksprintf tracen fmt
let trace s = event.Trigger { Level = TraceLevel.Info; Text = s; NewLine = false }
let tracef fmt = Printf.ksprintf trace fmt
let traceVerbose s =
    if verbose then
        event.Trigger { Level = TraceLevel.Verbose; Text = s; NewLine = true }
let verbosefn fmt = Printf.ksprintf traceVerbose fmt
let traceError s = event.Trigger { Level = TraceLevel.Error; Text = s; NewLine = true }
let traceWarn s = event.Trigger { Level = TraceLevel.Warning; Text = s; NewLine = true }
let traceErrorfn fmt = Printf.ksprintf traceError fmt
let traceWarnfn fmt = Printf.ksprintf traceWarn fmt


// Console Trace

let traceColored color (s:string) = 
    let curColor = Console.ForegroundColor
    if curColor <> color then Console.ForegroundColor <- color
    let textWriter = 
        match color with
        | ConsoleColor.Yellow -> Console.Error
        | ConsoleColor.Red -> Console.Error
        | _ -> Console.Out
    textWriter.WriteLine s
    if curColor <> color then Console.ForegroundColor <- curColor

let traceToConsole (trace:Trace) =
    lock monitor
        (fun () ->
            match trace.Level with
            | TraceLevel.Warning -> traceColored ConsoleColor.Yellow trace.Text
            | TraceLevel.Error -> traceColored ConsoleColor.Red trace.Text
            | _ ->
                if trace.NewLine then Console.WriteLine trace.Text
                else Console.Write trace.Text )