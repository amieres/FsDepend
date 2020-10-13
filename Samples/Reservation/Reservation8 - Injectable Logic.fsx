#load "CommonDefs.fsx"
#load @"..\..\FsDepend.fs"

// Using Anonymous Records instead of tuples

open System
open CommonDefs

let connectionStringD   = Depend.depend0 <@ Globals.connectionString  @>
let capacityD           = Depend.depend0 <@ Globals.capacity          @>
let readReservationsD   = Depend.depend2 <@ DB.readReservations       @>
let createReservationD  = Depend.depend2 <@ DB.createReservation      @>

let dbFunctionsD = Depend.depend {
    let! connectionString  = connectionStringD
    let! readReservations  = readReservationsD
    let! createReservation = createReservationD

    return {| readReservations  = readReservations  connectionString
              createReservation = createReservation connectionString
           |}
}

let flip f a b = f b a

let tryAcceptPure capacity reservations reservation =
    let reservedSeats = reservations |> List.sumBy (fun x -> x.Quantity)
    if reservedSeats + reservation.Quantity <= capacity
    then { reservation with IsAccepted = true } |> Some
    else None

let tryAcceptPureD = Depend.depend3 <@ tryAcceptPure @>

// Depend<(Reservation -> int option)>
let tryAcceptD = Depend.depend {
    let! capacity           = capacityD
    let! db                 = dbFunctionsD
    let! tryAcceptPure      = tryAcceptPureD

    return
        fun reservation ->
            reservation.Date
            |> db.readReservations 
            |> flip (tryAcceptPure capacity) reservation
            |> Option.map db.createReservation 
}


// Reservation -> int option
let tryAccept = 
    tryAcceptD
    |> Depend.resolver 
        [   Depend.replace0 <@ Globals.capacity     @> 20
            Depend.replace2 <@ DB.readReservations  @> (fun _ _ -> []        ) 
            Depend.replace2 <@ DB.createReservation @> (fun _ r -> r.Quantity) 
        ]

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAccept
|> printfn "%A"

printfn "%O" tryAcceptD