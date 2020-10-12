#load "CommonDefs.fsx"

open System
open CommonDefs

let flip f a b = f b a

let tryAcceptPure capacity reservations reservation =
    let reservedSeats = reservations |> List.sumBy (fun x -> x.Quantity)
    if reservedSeats + reservation.Quantity <= capacity
    then { reservation with IsAccepted = true } |> Some
    else None


let tryAcceptComposition reservation =
    reservation.Date
    |> DB.readReservations Globals.connectionString
    |> flip (tryAcceptPure Globals.capacity) reservation
    |> Option.map (DB.createReservation Globals.connectionString)

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAcceptComposition
|> printfn "%A"