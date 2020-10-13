#load @"..\Reservation\CommonDefs.fsx"
#load @"Reader.fs"

open System
open CommonDefs
open Reader.Operators

type Inject = {
    connectionString  : string
    capacity          : int
    readReservations  : string -> DateTime -> Reservation list
    createReservation : string -> Reservation -> int
}

let connectionStringR   (inj:Inject) = inj.connectionString  
let capacityR           (inj:Inject) = inj.capacity          
let readReservationsR   (inj:Inject) = inj.readReservations  
let createReservationR  (inj:Inject) = inj.createReservation 

// Reader<(Reservation -> int option), IInject>
let tryAcceptR = reader {
    let! connectionString   = connectionStringR
    let! capacity           = capacityR
    let! readReservations   = readReservationsR
    let! createReservation  = createReservationR

    return
        fun reservation ->
            let reservedSeats =
                readReservations connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then createReservation connectionString { reservation with IsAccepted = true } |> Some
            else None
}

// Reservation -> int option
let tryAccept = 
    tryAcceptR
    |> Reader.run {
            connectionString  = "Some connection"
            capacity          = 50 
            readReservations  = (fun _ _ -> []        ) 
            createReservation = (fun _ r -> r.Quantity) 
        }

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAccept
|> printfn "%A"
