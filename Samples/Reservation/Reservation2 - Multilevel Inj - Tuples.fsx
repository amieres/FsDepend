#load "CommonDefs.fsx"
#load @"..\..\FsDepend.fs"

// Multilevel Injection

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

    return (readReservations  connectionString)
          ,(createReservation connectionString)
}

// Depend<(Reservation -> int option)>
let tryAcceptD = Depend.depend {
    let! capacity           = capacityD
    let! readReservations
       , createReservation  = dbFunctionsD

    return
        fun reservation ->
            let reservedSeats =
                readReservations   reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then createReservation { reservation with IsAccepted = true } |> Some
            else None
}

// Reservation -> int option
let tryAccept = 
    tryAcceptD
    |> Depend.resolver 
        [   Depend.replace0 <@ Globals.capacity    @> 50 
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
