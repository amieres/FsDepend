#load "CommonDefs.fsx"

open System
open CommonDefs

// Reservation -> int option
let tryAccept reservation : int option =
    let reservedSeats =
        DB.readReservations Globals.connectionString reservation.Date |> List.sumBy (fun x -> x.Quantity)
    if reservedSeats + reservation.Quantity <= Globals.capacity
    then DB.createReservation Globals.connectionString { reservation with IsAccepted = true } |> Some
    else None

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAccept
|> printfn "%A"