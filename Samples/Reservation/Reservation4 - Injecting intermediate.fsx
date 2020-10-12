#load "CommonDefs.fsx"
#load @"..\..\FsDepend.fs"

// Injecting an intermediate function

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

let dbFunctionsDD = Depend.depend0 <@ dbFunctionsD @>

// Depend<(Reservation -> int option)>
let tryAcceptD = Depend.depend {
    let! capacity           = capacityD
    let! dbFunctionsD       = dbFunctionsDD
    let! db                 = dbFunctionsD

    return
        fun reservation ->
            let reservedSeats =
                db.readReservations   reservation.Date |> List.sumBy (fun x -> x.Quantity)
            if reservedSeats + reservation.Quantity <= capacity
            then db.createReservation { reservation with IsAccepted = true } |> Some
            else None
}


let dbFunctionsD2 = Depend.depend {
    return {| readReservations  = fun (_: DateTime   ) -> [] : Reservation list
              createReservation = fun (_: Reservation) -> 3
           |}
}


// Reservation -> int option
let tryAccept = 
    tryAcceptD
    |> Depend.resolver 
        [   Depend.replace0 <@ Globals.capacity     @> 20
            Depend.replace0 <@ dbFunctionsD         @> dbFunctionsD2
        ]

{
    Date        = DateTime.Today
    Quantity    = 5
    IsAccepted  = false
} 
|> tryAccept
|> printfn "%A"

tryAcceptD |> Depend.toString |> printfn "%s"
