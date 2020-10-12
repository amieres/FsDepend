type Reader<'T, 'R> = ('R -> 'T)

let swap f a b = f b a

module Reader =
    let inline wrap                         f  =  f : Reader<_,_>
    let inline getFun                       f  =  f 
    let inline ofFun                        f  =  f                                           |> wrap
    let inline rtn                          a  = (fun _ -> a                                ) |> wrap
    let inline bind                       f a  = (fun m -> getFun a m |> f   |> getFun <| m ) |> wrap
    let inline delayRun        f               = (fun m ->               f() |> getFun <| m ) |> wrap
    let inline map             f            m  = bind (f >> rtn) m                             : Reader<_,_>
    let inline apply           fR           vR = fR |> bind (swap map  vR)                     : Reader<_,_>
    let inline run          rsrc             a = getFun a (rsrc: 'R)                           :             'T
    let (>>=)                              v f = bind f v
    let rec    traverseSeq     f            sq = let folder head tail = f head >>= (fun h -> tail >>= (fun t -> List.Cons(h,t) |> rtn))
                                                 Array.foldBack folder (Seq.toArray sq) (rtn List.empty) |> map Seq.ofList
    let inline sequenceSeq                  sq = traverseSeq id sq
    let insertO  vvO                           = vvO  |> Option.map(map Some) |> Option.defaultWith(fun () -> rtn None)
    let insertR (vvR:Result<_,_>)              = vvR  |> function | Error m -> rtn (Error m) | Ok v -> map Ok v
    let insertFst (fst, vRm)                   = vRm  |> map (fun v -> fst, v)
    let insertSnd (vRm, snd)                   = vRm  |> map (fun v -> v, snd)
    let mapResource                       fR v = wrap ( fR >> (v    |> getFun) )
    let inline iter                f t         = run t >> (f: _ -> unit)
    let memoizeRm               getCache fRm p = (fun r -> 
                                                     let checkO, store = getCache r
                                                     checkO p |> Option.defaultWith(fun () -> (fRm p |> getFun) r |> store p)
                                                 ) |> wrap

    type Builder() =
        member inline this.Return      x                  = rtn  x
        member inline this.ReturnFrom  x                  =     (x:Reader<_,_>)
        member        this.Bind       (w , r )            = bind   r w
        member inline this.Zero       ()                  = rtn ()
        member inline this.Delay       f                  = f
        member inline this.Combine    (a, b)              = bind b a
        member inline this.Run         f                  = delayRun f
        member this.While(guard, body) =
            let rec whileLoop guard body =
                if guard() then body() |> bind (fun () -> whileLoop guard body)
                else rtn   ()
            whileLoop guard body
        member this.TryWith   (body, handler     ) = (fun r -> try body() |> run r with e -> handler     e            ) |> wrap
        member this.TryFinally(body, compensation) = (fun r -> try body() |> run r finally   compensation()           ) |> wrap 
        member this.Using     (disposable, body  ) = (fun r -> using (disposable:#System.IDisposable) (body >> run r) ) |> wrap
        member this.For(sequence:seq<_>, body) =
            this.Using(sequence.GetEnumerator(),fun enum -> 
                this.While(enum.MoveNext, 
                    this.Delay(fun () -> body enum.Current)))

    let reader = Builder()
    
    module Operators =
        let inline (<*>) f v   = apply f v
        let inline (|>>) v f   = map   f v
        let inline (>>=) v f   = bind  f v
        let inline (>>>) f g v = f v |>> g
        let inline (>=>) f g v = f v >>= g
        let inline rtn   v     = rtn    v
        let reader = reader
