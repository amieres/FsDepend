#load @"..\Reservation\CommonDefs.fsx"
#load @"Reader.fs"

open System
open CommonDefs
open Reader.Operators

type IInject = {
    connectionString  : string
    capacity          : int
    readReservations  : string -> DateTime -> Reservation list
    createReservation : string -> Reservation -> int
}

let connectionStringR   (inj:IInject) = inj.connectionString  
let capacityR           (inj:IInject) = inj.capacity          
let readReservationsR   (inj:IInject) = inj.readReservations  
let createReservationR  (inj:IInject) = inj.createReservation 

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
