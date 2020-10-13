#load @"..\Reservation\CommonDefs.fsx"
#load @"Reader.fs"

open System
open CommonDefs
open Reader.Operators

type IConnString = abstract Get        : string
type ICapacity   = abstract Get        : int
type IDbFuncs    = abstract ReadRsrv   : string -> DateTime -> Reservation list
                   abstract CreateRsrv : string -> Reservation -> int

let connectionStringR   (inj:#IConnString) = inj.Get        
let capacityR           (inj:#ICapacity  ) = inj.Get        
let readReservationsR   (inj:#IDbFuncs   ) = inj.ReadRsrv   
let createReservationR  (inj:#IDbFuncs   ) = inj.CreateRsrv 

let dbFunctionsR() = reader {
    let! connectionString  = connectionStringR
    let! readReservations  = readReservationsR
    let! createReservation = createReservationR

    return {| readReservations  = readReservations  connectionString
              createReservation = createReservation connectionString
           |}
}

// unit -> Reader<(Reservation -> int option), 'a>
let tryAcceptR() = reader {
    let! capacity           = capacityR
    let! db                 = dbFunctionsR()

    return
        fun reservation ->
            let reservedSeats =
                db.readReservations   reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then db.createReservation { reservation with IsAccepted = true } |> Some
            else None
}

type Inject(connectionString, capacity, readReservations, createReservation) =
    interface IConnString with member __.Get              = connectionString       
    interface ICapacity   with member __.Get              = capacity               
    interface IDbFuncs    with member __.ReadRsrv   p1 p2 = readReservations  p1 p2
                               member __.CreateRsrv p1 p2 = createReservation p1 p2

// Reservation -> int option
let tryAccept = 
    tryAcceptR()
    |> Reader.run (Inject(
                            connectionString  = "Some connection"
                          , capacity          = 50 
                          , readReservations  = (fun _ _ -> []        ) 
                          , createReservation = (fun _ r -> r.Quantity) 
                        ))

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
}              
|> tryAccept   
|> printfn "%A"
