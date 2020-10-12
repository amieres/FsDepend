module Depend
    open Microsoft.FSharp.Quotations.Patterns 
    open Microsoft.FSharp.Quotations.DerivedPatterns 

    let pname (prop  :System.Reflection.PropertyInfo) = prop  .DeclaringType.FullName + "." + prop  .Name
    let mname (method:System.Reflection.MethodInfo  ) = method.DeclaringType.FullName + "." + method.Name

    let shouldBe q = 
        let rec shouldBe l =
            function
            | Call(_, method, _) -> Some(mname method, l)
            | Lambda(a, q2)      -> shouldBe (l + 1) q2
            | _ -> None
        match q with
        | PropertyGet(o,                                          prop  , [         ]     )-> pname prop  , "should be 0"
        | Lambda(a,                                    Call(None, method, [         ])    )
        | Lambda(a,                                    Call(None, method, [_        ])    )-> mname method, "should be 1"
        | Lambda(a,Lambda(b,                           Call(None, method, [p;q      ]))   )-> mname method, "should be 2"
        | Lambda(a,Lambda(b,Lambda(c,                  Call(None, method, [p;q;r    ])))  )-> mname method, "should be 3"
        | Lambda(a,Lambda(b,Lambda(c,Lambda(d,         Call(None, method, [p;q;r;s  ])))) )-> mname method, "should be 4"
        | Lambda(a,Lambda(b,Lambda(c,Lambda(d,Lambda(e,Call(None, method, [p;q;r;s;t]))))))-> mname method, "should be 5"
        | q -> 
            shouldBe 0 q 
            |> Option.map(fun (nm,l) -> nm, sprintf "Not covered %d parms: %A" l q) 
            |> Option.defaultWith(fun () -> "?", sprintf "Not covered: %A" q)
        |> fun (nm, m) -> failwithf " %s : %s" nm m

    let getName0(q:Quotations.Expr<                    'T>) : string *                      'T = 
        match q with
        | PropertyGet(o,                                          prop  , [         ]     )-> pname prop  ,                  prop.GetValue(null, [|         |]) |> unbox 
        |_-> shouldBe q
    let getName1(q:Quotations.Expr<'a                ->'T>) : string * ('a                ->'T) = 
        match q with
        | Lambda(a,                                    Call(None, method, [         ])    )-> mname method, fun a         -> method.Invoke(null, [|         |]) |> unbox 
        | Lambda(a,                                    Call(None, method, [p        ])    )-> mname method, fun a         -> method.Invoke(null, [|a        |]) |> unbox 
        |_-> shouldBe q
    let getName2(q:Quotations.Expr<'a->'b            ->'T>) : string * ('a->'b            ->'T) = 
        match q with
        | Lambda(a,Lambda(b,                           Call(None, method, [p;q      ]))   )-> mname method, fun a b       -> method.Invoke(null, [|a;b      |]) |> unbox
        |_-> shouldBe q
    let getName3(q:Quotations.Expr<'a->'b->'c        ->'T>) : string * ('a->'b->'c        ->'T) = 
        match q with
        | Lambda(a,Lambda(b,Lambda(c,                  Call(None, method, [p;q;r    ])))  )-> mname method, fun a b c     -> method.Invoke(null, [|a;b;c    |]) |> unbox
        |_-> shouldBe q
    let getName4(q:Quotations.Expr<'a->'b->'c->'d    ->'T>) : string * ('a->'b->'c->'d    ->'T) = 
        match q with
        | Lambda(a,Lambda(b,Lambda(c,Lambda(d,         Call(None, method, [p;q;r;s  ])))) )-> mname method, fun a b c d   -> method.Invoke(null, [|a;b;c;d  |]) |> unbox
        |_-> shouldBe q
    let getName5(q:Quotations.Expr<'a->'b->'c->'d->'e->'T>) : string * ('a->'b->'c->'d->'e->'T) = 
        match q with
        | Lambda(a,Lambda(b,Lambda(c,Lambda(d,Lambda(e,Call(None, method, [p;q;r;s;t]))))))-> mname method, fun a b c d e -> method.Invoke(null, [|a;b;c;d;e|]) |> unbox
        |_-> shouldBe q

    type Depend<'T> = 
    | Dependency of (string * obj) option * (obj -> Depend<'T>)
    | NoMore     of 'T

    let dependByName nm (defF:'f) (kf:'f->'T) = Dependency(Some(nm, box defF), fun f -> NoMore (kf (unbox f)) )

    let depend0   q                                                                      = let (nm, f) = getName0 q in dependByName nm f id
    let depend1   q                                                                      = let (nm, f) = getName1 q in dependByName nm f id
    let depend2   q                                                                      = let (nm, f) = getName2 q in dependByName nm f id
    let depend3   q                                                                      = let (nm, f) = getName3 q in dependByName nm f id
    let depend4   q                                                                      = let (nm, f) = getName4 q in dependByName nm f id
    let depend5   q                                                                      = let (nm, f) = getName5 q in dependByName nm f id
    let replace0 (q:Quotations.Expr<                    'T>) (fr:                    'T) = let (nm, _) = getName0 q in nm, box fr
    let replace1 (q:Quotations.Expr<'a->                'T>) (fr:'a->                'T) = let (nm, _) = getName1 q in nm, box fr
    let replace2 (q:Quotations.Expr<'a->'b->            'T>) (fr:'a->'b->            'T) = let (nm, _) = getName2 q in nm, box fr
    let replace3 (q:Quotations.Expr<'a->'b->'c->        'T>) (fr:'a->'b->'c->        'T) = let (nm, _) = getName3 q in nm, box fr
    let replace4 (q:Quotations.Expr<'a->'b->'c->'d->    'T>) (fr:'a->'b->'c->'d->    'T) = let (nm, _) = getName4 q in nm, box fr
    let replace5 (q:Quotations.Expr<'a->'b->'c->'d->'e->'T>) (fr:'a->'b->'c->'d->'e->'T) = let (nm, _) = getName5 q in nm, box fr

    let bind (f: 'a -> Depend<'b>) (pa:Depend<'a>) : Depend<'b> = 
        let rec bindR =
            function
            | Dependency(dep, k) -> Dependency(dep , fun p -> bindR (k p) ) 
            | NoMore     v       -> Dependency(None, fun p -> f v         )
        bindR pa
    let rtn = NoMore
    let map f = bind (f >> rtn)

    let replacer lst depend =
        let rec replace =
            function
            | Dependency(None       , k)      -> Dependency(None        , k >> replace)
            | Dependency(Some(nm, v), k)      ->
                lst 
                |> Seq.tryFind (fst >> ((=) nm))
                |> Option.map  (snd >> fun v2 -> Dependency(Some(nm, v2), k >> replace) )
                |> Option.defaultWith(fun ()  -> Dependency(Some(nm, v ), k >> replace) )
            | NoMore v                        -> NoMore              v
        replace depend

    let replacerDef lst depend =
        let rec replace =
            function
            | Dependency(None       , k)          -> Dependency(None         , k >> replace)
            | Dependency(Some(nm, v), k)          ->
                lst 
                |> Seq.tryFind (fun (_, (nm2, _)) -> nm2 = nm)
                |> Option.map  (fun (nmN,(_, v2)) -> Dependency(Some(nmN, v2), k >> replace) )
                |> Option.defaultWith(fun ()      -> Dependency(Some(nm , v ), k >> replace) )
            | NoMore v                            -> NoMore               v
        replace depend

    let resolver lst depend = 
        let rec resolve =
            function
            | Dependency(None       , k)      -> k () |> resolve
            | Dependency(Some(nm, v), k)      ->
                lst 
                |> Seq.tryFind (fst >> ((=) nm))
                |> Option.map  (snd >> fun v2 -> k v2  )
                |> Option.defaultWith(fun ()  -> k v )
                |> resolve
            | NoMore v                        ->   v
        resolve depend

    type DependBuilder() =
        member __.Bind   (m, f) = bind f m
        member __.Return     v  = rtn v
        member __.Delay      f  = f ()

    let depend = DependBuilder()

    let getDependencies dep =
        let rec collect lst dep =
            let     lst2 = dep :: lst
            match dep with
            | Dependency(None      , k) -> collect lst2 (k () )
            | Dependency(Some(_, v), k) -> collect lst2 (k v  )
            | NoMore f                  -> lst2
        collect [] dep
        |> List.filter (function| Depend.Dependency(None,_) -> false |_-> true) 
        |> List.rev 

    let toString dep =
        getDependencies dep
        |> Seq.map       
            (function 
            | Depend.Dependency(Some(nm, v), next) -> sprintf "%-50s %A" nm v
            | x -> string x)
        |> Seq.distinct
        |> Seq.sort
        |> String.concat "\n"

    module Operators =
        let (>>=) ma f = bind f ma
        let (|>>) ma f = map  f ma

