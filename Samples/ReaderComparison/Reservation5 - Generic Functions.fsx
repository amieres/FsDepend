#load @"..\Reservation\CommonDefs.fsx"
#load @"Reader.fs"

open System
open CommonDefs
open Reader.Operators

type IConnString = abstract Get        : string
type ICapacity   = abstract Get        : int
type IDbFuncs    = abstract ReadRsrv   : string -> DateTime           -> Reservation list
                   abstract CreateRsrv : string -> Reservation        -> int
type ILogger     = abstract Log        : Printf.StringFormat<'a,unit> -> 'a

let connectionStringR   (inj:#IConnString) = inj.Get        
let capacityR           (inj:#ICapacity  ) = inj.Get        
let readReservationsR   (inj:#IDbFuncs   ) = inj.ReadRsrv   
let createReservationR  (inj:#IDbFuncs   ) = inj.CreateRsrv 
let loggerR             (inj:#ILogger    ) = inj :> ILogger 

let tryAcceptR() = reader {
    let! logger             = loggerR
    let! connectionString   = connectionStringR
    let! capacity           = capacityR
    let! readReservations   = readReservationsR
    let! createReservation  = createReservationR

    return
        fun reservation ->
            logger.Log "Capacity: %d" capacity
            logger.Log "Connection String: %s" connectionString
            let reservedSeats =
                readReservations connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then createReservation connectionString { reservation with IsAccepted = true } |> Some
            else None
}

type Inject(connectionString, capacity, readReservations, createReservation) =
    interface IConnString with member __.Get              = connectionString       
    interface ICapacity   with member __.Get              = capacity               
    interface IDbFuncs    with member __.ReadRsrv   p1 p2 = readReservations  p1 p2
                               member __.CreateRsrv p1 p2 = createReservation p1 p2
    interface ILogger     with member __.Log        fmt   = Printf.ksprintf (printfn "%s") fmt

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
