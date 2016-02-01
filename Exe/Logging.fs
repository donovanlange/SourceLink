// derived from https://github.com/fsprojects/Paket/blob/master/src/Paket.Core/Logging.fs
[<AutoOpen>]
module SourceLink.Logging

open System
open System.Diagnostics

let mutable verbose = false

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
let traceToConsole (t:Trace) =
    let prnt =
        let stdout = if t.NewLine then printfn else printf
        let stderr = if t.NewLine then eprintfn else eprintf
        match t.Level with
        | TraceLevel.Warning -> stderr
        | TraceLevel.Error -> stderr
        | _ -> stdout
    prnt "%s" t.Text